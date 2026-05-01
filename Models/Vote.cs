using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace VotingSystem.Models
{
    public class Vote
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [Required]
        public string ElectionId { get; set; } = string.Empty;

        [Required]
        public string PositionId { get; set; } = string.Empty;

        [Required]
        public string CandidateId { get; set; } = string.Empty;

        [Required]
        public string VoterId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // For audit purposes (IP address, user agent, etc.)
        public string IpAddress { get; set; } = string.Empty;
    }

    public class VoteReceipt
    {
        public string ElectionId { get; set; } = string.Empty;
        public string ElectionTitle { get; set; } = string.Empty;
        public List<VoteReceiptItem> Votes { get; set; } = new();
        public DateTime SubmittedAt { get; set; }
    }

    public class VoteReceiptItem
    {
        public string PositionTitle { get; set; } = string.Empty;
        public string CandidateName { get; set; } = string.Empty;
    }

    public class VotingStatus
    {
        public string ElectionId { get; set; } = string.Empty;
        public string PositionId { get; set; } = string.Empty;
        public string PositionTitle { get; set; } = string.Empty;
        public bool HasVoted { get; set; }
        public DateTime? VotedAt { get; set; }
    }
}
