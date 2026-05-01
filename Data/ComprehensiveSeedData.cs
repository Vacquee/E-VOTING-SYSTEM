using MongoDB.Driver;
using VotingSystem.Models;
using VotingSystem.Services;

namespace VotingSystem.Data
{
    public static class ComprehensiveSeedData
    {
        /// <summary>
        /// Seeds the database with 100+ students, elections, positions, candidates, and sample votes
        /// Distribution: Various verification statuses, candidate applications, and voting patterns
        /// </summary>
        public static async Task SeedLargeDatasetAsync(
            MongoDbContext dbContext,
            AuthService authService,
            ElectionService electionService)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("  COMPREHENSIVE DATA SEEDING STARTED");
            Console.WriteLine("========================================");

            // Check if data already exists
            var existingStudents = await dbContext.Users.CountDocumentsAsync(u => u.Roles.Contains("Student"));
            if (existingStudents >= 100)
            {
                Console.WriteLine($"Database already has {existingStudents} students. Skipping seed.");
                Console.WriteLine("To reseed, drop the database first.");
                return;
            }

            var random = new Random(12345); // Fixed seed for reproducibility

            // Arrays for generating realistic student data
            var firstNames = new[]
            {
                "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda",
                "William", "Barbara", "David", "Elizabeth", "Richard", "Susan", "Joseph", "Jessica",
                "Thomas", "Sarah", "Charles", "Karen", "Christopher", "Nancy", "Daniel", "Lisa",
                "Matthew", "Betty", "Anthony", "Margaret", "Mark", "Sandra", "Donald", "Ashley",
                "Steven", "Kimberly", "Paul", "Emily", "Andrew", "Donna", "Joshua", "Michelle",
                "Kenneth", "Carol", "Kevin", "Amanda", "Brian", "Dorothy", "George", "Melissa",
                "Edward", "Deborah", "Ronald", "Stephanie", "Timothy", "Rebecca", "Jason", "Sharon",
                "Jeffrey", "Laura", "Ryan", "Cynthia", "Jacob", "Kathleen", "Gary", "Amy",
                "Nicholas", "Shirley", "Eric", "Angela", "Jonathan", "Helen", "Stephen", "Anna",
                "Larry", "Brenda", "Justin", "Pamela", "Scott", "Nicole", "Brandon", "Emma",
                "Benjamin", "Samantha", "Samuel", "Katherine", "Raymond", "Christine", "Gregory", "Debra",
                "Frank", "Rachel", "Alexander", "Catherine", "Patrick", "Carolyn", "Jack", "Janet",
                "Dennis", "Ruth", "Jerry", "Maria", "Tyler", "Heather"
            };

            var lastNames = new[]
            {
                "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
                "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas",
                "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson", "White",
                "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson", "Walker", "Young",
                "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill", "Flores",
                "Green", "Adams", "Nelson", "Baker", "Hall", "Rivera", "Campbell", "Mitchell",
                "Carter", "Roberts", "Gomez", "Phillips", "Evans", "Turner", "Diaz", "Parker",
                "Cruz", "Edwards", "Collins", "Reyes", "Stewart", "Morris", "Morales", "Murphy",
                "Cook", "Rogers", "Gutierrez", "Ortiz", "Morgan", "Cooper", "Peterson", "Bailey",
                "Reed", "Kelly", "Howard", "Ramos", "Kim", "Cox", "Ward", "Richardson",
                "Watson", "Brooks", "Chavez", "Wood", "James", "Bennett", "Gray", "Mendoza",
                "Ruiz", "Hughes", "Price", "Alvarez", "Castillo", "Sanders", "Patel", "Myers",
                "Long", "Ross", "Foster", "Jimenez"
            };

            var yearSections = new[]
            {
                "1-A", "1-B", "1-C", "2-A", "2-B", "2-C", "3-A", "3-B", "3-C", "4-A", "4-B", "4-C"
            };

            var sexOptions = new[] { "Male", "Female" };

            // ===== STEP 1: Create 100 Students =====
            Console.WriteLine("\n[1/7] Creating 100 students...");

            var students = new List<User>();
            var studentIds = new List<string>();

