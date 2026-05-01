using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace VotingSystem.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Student Number is required")]
        [RegularExpression(@"^\d{2}-\d{5}$", ErrorMessage = "Student Number must be in format YY-#####")]
        public string StudentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "Birthday is required")]
        public DateTime Birthday { get; set; }

        [Required(ErrorMessage = "Sex is required")]
        public string Sex { get; set; } = string.Empty;

        [Required(ErrorMessage = "Year/Section is required")]
        public string YearSection { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new List<string> { "Student" };

        public bool IsVerified { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LockoutEnd { get; set; }

        // Verification request tracking
        public bool VerificationRequested { get; set; } = false;
        public DateTime? VerificationRequestedAt { get; set; }
        public DateTime? VerificationApprovedAt { get; set; }
        public string? VerificationDocumentUrl { get; set; }
    }

    public class VerificationRequest
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public string StudentNumber { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string DocumentUrl { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }

        public string? ProcessedBy { get; set; }

        public string? RejectionReason { get; set; }
    }
}
