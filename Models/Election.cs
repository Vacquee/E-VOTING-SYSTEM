using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace VotingSystem.Models
{
    public class Election
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartAt { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime EndAt { get; set; }

        // draft, published, closed
        public string Status { get; set; } = "draft";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        // Computed property to determine current status
        [BsonIgnore]
        public string ComputedStatus
        {
            get
            {
                if (Status == "draft") return "Draft";
                if (Status == "closed") return "Closed";

                var now = DateTime.UtcNow;
                if (now < StartAt) return "Upcoming";
                if (now >= StartAt && now <= EndAt) return "Ongoing";
                return "Past";
            }
        }

        [BsonIgnore]
        public bool IsOngoing => ComputedStatus == "Ongoing";

        [BsonIgnore]
        public bool IsPast => ComputedStatus == "Past" || Status == "closed";
    }

    public class Position
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [Required]
        public string ElectionId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int Order { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Candidate
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [Required]
        public string ElectionId { get; set; } = string.Empty;

        [Required]
        public string PositionId { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string StudentNumber { get; set; } = string.Empty;

        public string Bio { get; set; } = string.Empty;

        public string PhotoUrl { get; set; } = string.Empty;

        // Pending, Approved, Rejected
        public string Status { get; set; } = "Pending";

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        public string? ApprovedBy { get; set; }

        public string? RejectionReason { get; set; }
    }
}
