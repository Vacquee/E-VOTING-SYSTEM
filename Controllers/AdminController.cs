using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using VotingSystem.Data;
using VotingSystem.Models;
using VotingSystem.Services;

namespace VotingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly MongoDbContext _dbContext;
        private readonly ElectionService _electionService;
        private readonly VotingService _votingService;
        private readonly VerificationService _verificationService;
        private readonly AuthService _authService;

        public AdminController(
            MongoDbContext dbContext,
            ElectionService electionService,
            VotingService votingService,
            VerificationService verificationService,
            AuthService authService)
        {
            _dbContext = dbContext;
            _electionService = electionService;
            _votingService = votingService;
            _verificationService = verificationService;
            _authService = authService;
        }

        private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        private string GetUserName() => User.Identity?.Name ?? string.Empty;

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var totalStudents = await _dbContext.Users.CountDocumentsAsync(u => u.Roles.Contains("Student"));
            var verifiedStudents = await _dbContext.Users.CountDocumentsAsync(u => u.IsVerified);
            var pendingVerifications = await _dbContext.VerificationRequests.CountDocumentsAsync(vr => vr.Status == "Pending");
            var totalElections = await _dbContext.Elections.CountDocumentsAsync(_ => true);
            var activeElections = await _dbContext.Elections.CountDocumentsAsync(e => e.Status == "published");
            var pendingCandidates = await _dbContext.Candidates.CountDocumentsAsync(c => c.Status == "Pending");
            var recentElections = await _electionService.GetAllElectionsAsync();

            var viewModel = new AdminDashboardViewModel
            {
                TotalStudents = (int)totalStudents,
                VerifiedStudents = (int)verifiedStudents,
                PendingVerifications = (int)pendingVerifications,
                ActiveElections = (int)activeElections,
                TotalElections = (int)totalElections,
                PendingCandidates = (int)pendingCandidates,
                RecentElections = recentElections.Take(5).ToList()
            };

            return View(viewModel);
        }

        // Elections Management
        [HttpGet]
        public async Task<IActionResult> Elections()
        {
            var elections = await _electionService.GetAllElectionsAsync();
            return View(elections);
        }

        [HttpGet]
        public IActionResult CreateElection()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateElection(Election election)
        {
            if (!ModelState.IsValid)
            {
                return View(election);
            }

            await _electionService.CreateElectionAsync(election, GetUserId(), GetUserName());
            TempData["SuccessMessage"] = "Election created successfully.";
            return RedirectToAction(nameof(Elections));
        }

        [HttpGet]
        public async Task<IActionResult> EditElection(string id)
        {
            var election = await _electionService.GetElectionByIdAsync(id);
            if (election == null)
            {
                return NotFound();
            }

            return View(election);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditElection(Election election)
        {
            if (!ModelState.IsValid)
            {
                return View(election);
            }

            await _electionService.UpdateElectionAsync(election, GetUserId(), GetUserName());
            TempData["SuccessMessage"] = "Election updated successfully.";
            return RedirectToAction(nameof(Elections));
        }

        [HttpGet]
        public async Task<IActionResult> ElectionDetails(string id)
        {
            var election = await _electionService.GetElectionByIdAsync(id);
            if (election == null)
            {
                return NotFound();
            }

            var positions = await _electionService.GetPositionsByElectionAsync(id);
            var positionWithCandidates = new List<PositionWithCandidates>();

            foreach (var position in positions)
            {
                var candidates = await _dbContext.Candidates
                    .Find(c => c.PositionId == position.Id)
                    .ToListAsync();

                positionWithCandidates.Add(new PositionWithCandidates
                {
                    Position = position,
                    Candidates = candidates
                });
            }

            ViewBag.Election = election;
            ViewBag.Positions = positionWithCandidates;

            return View();
        }

        // Positions Management
        [HttpGet]
        public async Task<IActionResult> CreatePosition(string electionId)
        {
            var election = await _electionService.GetElectionByIdAsync(electionId);
            if (election == null)
            {
                return NotFound();
            }

            ViewBag.Election = election;
            return View(new Position { ElectionId = electionId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePosition(Position position)
        {
            if (!ModelState.IsValid)
            {
                return View(position);
            }

            await _electionService.CreatePositionAsync(position, GetUserId(), GetUserName());
            TempData["SuccessMessage"] = "Position created successfully.";
            return RedirectToAction(nameof(ElectionDetails), new { id = position.ElectionId });
        }

        // Students Management
        [HttpGet]
        public async Task<IActionResult> Students()
        {
            var students = await _dbContext.Users
                .Find(u => u.Roles.Contains("Student"))
                .SortBy(u => u.Name)
                .ToListAsync();

            return View(students);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStudentStatus(string id)
        {
            var user = await _authService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;
            await _authService.UpdateUserAsync(user);

            TempData["SuccessMessage"] = $"Student account {(user.IsActive ? "activated" : "deactivated")} successfully.";
            return RedirectToAction(nameof(Students));
        }

        // Verification Requests
        [HttpGet]
        public async Task<IActionResult> VerificationRequests()
        {
            var requests = await _verificationService.GetPendingRequestsAsync();
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveVerification(string id)
        {
            await _verificationService.ApproveVerificationAsync(id, GetUserId(), GetUserName());
            TempData["SuccessMessage"] = "Verification approved successfully.";
            return RedirectToAction(nameof(VerificationRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectVerification(string id, string reason)
        {
            await _verificationService.RejectVerificationAsync(id, reason, GetUserId(), GetUserName());
            TempData["SuccessMessage"] = "Verification rejected.";
            return RedirectToAction(nameof(VerificationRequests));
        }

        // Candidate Approvals
        [HttpGet]
        public async Task<IActionResult> CandidateApprovals()
        {
            var candidates = await _electionService.GetPendingCandidatesAsync();
            return View(candidates);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCandidate(string id)
        {
            await _electionService.ApproveCandidateAsync(id, GetUserId(), GetUserName());
            TempData["SuccessMessage"] = "Candidate approved successfully.";
            return RedirectToAction(nameof(CandidateApprovals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectCandidate(string id, string reason)
        {
            await _electionService.RejectCandidateAsync(id, reason, GetUserId(), GetUserName());
            TempData["SuccessMessage"] = "Candidate rejected.";
            return RedirectToAction(nameof(CandidateApprovals));
        }

        // Election Results
        [HttpGet]
        public async Task<IActionResult> ElectionResults(string id)
        {
            var election = await _electionService.GetElectionByIdAsync(id);
            if (election == null)
            {
                return NotFound();
            }

            var positions = await _electionService.GetPositionsByElectionAsync(id);
            var results = await _electionService.GetElectionResultsWithDetailsAsync(id);
            var totalVoters = await _votingService.GetUniqueVotersCountAsync(id);

            ViewBag.Election = election;
            ViewBag.Positions = positions;
            ViewBag.Results = results;
            ViewBag.TotalVoters = totalVoters;

            return View();
        }
    }
}
