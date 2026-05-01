using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MongoDB.Driver;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using VotingSystem.Data;
using VotingSystem.Models;

namespace VotingSystem.Services
{
    public class AuthService
    {
        private readonly MongoDbContext _dbContext;
        private readonly AuditService _auditService;

        public AuthService(MongoDbContext dbContext, AuditService auditService)
        {
            _dbContext = dbContext;
            _auditService = auditService;
        }

        // Hash password using SHA256 (for simplicity; in production, use PBKDF2 or bcrypt)
        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        public bool VerifyPassword(string password, string hash)
        {
            var newHash = HashPassword(password);
            return newHash == hash;
        }

        public async Task<User?> AuthenticateAsync(string studentNumber, string password)
        {
            var user = await _dbContext.Users
                .Find(u => u.StudentNumber == studentNumber && u.IsActive)
                .FirstOrDefaultAsync();

            if (user == null)
                return null;

            // Check lockout
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                return null;

            if (!VerifyPassword(password, user.PasswordHash))
            {
                // Increment failed login attempts
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                }
                await _dbContext.Users.ReplaceOneAsync(u => u.Id == user.Id, user);
                return null;
            }

            // Reset failed attempts on successful login
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.LastLoginAt = DateTime.UtcNow;
            await _dbContext.Users.ReplaceOneAsync(u => u.Id == user.Id, user);

            return user;
        }

        public async Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("StudentNumber", user.StudentNumber),
                new Claim("IsVerified", user.IsVerified.ToString())
            };

            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }

        public async Task<bool> RegisterUserAsync(RegisterViewModel model)
        {
            // Check if email or student number already exists
            var existingUser = await _dbContext.Users
                .Find(u => u.Email == model.Email || u.StudentNumber == model.StudentNumber)
                .FirstOrDefaultAsync();

            if (existingUser != null)
                return false;

            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                StudentNumber = model.StudentNumber,
                PasswordHash = HashPassword(model.Password),
                Birthday = model.Birthday,
                Sex = model.Sex,
                YearSection = model.YearSection,
                Roles = new List<string> { "Student" },
                IsVerified = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Users.InsertOneAsync(user);
            await _auditService.LogAsync(user.Id!, user.Name, "UserRegistered", "User", user.Id!, "User registered successfully");

            return true;
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            return await _dbContext.Users
                .Find(u => u.Id == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            var result = await _dbContext.Users.ReplaceOneAsync(u => u.Id == user.Id, user);
            return result.ModifiedCount > 0;
        }
    }
}
