using VotingSystem.Data;
using VotingSystem.Models;

namespace VotingSystem.Services
{
    public class AuditService
    {
        private readonly MongoDbContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(MongoDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string userId, string userName, string action, string entityType, string entityId, string details)
        {
            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var log = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.AuditLogs.InsertOneAsync(log);
        }
    }
}
