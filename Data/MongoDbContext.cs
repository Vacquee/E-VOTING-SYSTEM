using MongoDB.Driver;
using VotingSystem.Models;

namespace VotingSystem.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IConfiguration configuration)
        {
            var connectionString = configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017";
            var databaseName = configuration["MongoDB:DatabaseName"] ?? "VotingSystemDB";

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>("users");
        public IMongoCollection<Election> Elections => _database.GetCollection<Election>("elections");
        public IMongoCollection<Position> Positions => _database.GetCollection<Position>("positions");
        public IMongoCollection<Candidate> Candidates => _database.GetCollection<Candidate>("candidates");
        public IMongoCollection<Vote> Votes => _database.GetCollection<Vote>("votes");
        public IMongoCollection<VerificationRequest> VerificationRequests => _database.GetCollection<VerificationRequest>("verification_requests");
        public IMongoCollection<AuditLog> AuditLogs => _database.GetCollection<AuditLog>("audit_logs");

        // Initialize indexes - called at startup
        public async Task InitializeIndexesAsync()
        {
            // Users collection indexes
            var usersIndexes = Builders<User>.IndexKeys;
            await Users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
                usersIndexes.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true }
            ));
            await Users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
                usersIndexes.Ascending(u => u.StudentNumber),
                new CreateIndexOptions { Unique = true }
            ));

            // Elections collection indexes
            var electionsIndexes = Builders<Election>.IndexKeys;
            await Elections.Indexes.CreateOneAsync(new CreateIndexModel<Election>(
                electionsIndexes.Ascending(e => e.Status)
            ));

            // Positions collection indexes
            var positionsIndexes = Builders<Position>.IndexKeys;
            await Positions.Indexes.CreateOneAsync(new CreateIndexModel<Position>(
                positionsIndexes.Ascending(p => p.ElectionId)
            ));

            // Candidates collection indexes
            var candidatesIndexes = Builders<Candidate>.IndexKeys;
            await Candidates.Indexes.CreateOneAsync(new CreateIndexModel<Candidate>(
                candidatesIndexes.Ascending(c => c.PositionId)
            ));
            await Candidates.Indexes.CreateOneAsync(new CreateIndexModel<Candidate>(
                candidatesIndexes.Ascending(c => c.ElectionId)
            ));

            // Votes collection indexes - enforce one vote per position per student
            var votesIndexes = Builders<Vote>.IndexKeys;
            await Votes.Indexes.CreateOneAsync(new CreateIndexModel<Vote>(
                votesIndexes
                    .Ascending(v => v.ElectionId)
                    .Ascending(v => v.PositionId)
                    .Ascending(v => v.VoterId),
                new CreateIndexOptions { Unique = true }
            ));

            // Verification requests indexes
            var verificationIndexes = Builders<VerificationRequest>.IndexKeys;
            await VerificationRequests.Indexes.CreateOneAsync(new CreateIndexModel<VerificationRequest>(
                verificationIndexes.Ascending(vr => vr.UserId)
            ));
            await VerificationRequests.Indexes.CreateOneAsync(new CreateIndexModel<VerificationRequest>(
                verificationIndexes.Ascending(vr => vr.Status)
            ));
        }
    }
}
