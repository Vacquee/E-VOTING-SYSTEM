using MongoDB.Driver;
using VotingSystem.Data;
using VotingSystem.Models;

namespace VotingSystem.Services
{
    public class VotingService
    {
        private readonly MongoDbContext _dbContext;
        private readonly AuditService _auditService;

        public VotingService(MongoDbContext dbContext, AuditService auditService)
        {
            _dbContext = dbContext;
            _auditService = auditService;
        }

        // Check if user has already voted for a specific position
        public async Task<bool> HasVotedForPositionAsync(string electionId, string positionId, string voterId)
        {
            var vote = await _dbContext.Votes
                .Find(v => v.ElectionId == electionId && v.PositionId == positionId && v.VoterId == voterId)
                .FirstOrDefaultAsync();

            return vote != null;
        }

        // Get voting status for all positions in an election
        public async Task<Dictionary<string, bool>> GetVotingStatusAsync(string electionId, string voterId)
        {
            var votes = await _dbContext.Votes
                .Find(v => v.ElectionId == electionId && v.VoterId == voterId)
                .ToListAsync();

            return votes.ToDictionary(v => v.PositionId, v => true);
        }

        // Submit votes for an election
        // Enforces one vote per position using unique index and duplicate check
        public async Task<(bool success, string message)> SubmitVotesAsync(
            string electionId,
            Dictionary<string, string> positionVotes,
            string voterId,
            string voterName,
            string ipAddress)
        {
            try
            {
                // Validate that user hasn't already voted for any of these positions
                foreach (var (positionId, candidateId) in positionVotes)
                {
                    var hasVoted = await HasVotedForPositionAsync(electionId, positionId, voterId);
                    if (hasVoted)
                    {
                        return (false, "You have already voted for one or more positions in this election.");
                    }
                }

                // Create vote records
                var votes = new List<Vote>();
                foreach (var (positionId, candidateId) in positionVotes)
                {
                    votes.Add(new Vote
                    {
                        ElectionId = electionId,
                        PositionId = positionId,
                        CandidateId = candidateId,
                        VoterId = voterId,
                        IpAddress = ipAddress,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Insert all votes
                // The unique index on (ElectionId, PositionId, VoterId) will prevent duplicates
                await _dbContext.Votes.InsertManyAsync(votes);

                // Audit log
                await _auditService.LogAsync(
                    voterId,
                    voterName,
                    "VoteSubmitted",
                    "Vote",
                    electionId,
                    $"Submitted {votes.Count} vote(s) for election");

                return (true, "Your votes have been successfully submitted.");
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // Duplicate key error means user already voted
                return (false, "You have already voted for one or more positions. Each student can only vote once per position.");
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred while submitting your votes: {ex.Message}");
            }
        }

        // Get vote count for a candidate
        public async Task<int> GetVoteCountAsync(string candidateId)
        {
            var count = await _dbContext.Votes
                .CountDocumentsAsync(v => v.CandidateId == candidateId);
            return (int)count;
        }

        // Get total votes for an election
        public async Task<int> GetTotalVotesForElectionAsync(string electionId)
        {
            var count = await _dbContext.Votes
                .CountDocumentsAsync(v => v.ElectionId == electionId);
            return (int)count;
        }

        // Get unique voters for an election
        public async Task<int> GetUniqueVotersCountAsync(string electionId)
        {
            var votes = await _dbContext.Votes
                .Find(v => v.ElectionId == electionId)
                .ToListAsync();

            return votes.Select(v => v.VoterId).Distinct().Count();
        }
    }
}