            for (int i = 1; i <= 100; i++)
            {
                var firstName = firstNames[random.Next(firstNames.Length)];
                var lastName = lastNames[random.Next(lastNames.Length)];
                var studentNumber = $"24-{i:D5}"; // 24-00001 to 24-00100
                var yearSection = yearSections[random.Next(yearSections.Length)];
                var sex = sexOptions[random.Next(sexOptions.Length)];

                // Determine verification status
                // 60% verified, 20% pending verification, 20% not requested
                bool isVerified = i <= 60;
                bool verificationRequested = i > 60 && i <= 80;

                var student = new User
                {
                    Name = $"{firstName} {lastName}",
                    Email = $"{firstName.ToLower()}.{lastName.ToLower()}{i}@student.edu",
                    StudentNumber = studentNumber,
                    PasswordHash = authService.HashPassword("student123"),
                    Birthday = new DateTime(2003, random.Next(1, 13), random.Next(1, 28)),
                    Sex = sex,
                    YearSection = yearSection,
                    Roles = new List<string> { "Student" },
                    IsVerified = isVerified,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                    VerificationRequested = verificationRequested,
                    VerificationRequestedAt = verificationRequested ? DateTime.UtcNow.AddDays(-random.Next(1, 30)) : null,
                    VerificationApprovedAt = isVerified ? DateTime.UtcNow.AddDays(-random.Next(1, 180)) : null
                };

                students.Add(student);
            }

            await dbContext.Users.InsertManyAsync(students);
            Console.WriteLine($"   ✓ Created 100 students");
            Console.WriteLine($"   ✓ 60 verified, 20 pending verification, 20 unverified");

            // Store student IDs for later use
            studentIds = students.Select(s => s.Id!).ToList();

            // ===== STEP 2: Create Verification Requests =====
            Console.WriteLine("\n[2/7] Creating verification requests...");

            var verificationRequests = new List<VerificationRequest>();

            // Create verification requests for students 61-80 (pending)
            for (int i = 60; i < 80; i++)
            {
                var student = students[i];
                var request = new VerificationRequest
                {
                    UserId = student.Id!,
                    StudentNumber = student.StudentNumber,
                    Name = student.Name,
                    Email = student.Email,
                    DocumentUrl = $"documents/verification_{student.StudentNumber}.pdf",
                    Status = "Pending",
                    RequestedAt = student.VerificationRequestedAt ?? DateTime.UtcNow,
                };
                verificationRequests.Add(request);
            }

            if (verificationRequests.Any())
            {
                await dbContext.VerificationRequests.InsertManyAsync(verificationRequests);
                Console.WriteLine($"   ✓ Created {verificationRequests.Count} pending verification requests");
            }

            // ===== STEP 3: Create Elections =====
            Console.WriteLine("\n[3/7] Creating elections...");

            var elections = new List<Election>();

            // Past Election
            var pastElection = new Election
            {
                Title = "Student Council Elections 2023",
                Description = "Previous year's student council elections - results available",
                StartAt = DateTime.UtcNow.AddDays(-120),
                EndAt = DateTime.UtcNow.AddDays(-113),
                Status = "closed",
                CreatedAt = DateTime.UtcNow.AddDays(-150),
                CreatedBy = "admin"
            };
            elections.Add(pastElection);

            // Ongoing Election
            var ongoingElection = new Election
            {
                Title = "Student Council Elections 2024",
                Description = "Annual student council elections for leadership positions",
                StartAt = DateTime.UtcNow.AddDays(-2),
                EndAt = DateTime.UtcNow.AddDays(5),
                Status = "published",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                CreatedBy = "admin"
            };
            elections.Add(ongoingElection);

            // Upcoming Election
            var upcomingElection = new Election
            {
                Title = "Sports Committee Elections 2024",
                Description = "Elections for sports and athletics committee representatives",
                StartAt = DateTime.UtcNow.AddDays(10),
                EndAt = DateTime.UtcNow.AddDays(17),
                Status = "published",
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                CreatedBy = "admin"
            };
            elections.Add(upcomingElection);

            await dbContext.Elections.InsertManyAsync(elections);
            Console.WriteLine($"   ✓ Created 3 elections (Past, Ongoing, Upcoming)");

            // ===== STEP 4: Create Positions =====
            Console.WriteLine("\n[4/7] Creating positions...");

            var allPositions = new List<Position>();

            // Positions for each election
            var electionPositions = new Dictionary<string, List<(string title, string desc)>>
            {
                [pastElection.Id!] = new()
                {
                    ("President", "Student Council President"),
                    ("Vice President", "Student Council Vice President"),
                    ("Secretary", "Student Council Secretary"),
                    ("Treasurer", "Student Council Treasurer")
                },
                [ongoingElection.Id!] = new()
                {
                    ("President", "Student Council President"),
                    ("Vice President", "Student Council Vice President"),
                    ("Secretary", "Student Council Secretary"),
                    ("Treasurer", "Student Council Treasurer"),
                    ("Public Relations Officer", "Public Relations and Communications")
                },
                [upcomingElection.Id!] = new()
                {
                    ("Sports Coordinator", "Overall sports activities coordinator"),
                    ("Athletics Captain", "Track and field captain"),
                    ("Team Sports Captain", "Basketball, volleyball, soccer captain")
                }
            };

