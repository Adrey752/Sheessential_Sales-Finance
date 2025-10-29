using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Sheessential_Sales_Finance.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("resetToken")]
        public string? ResetToken { get; set; }

        [BsonElement("resetTokenExpiry")]
        public DateTime? ResetTokenExpiry { get; set; }

        [BsonElement("firstName")]
        public required string FirstName { get; set; }

        [BsonElement("lastName")]
        public required string LastName { get; set; }

        [BsonElement("email")]
        public required string Email { get; set; }

        [BsonElement("passwordHash")]
        public required string Password { get; set; }

        [BsonElement("role")]
        public string Role { get; set; } = "User    "; 

        [BsonElement("phone")]
        public required string Phone { get; set; }

        [BsonElement("address")]
        public required Address Address { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now; 

        [BsonElement("lastLogin")]
        public DateTime LastLogin { get; set; } = DateTime.Now;

        [BsonElement("status")]
        public string Status { get; set; } = "Active";

        [BsonIgnore]
        public string FullName => $"{FirstName} {LastName}";
    }
}
