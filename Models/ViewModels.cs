using System.ComponentModel.DataAnnotations;

namespace VotingSystem.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Student Number is required")]
        [Display(Name = "Student Number")]
        public string StudentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Birthday is required")]
        [DataType(DataType.Date)]
        public DateTime Birthday { get; set; }

        [Required(ErrorMessage = "Sex is required")]
        public string Sex { get; set; } = string.Empty;

        [Required(ErrorMessage = "Year/Section is required")]
        [Display(Name = "Year/Section")]
        public string YearSection { get; set; } = string.Empty;

        [Required(ErrorMessage = "Student Number is required")]
        [Display(Name = "Student Number")]
        [RegularExpression(@"^\d{2}-\d{5}$", ErrorMessage = "Student Number must be in format YY-#####")]
        public string StudentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Student Number is required")]
        [Display(Name = "Student Number")]
        public string StudentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class VoteSubmissionViewModel
    {
        public string ElectionId { get; set; } = string.Empty;
        public Dictionary<string, string> PositionVotes { get; set; } = new(); // PositionId -> CandidateId
        public bool AcceptTerms { get; set; }
    }

    public class CandidateApplicationViewModel
    {
        [Required]
        public string ElectionId { get; set; } = string.Empty;

        [Required]
        public string PositionId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bio is required")]
        [MinLength(50, ErrorMessage = "Bio must be at least 50 characters")]
        [MaxLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        public string Bio { get; set; } = string.Empty;

        public string PhotoUrl { get; set; } = string.Empty;
    }

    public class ElectionDetailsViewModel
    {
        public Election Election { get; set; } = null!;
        public List<PositionWithCandidates> Positions { get; set; } = new();
        public Dictionary<string, bool> VotingStatus { get; set; } = new(); // PositionId -> HasVoted
        public bool CanVote { get; set; }
        public bool IsVerified { get; set; }
    }

    public class PositionWithCandidates
    {
        public Position Position { get; set; } = null!;
        public List<Candidate> Candidates { get; set; } = new();
    }

    public class DashboardViewModel
    {
        public User User { get; set; } = null!;
        public List<Election> OngoingElections { get; set; } = new();
        public List<Election> UpcomingElections { get; set; } = new();
        public List<Election> PastElections { get; set; } = new();
    }

    public class AdminDashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int VerifiedStudents { get; set; }
        public int PendingVerifications { get; set; }
        public int ActiveElections { get; set; }
        public int TotalElections { get; set; }
        public int PendingCandidates { get; set; }
        public List<Election> RecentElections { get; set; } = new();
    }
}