            foreach (var electionPos in electionPositions)
            {
                int order = 1;
                foreach (var (title, desc) in electionPos.Value)
                {
                    var position = new Position
                    {
                        ElectionId = electionPos.Key,
                        Title = title,
                        Description = desc,
                        Order = order++,
                        CreatedAt = DateTime.UtcNow.AddDays(-20)
                    };
                    allPositions.Add(position);
                }
            }

            await dbContext.Positions.InsertManyAsync(allPositions);
            Console.WriteLine($"   ✓ Created {allPositions.Count} positions across all elections");

            // ===== STEP 5: Create Candidates =====
            Console.WriteLine("\n[5/7] Creating candidates...");

            var candidates = new List<Candidate>();
            var candidateIndex = 0;

            // Get verified students for candidacy
            var verifiedStudents = students.Where(s => s.IsVerified).ToList();

            // For each election, create candidates
            foreach (var election in elections)
            {
                var positions = allPositions.Where(p => p.ElectionId == election.Id).ToList();

                foreach (var position in positions)
                {
                    // Create 2-4 candidates per position
                    int candidateCount = random.Next(2, 5);

                    for (int i = 0; i < candidateCount && candidateIndex < verifiedStudents.Count; i++)
                    {
                        var student = verifiedStudents[candidateIndex];
                        candidateIndex++;

                        // Determine candidate status
                        // 70% approved, 20% pending, 10% rejected
                        string status;
                        DateTime? approvedAt = null;
                        string? approvedBy = null;
                        string? rejectionReason = null;

                        var statusRoll = random.Next(100);
                        if (statusRoll < 70)
                        {
                            status = "Approved";
                            approvedAt = DateTime.UtcNow.AddDays(-random.Next(1, 20));
                            approvedBy = "admin";
                        }
                        else if (statusRoll < 90)
                        {
                            status = "Pending";
                        }
                        else
                        {
                            status = "Rejected";
                            rejectionReason = "Incomplete application documentation";
                        }

                        var candidate = new Candidate
                        {
                            ElectionId = election.Id!,
                            PositionId = position.Id!,
                            UserId = student.Id!,
                            Name = student.Name,
                            StudentNumber = student.StudentNumber,
                            Bio = GenerateRandomBio(student.Name, position.Title, random),
                            PhotoUrl = $"photos/{student.StudentNumber}.jpg",
                            Status = status,
                            AppliedAt = DateTime.UtcNow.AddDays(-random.Next(5, 25)),
                            ApprovedAt = approvedAt,
                            ApprovedBy = approvedBy,
                            RejectionReason = rejectionReason
                        };

                        candidates.Add(candidate);
                    }
                }
            }

            await dbContext.Candidates.InsertManyAsync(candidates);
            Console.WriteLine($"   ✓ Created {candidates.Count} candidates");
            Console.WriteLine($"   ✓ ~70% approved, ~20% pending, ~10% rejected");

            // ===== STEP 6: Create Votes for Past and Ongoing Elections =====
            Console.WriteLine("\n[6/7] Creating votes...");

            var votes = new List<Vote>();

            // Votes for past election (closed - 50 voters)
            var pastPositions = allPositions.Where(p => p.ElectionId == pastElection.Id).ToList();
            var pastCandidates = candidates.Where(c => c.ElectionId == pastElection.Id && c.Status == "Approved").ToList();

            for (int i = 0; i < 50 && i < verifiedStudents.Count; i++)
            {
                var voter = verifiedStudents[i];

                foreach (var position in pastPositions)
                {
                    var positionCandidates = pastCandidates.Where(c => c.PositionId == position.Id).ToList();
                    if (positionCandidates.Any())
                    {
                        var selectedCandidate = positionCandidates[random.Next(positionCandidates.Count)];

                        var vote = new Vote
                        {
                            ElectionId = pastElection.Id!,
                            PositionId = position.Id!,
                            CandidateId = selectedCandidate.Id!,
                            VoterId = voter.Id!,
                            IpAddress = $"192.168.1.{random.Next(1, 255)}",
                            CreatedAt = DateTime.UtcNow.AddDays(-random.Next(113, 120))
                        };
                        votes.Add(vote);
                    }
                }
            }

            // Votes for ongoing election (20 voters so far)
            var ongoingPositions = allPositions.Where(p => p.ElectionId == ongoingElection.Id).ToList();
            var ongoingCandidates = candidates.Where(c => c.ElectionId == ongoingElection.Id && c.Status == "Approved").ToList();

