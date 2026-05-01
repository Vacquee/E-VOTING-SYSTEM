using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VotingSystem.Models;
using VotingSystem.Services;

namespace VotingSystem.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ElectionService _electionService;
        private readonly VotingService _votingService;
        private readonly AuthService _authService;
        private readonly VerificationService _verificationService;

        public StudentController(
            ElectionService electionService,
            VotingService votingService,
            AuthService authService,
            VerificationService verificationService)
        {
            _electionService = electionService;
            _votingService = votingService;
            _authService = authService;
            _verificationService = verificationService;
        }

        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        private string GetUserName() => User.Identity?.Name ?? string.Empty;
        private bool IsVerified() => User.FindFirst("IsVerified")?.Value == "True";

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var userId = GetUserId();
            var user = await _authService.GetUserByIdAsync(userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var allElections = await _electionService.GetPublishedElectionsAsync();
            var now = DateTime.UtcNow;

            var viewModel = new DashboardViewModel
            {
                User = user,
                OngoingElections = allElections.Where(e => e.IsOngoing).ToList(),
                UpcomingElections = allElections.Where(e => e.ComputedStatus == "Upcoming").ToList(),
                PastElections = allElections.Where(e => e.IsPast).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Election(string id)
        {
            var election = await _electionService.GetElectionByIdAsync(id);
            if (election == null)
            {
                return NotFound();
            }

            var userId = GetUserId();
            var positions = await _electionService.GetPositionsByElectionAsync(id);
            var positionWithCandidates = new List<PositionWithCandidates>();

            foreach (var position in positions)
            {
                var candidates = await _electionService.GetCandidatesByPositionAsync(position.Id!);
                positionWithCandidates.Add(new PositionWithCandidates
                {
                    Position = position,
                    Candidates = candidates
                });
            }

            var votingStatus = await _votingService.GetVotingStatusAsync(id, userId);
            var isVerified = IsVerified();

            var viewModel = new ElectionDetailsViewModel
            {
                Election = election,
                Positions = positionWithCandidates,
                VotingStatus = votingStatus,
                CanVote = election.IsOngoing && isVerified,
                IsVerified = isVerified
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Vote(string id)
        {
            var election = await _electionService.GetElectionByIdAsync(id);
            if (election == null || !election.IsOngoing)
            {
                TempData["ErrorMessage"] = "This election is not available for voting.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (!IsVerified())
            {
                TempData["ErrorMessage"] = "You must verify your account before voting.";
                return RedirectToAction(nameof(Profile));
            }

            var userId = GetUserId();
            var positions = await _electionService.GetPositionsByElectionAsync(id);
            var positionWithCandidates = new List<PositionWithCandidates>();

            foreach (var position in positions)
            {
                var hasVoted = await _votingService.HasVotedForPositionAsync(id, position.Id!, userId);
                if (hasVoted)
                {
                    TempData["ErrorMessage"] = "You have already voted in this election.";
                    return RedirectToAction(nameof(Election), new { id });
                }

                var candidates = await _electionService.GetCandidatesByPositionAsync(position.Id!);
                positionWithCandidates.Add(new PositionWithCandidates
                {
                    Position = position,
                    Candidates = candidates
                });
            }

            ViewBag.Election = election;
            return View(positionWithCandidates);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitVote(VoteSubmissionViewModel model)
        {
            if (!model.AcceptTerms)
            {
                TempData["ErrorMessage"] = "You must accept the terms and conditions to vote.";
                return RedirectToAction(nameof(Vote), new { id = model.ElectionId });
            }

            if (!IsVerified())
            {
                TempData["ErrorMessage"] = "You must verify your account before voting.";
                return RedirectToAction(nameof(Profile));
            }

            var userId = GetUserId();
            var userName = GetUserName();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var (success, message) = await _votingService.SubmitVotesAsync(
                model.ElectionId,
                model.PositionVotes,
                userId,
                userName,
                ipAddress);

            if (success)
            {
                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(VoteConfirmation), new { id = model.ElectionId });
            }

            TempData["ErrorMessage"] = message;
            return RedirectToAction(nameof(Vote), new { id = model.ElectionId });
        }

        [HttpGet]
        public async Task<IActionResult> VoteConfirmation(string id)
        {
            var election = await _electionService.GetElectionByIdAsync(id);
            if (election == null)
            {
                return NotFound();
            }

            ViewBag.Election = election;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = GetUserId();
            var user = await _authService.GetUserByIdAsync(userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var verificationRequest = await _verificationService.GetVerificationRequestByUserIdAsync(userId);
            ViewBag.VerificationRequest = verificationRequest;

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestVerification(string documentUrl)
        {
            var userId = GetUserId();

            if (string.IsNullOrWhiteSpace(documentUrl))
            {
                documentUrl = "document_placeholder.pdf"; // In a real app, handle file upload
            }

            var success = await _verificationService.RequestVerificationAsync(userId, documentUrl);

            if (success)
            {
                TempData["SuccessMessage"] = "Verification request submitted successfully. Please wait for admin approval.";
            }
            else
            {
                TempData["ErrorMessage"] = "You already have a pending verification request.";
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public async Task<IActionResult> ApplyCandidate(string electionId, string positionId)
        {
            if (!IsVerified())
            {
                TempData["ErrorMessage"] = "You must verify your account before applying as a candidate.";
                return RedirectToAction(nameof(Profile));
            }

            var userId = GetUserId();

            // Check if student is already a candidate in this election
            var existingCandidacy = await _electionService.CheckExistingCandidacyAsync(userId, electionId);
            if (existingCandidacy != null)
            {
                TempData["ErrorMessage"] = $"You are already a candidate in this election for the position: {existingCandidacy.Value.PositionTitle}. You cannot apply for multiple positions in the same election.";
                return RedirectToAction(nameof(Election), new { id = electionId });
            }

            var election = await _electionService.GetElectionByIdAsync(electionId);
            var position = await _electionService.GetPositionByIdAsync(positionId);

            if (election == null || position == null)
            {
                return NotFound();
            }

            ViewBag.Election = election;
            ViewBag.Position = position;

            return View(new CandidateApplicationViewModel
            {
                ElectionId = electionId,
                PositionId = positionId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyCandidate(CandidateApplicationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!IsVerified())
            {
                TempData["ErrorMessage"] = "You must verify your account before applying as a candidate.";
                return RedirectToAction(nameof(Profile));
            }

            var userId = GetUserId();

            // Check if student is already a candidate in this election
            var existingCandidacy = await _electionService.CheckExistingCandidacyAsync(userId, model.ElectionId);
            if (existingCandidacy != null)
            {
                TempData["ErrorMessage"] = $"You are already a candidate in this election for the position: {existingCandidacy.Value.PositionTitle}. You cannot apply for multiple positions in the same election.";
                return RedirectToAction(nameof(Election), new { id = model.ElectionId });
            }

            var user = await _authService.GetUserByIdAsync(userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var candidate = new Candidate
            {
                ElectionId = model.ElectionId,
                PositionId = model.PositionId,
                UserId = userId,
                Name = user.Name,
                StudentNumber = user.StudentNumber,
                Bio = model.Bio,
                PhotoUrl = model.PhotoUrl,
                Status = "Pending"
            };

            await _electionService.ApplyCandidacyAsync(candidate, userId, user.Name);

            TempData["SuccessMessage"] = "Your candidacy application has been submitted. Please wait for admin approval.";
            return RedirectToAction(nameof(Election), new { id = model.ElectionId });
        }
    }
}
