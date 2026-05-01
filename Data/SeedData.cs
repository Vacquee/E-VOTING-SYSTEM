using MongoDB.Driver;
using VotingSystem.Models;
using VotingSystem.Services;

namespace VotingSystem.Data
{
    public static class SeedData
    {
        public static async Task SeedAdminUserAsync(MongoDbContext dbContext, AuthService authService)
        {
            // Check if admin user already exists
            var adminExists = await dbContext.Users
                .Find(u => u.Roles.Contains("Admin"))
                .AnyAsync();

            if (adminExists)
            {
                Console.WriteLine("Admin user already exists.");
                return;
            }

            // Create default admin user
            var adminUser = new User
            {
                Name = "System Administrator",
                Email = "admin@votingsystem.com",
                StudentNumber = "00-00000",
                PasswordHash = authService.HashPassword("admin123"), // Change this in production!
                Birthday = new DateTime(1990, 1, 1),
                Sex = "Other",
                YearSection = "Admin",
                Roles = new List<string> { "Admin", "Student" },
                IsVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.Users.InsertOneAsync(adminUser);
            Console.WriteLine("Default admin user created:");
            Console.WriteLine("  Student Number: 00-00000");
            Console.WriteLine("  Password: admin123");
            Console.WriteLine("  ** PLEASE CHANGE THE DEFAULT PASSWORD **");
        }

        public static async Task SeedSampleDataAsync(MongoDbContext dbContext, AuthService authService, ElectionService electionService)
        {
            // This method can be called manually to seed sample elections and students for testing

            // Create sample students
            var students = new List<User>
            {
                new User
                {
                    Name = "John Doe",
                    Email = "john.doe@student.com",
                    StudentNumber = "24-12345",
                    PasswordHash = authService.HashPassword("password123"),
                    Birthday = new DateTime(2003, 5, 15),
                    Sex = "Male",
                    YearSection = "4-A",
                    Roles = new List<string> { "Student" },
                    IsVerified = true,
                    IsActive = true
                },
                new User
                {
                    Name = "Jane Smith",
                    Email = "jane.smith@student.com",
                    StudentNumber = "24-12346",
                    PasswordHash = authService.HashPassword("password123"),
                    Birthday = new DateTime(2003, 8, 22),
                    Sex = "Female",
                    YearSection = "4-A",
                    Roles = new List<string> { "Student" },
                    IsVerified = true,
                    IsActive = true
                }
            };

            foreach (var student in students)
            {
                var exists = await dbContext.Users.Find(u => u.StudentNumber == student.StudentNumber).AnyAsync();
                if (!exists)
                {
                    await dbContext.Users.InsertOneAsync(student);
                }
            }

            // Create sample election
            var electionExists = await dbContext.Elections.Find(_ => true).AnyAsync();
            if (!electionExists)
            {
                var election = new Election
                {
                    Title = "Student Council Elections 2024",
                    Description = "Annual student council elections for leadership positions.",
                    StartAt = DateTime.UtcNow.AddDays(-1),
                    EndAt = DateTime.UtcNow.AddDays(7),
                    Status = "published",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                };

                await dbContext.Elections.InsertOneAsync(election);

                // Create positions
                var positions = new List<Position>
                {
                    new Position
                    {
                        ElectionId = election.Id!,
                        Title = "President",
                        Description = "Student Council President",
                        Order = 1
                    },
                    new Position
                    {
                        ElectionId = election.Id!,
                        Title = "Vice President",
                        Description = "Student Council Vice President",
                        Order = 2
                    }
                };

                await dbContext.Positions.InsertManyAsync(positions);

                // Create sample candidates
                var candidates = new List<Candidate>
                {
                    new Candidate
                    {
                        ElectionId = election.Id!,
                        PositionId = positions[0].Id!,
                        UserId = students[0].Id!,
                        Name = students[0].Name,
                        StudentNumber = students[0].StudentNumber,
                        Bio = "Experienced leader with a passion for student welfare and academic excellence.",
                        Status = "Approved"
                    },
                    new Candidate
                    {
                        ElectionId = election.Id!,
                        PositionId = positions[0].Id!,
                        UserId = students[1].Id!,
                        Name = students[1].Name,
                        StudentNumber = students[1].StudentNumber,
                        Bio = "Dedicated to improving campus facilities and student engagement programs.",
                        Status = "Approved"
                    }
                };

                await dbContext.Candidates.InsertManyAsync(candidates);
            }

            Console.WriteLine("Sample data seeded successfully.");
        }
    }
}