            for (int i = 50; i < 70 && i < verifiedStudents.Count; i++)
            {
                var voter = verifiedStudents[i];

                foreach (var position in ongoingPositions)
                {
                    var positionCandidates = ongoingCandidates.Where(c => c.PositionId == position.Id).ToList();
                    if (positionCandidates.Any())
                    {
                        var selectedCandidate = positionCandidates[random.Next(positionCandidates.Count)];

                        var vote = new Vote
                        {
                            ElectionId = ongoingElection.Id!,
                            PositionId = position.Id!,
                            CandidateId = selectedCandidate.Id!,
                            VoterId = voter.Id!,
                            IpAddress = $"192.168.1.{random.Next(1, 255)}",
                            CreatedAt = DateTime.UtcNow.AddHours(-random.Next(1, 48))
                        };
                        votes.Add(vote);
                    }
                }
            }

            await dbContext.Votes.InsertManyAsync(votes);
            Console.WriteLine($"   ✓ Created {votes.Count} votes");
            Console.WriteLine($"   ✓ Past election: 50 voters, Ongoing: 20 voters");

            // ===== STEP 7: Create Audit Logs =====
            Console.WriteLine("\n[7/7] Creating audit logs...");

            var auditLogs = new List<AuditLog>();

            // Sample audit entries
            foreach (var student in verifiedStudents.Take(10))
            {
                auditLogs.Add(new AuditLog
                {
                    UserId = student.Id!,
                    UserName = student.Name,
                    Action = "Login",
                    EntityType = "User",
                    EntityId = student.Id!,
                    Details = "User logged in successfully",
                    IpAddress = $"192.168.1.{random.Next(1, 255)}",
                    CreatedAt = DateTime.UtcNow.AddHours(-random.Next(1, 72))
                });
            }

            await dbContext.AuditLogs.InsertManyAsync(auditLogs);
            Console.WriteLine($"   ✓ Created {auditLogs.Count} audit log entries");

            // ===== SUMMARY =====
            Console.WriteLine("\n========================================");
            Console.WriteLine("  SEEDING COMPLETE - SUMMARY");
            Console.WriteLine("========================================");
            Console.WriteLine($"✓ Students: {students.Count}");
            Console.WriteLine($"  - Verified: {students.Count(s => s.IsVerified)}");
            Console.WriteLine($"  - Pending verification: {students.Count(s => s.VerificationRequested && !s.IsVerified)}");
            Console.WriteLine($"  - Unverified: {students.Count(s => !s.IsVerified && !s.VerificationRequested)}");
            Console.WriteLine($"\n✓ Elections: {elections.Count}");
            Console.WriteLine($"  - Past: 1, Ongoing: 1, Upcoming: 1");
            Console.WriteLine($"\n✓ Positions: {allPositions.Count}");
            Console.WriteLine($"\n✓ Candidates: {candidates.Count}");
            Console.WriteLine($"  - Approved: {candidates.Count(c => c.Status == "Approved")}");
            Console.WriteLine($"  - Pending: {candidates.Count(c => c.Status == "Pending")}");
            Console.WriteLine($"  - Rejected: {candidates.Count(c => c.Status == "Rejected")}");
            Console.WriteLine($"\n✓ Votes: {votes.Count}");
            Console.WriteLine($"\n✓ Verification Requests: {verificationRequests.Count}");
            Console.WriteLine($"\n✓ Audit Logs: {auditLogs.Count}");
            Console.WriteLine("========================================");
            Console.WriteLine("\nTEST ACCOUNTS:");
            Console.WriteLine("  Admin: 00-00000 / admin123");
            Console.WriteLine("  Student (verified): 24-00001 / student123");
            Console.WriteLine("  Student (verified): 24-00010 / student123");
            Console.WriteLine("  Student (pending): 24-00065 / student123");
            Console.WriteLine("  Student (unverified): 24-00085 / student123");
            Console.WriteLine("========================================\n");
        }

        private static string GenerateRandomBio(string name, string position, Random random)
        {
            var templates = new[]
            {
                $"I am {name} and I am running for {position}. With my experience in student leadership and dedication to our school community, I promise to bring positive change and represent your voices effectively.",
                $"As a candidate for {position}, I bring fresh ideas and strong commitment to student welfare. My goal is to enhance campus life and ensure every student's concerns are heard and addressed.",
                $"Hello! I'm {name}, your candidate for {position}. I believe in transparent leadership and active student engagement. Together, we can make our school a better place for everyone.",
                $"Vote for {name} as your {position}! I have a proven track record of organization skills and teamwork. I will work tirelessly to improve student facilities and academic support services.",
                $"I am passionate about student rights and campus improvement. As {position}, I will focus on better communication between students and administration, more events, and enhanced learning resources.",
                $"My name is {name} and I'm excited to run for {position}. My platform focuses on sustainability, inclusivity, and creating more opportunities for student involvement in decision-making processes."
            };

            return templates[random.Next(templates.Length)];
        }
    }
}
