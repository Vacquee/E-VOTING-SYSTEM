using MongoDB.Driver;
using VotingSystem.Data;
using VotingSystem.Models;

namespace VotingSystem.Services
{
    public class VerificationService
    {
        private readonly MongoDbContext _dbContext;
        private readonly AuditService _auditService;

        public VerificationService(MongoDbContext dbContext, AuditService auditService)
        {
            _dbContext = dbContext;
            _auditService = auditService;
        }

        public async Task<bool> RequestVerificationAsync(string userId, string documentUrl)
        {
            var user = await _dbContext.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null) return false;

            // Check if already has a pending request
            var existingRequest = await _dbContext.VerificationRequests
                .Find(vr => vr.UserId == userId && vr.Status == "Pending")
                .FirstOrDefaultAsync();

            if (existingRequest != null)
                return false;

            var request = new VerificationRequest
            {
                UserId = userId,
                StudentNumber = user.StudentNumber,
                Name = user.Name,
                Email = user.Email,
                DocumentUrl = documentUrl,
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            };

            await _dbContext.VerificationRequests.InsertOneAsync(request);

            // Update user
            user.VerificationRequested = true;
            user.VerificationRequestedAt = DateTime.UtcNow;
            user.VerificationDocumentUrl = documentUrl;
            await _dbContext.Users.ReplaceOneAsync(u => u.Id == userId, user);

            await _auditService.LogAsync(userId, user.Name, "VerificationRequested", "VerificationRequest", request.Id!, "User requested account verification");

            return true;
        }

        public async Task<List<VerificationRequest>> GetPendingRequestsAsync()
        {
            return await _dbContext.VerificationRequests
                .Find(vr => vr.Status == "Pending")
                .SortBy(vr => vr.RequestedAt)
                .ToListAsync();
        }

        public async Task<bool> ApproveVerificationAsync(string requestId, string adminId, string adminName)
        {
            var request = await _dbContext.VerificationRequests
                .Find(vr => vr.Id == requestId)
                .FirstOrDefaultAsync();

            if (request == null) return false;

            request.Status = "Approved";
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedBy = adminId;

            await _dbContext.VerificationRequests.ReplaceOneAsync(vr => vr.Id == requestId, request);

            // Update user
            var user = await _dbContext.Users.Find(u => u.Id == request.UserId).FirstOrDefaultAsync();
            if (user != null)
            {
                user.IsVerified = true;
                user.VerificationApprovedAt = DateTime.UtcNow;
                await _dbContext.Users.ReplaceOneAsync(u => u.Id == request.UserId, user);
            }

            await _auditService.LogAsync(adminId, adminName, "VerificationApproved", "VerificationRequest", requestId, $"Approved verification for {request.Name}");

            return true;
        }

        public async Task<bool> RejectVerificationAsync(string requestId, string reason, string adminId, string adminName)
        {
            var request = await _dbContext.VerificationRequests
                .Find(vr => vr.Id == requestId)
                .FirstOrDefaultAsync();

            if (request == null) return false;

            request.Status = "Rejected";
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedBy = adminId;
            request.RejectionReason = reason;

            await _dbContext.VerificationRequests.ReplaceOneAsync(vr => vr.Id == requestId, request);

            // Update user
            var user = await _dbContext.Users.Find(u => u.Id == request.UserId).FirstOrDefaultAsync();
            if (user != null)
            {
                user.VerificationRequested = false;
                await _dbContext.Users.ReplaceOneAsync(u => u.Id == request.UserId, user);
            }

            await _auditService.LogAsync(adminId, adminName, "VerificationRejected", "VerificationRequest", requestId, $"Rejected verification for {request.Name}");

            return true;
        }

        public async Task<VerificationRequest?> GetVerificationRequestByUserIdAsync(string userId)
        {
            return await _dbContext.VerificationRequests
                .Find(vr => vr.UserId == userId)
                .SortByDescending(vr => vr.RequestedAt)
                .FirstOrDefaultAsync();
        }
    }
}
