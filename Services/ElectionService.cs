using MongoDB.Driver;
using VotingSystem.Data;
using VotingSystem.Models;

namespace VotingSystem.Services
{
    public class ElectionService
    {
        private readonly MongoDbContext _dbContext;
        private readonly AuditService _auditService;

        public ElectionService(MongoDbContext dbContext, AuditService auditService)
        {
            _dbContext = dbContext;
            _auditService = auditService;
        }

        public async Task<List<Election>> GetPublishedElectionsAsync()
        {
            return await _dbContext.Elections
                .Find(e => e.Status == "published")
                .SortBy(e => e.StartAt)
                .ToListAsync();
        }

        public async Task<Election?> GetElectionByIdAsync(string electionId)
        {
            return await _dbContext.Elections
                .Find(e => e.Id == electionId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Position>> GetPositionsByElectionAsync(string electionId)
        {
            return await _dbContext.Positions
                .Find(p => p.ElectionId == electionId)
                .SortBy(p => p.Order)
                .ToListAsync();
        }

        public async Task<List<Candidate>> GetCandidatesByPositionAsync(string positionId)
        {
            return await _dbContext.Candidates
                .Find(c => c.PositionId == positionId && c.Status == "Approved")
                .ToListAsync();
        }

        public async Task<List<Candidate>> GetCandidatesByElectionAsync(string electionId)
        {
            return await _dbContext.Candidates
                .Find(c => c.ElectionId == electionId && c.Status == "Approved")
                .ToListAsync();
        }

        public async Task<bool> CreateElectionAsync(Election election, string userId, string userName)
        {
            election.CreatedBy = userId;
            election.CreatedAt = DateTime.UtcNow;
            await _dbContext.Elections.InsertOneAsync(election);
            await _auditService.LogAsync(userId, userName, "ElectionCreated", "Election", election.Id!, $"Created election: {election.Title}");
            return true;
        }

        public async Task<bool> UpdateElectionAsync(Election election, string userId, string userName)
        {
            election.UpdatedAt = DateTime.UtcNow;
            var result = await _dbContext.Elections.ReplaceOneAsync(e => e.Id == election.Id, election);
            if (result.ModifiedCount > 0)
            {
                await _auditService.LogAsync(userId, userName, "ElectionUpdated", "Election", election.Id!, $"Updated election: {election.Title}");
            }
            return result.ModifiedCount > 0;
        }

        public async Task<bool> CreatePositionAsync(Position position, string userId, string userName)
        {
            await _dbContext.Positions.InsertOneAsync(position);
            await _auditService.LogAsync(userId, userName, "PositionCreated", "Position", position.Id!, $"Created position: {position.Title}");
            return true;
        }

        public async Task<bool> ApplyCandidacyAsync(Candidate candidate, string userId, string userName)
        {
            candidate.AppliedAt = DateTime.UtcNow;
            candidate.Status = "Pending";
            await _dbContext.Candidates.InsertOneAsync(candidate);
            await _auditService.LogAsync(userId, userName, "CandidateApplied", "Candidate", candidate.Id!, $"Applied for candidacy: {candidate.Name}");
            return true;
        }

        public async Task<Candidate?> GetCandidateByIdAsync(string candidateId)
        {
            return await _dbContext.Candidates
                .Find(c => c.Id == candidateId)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ApproveCandidateAsync(string candidateId, string userId, string userName)
        {
            var candidate = await GetCandidateByIdAsync(candidateId);
            if (candidate == null) return false;

            candidate.Status = "Approved";
            candidate.ApprovedAt = DateTime.UtcNow;
            candidate.ApprovedBy = userId;

            var result = await _dbContext.Candidates.ReplaceOneAsync(c => c.Id == candidateId, candidate);
            if (result.ModifiedCount > 0)
            {
                await _auditService.LogAsync(userId, userName, "CandidateApproved", "Candidate", candidateId, $"Approved candidate: {candidate.Name}");
            }
            return result.ModifiedCount > 0;
        }

        public async Task<bool> RejectCandidateAsync(string candidateId, string reason, string userId, string userName)
        {
            var candidate = await GetCandidateByIdAsync(candidateId);
            if (candidate == null) return false;

            candidate.Status = "Rejected";
            candidate.RejectionReason = reason;

            var result = await _dbContext.Candidates.ReplaceOneAsync(c => c.Id == candidateId, candidate);
            if (result.ModifiedCount > 0)
            {
                await _auditService.LogAsync(userId, userName, "CandidateRejected", "Candidate", candidateId, $"Rejected candidate: {candidate.Name}");
            }
            return result.ModifiedCount > 0;
        }

        public async Task<Position?> GetPositionByIdAsync(string positionId)
        {
            return await _dbContext.Positions
                .Find(p => p.Id == positionId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Election>> GetAllElectionsAsync()
        {
            return await _dbContext.Elections
                .Find(_ => true)
                .SortByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Candidate>> GetPendingCandidatesAsync()
        {
            return await _dbContext.Candidates
                .Find(c => c.Status == "Pending")
                .ToListAsync();
        }

        // Check if student is already a candidate in an election
        public async Task<(string PositionId, string PositionTitle)?> CheckExistingCandidacyAsync(string userId, string electionId)
        {
            var existingCandidate = await _dbContext.Candidates
                .Find(c => c.UserId == userId && c.ElectionId == electionId)
                .FirstOrDefaultAsync();

            if (existingCandidate == null)
                return null;

            var position = await GetPositionByIdAsync(existingCandidate.PositionId);
            return (existingCandidate.PositionId, position?.Title ?? "Unknown Position");
        }

        // Get election results
        public async Task<Dictionary<string, Dictionary<string, int>>> GetElectionResultsAsync(string electionId)
        {
            var votes = await _dbContext.Votes
                .Find(v => v.ElectionId == electionId)
                .ToListAsync();

            var results = new Dictionary<string, Dictionary<string, int>>();

            foreach (var vote in votes)
            {
                if (!results.ContainsKey(vote.PositionId))
                    results[vote.PositionId] = new Dictionary<string, int>();

                if (!results[vote.PositionId].ContainsKey(vote.CandidateId))
                    results[vote.PositionId][vote.CandidateId] = 0;

                results[vote.PositionId][vote.CandidateId]++;
            }

            return results;
        }

        // Get election results with candidate details
        public async Task<Dictionary<string, List<CandidateResult>>> GetElectionResultsWithDetailsAsync(string electionId)
        {
            var votes = await _dbContext.Votes
                .Find(v => v.ElectionId == electionId)
                .ToListAsync();

            // Count votes per candidate
            var voteCounts = new Dictionary<string, int>();
            foreach (var vote in votes)
            {
                if (!voteCounts.ContainsKey(vote.CandidateId))
                    voteCounts[vote.CandidateId] = 0;
                voteCounts[vote.CandidateId]++;
            }

            // Get all candidates for this election
            var candidates = await _dbContext.Candidates
                .Find(c => c.ElectionId == electionId && c.Status == "Approved")
                .ToListAsync();

            // Group results by position
            var results = new Dictionary<string, List<CandidateResult>>();

            foreach (var candidate in candidates)
            {
                if (!results.ContainsKey(candidate.PositionId))
                    results[candidate.PositionId] = new List<CandidateResult>();

                var voteCount = voteCounts.ContainsKey(candidate.Id!) ? voteCounts[candidate.Id!] : 0;

                results[candidate.PositionId].Add(new CandidateResult
                {
                    CandidateId = candidate.Id!,
                    CandidateName = candidate.Name,
                    StudentNumber = candidate.StudentNumber,
                    VoteCount = voteCount
                });
            }

            // Sort candidates by vote count (descending) within each position
            foreach (var positionId in results.Keys.ToList())
            {
                results[positionId] = results[positionId]
                    .OrderByDescending(c => c.VoteCount)
                    .ToList();
            }

            return results;
        }

        // Helper class for candidate results
        public class CandidateResult
        {
            public string CandidateId { get; set; } = string.Empty;
            public string CandidateName { get; set; } = string.Empty;
            public string StudentNumber { get; set; } = string.Empty;
            public int VoteCount { get; set; }
        }
    }
}
