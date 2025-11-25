using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Sheessential_Sales_Finance.Models
{
    // Nested class for the embedded Address document
    public class UserAddress
    {
        [BsonElement("street")]
        public string Street { get; set; } = string.Empty;

        [BsonElement("city")]
        public string City { get; set; } = string.Empty;

        [BsonElement("state")]
        public string State { get; set; } = string.Empty;

        [BsonElement("country")]
        public string Country { get; set; } = string.Empty;

        [BsonElement("latitude")]
        public double? Latitude { get; set; }

        [BsonElement("longitude")]
        public double? Longitude { get; set; }

        [BsonElement("full_address")]
        public string FullAddress { get; set; } = string.Empty;
    }

    public class TbUser
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; } // Maps to _id

        [BsonElement("first_name")]
        public string FirstName { get; set; } = string.Empty;

        [BsonElement("middle_name")]
        public string? MiddleName { get; set; } // Can be null

        [BsonElement("last_name")]
        public string LastName { get; set; } = string.Empty;

        [BsonElement("address")]
        public UserAddress Address { get; set; } = new UserAddress(); // Embedded document

        [BsonElement("zip_code")]
        public string ZipCode { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("phone")]
        public string Phone { get; set; } = string.Empty;

        [BsonElement("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [BsonElement("is_email_verified")]
        public bool IsEmailVerified { get; set; }

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [BsonElement("terms_accepted")]
        public bool TermsAccepted { get; set; }

        [BsonElement("role")]
        public string Role { get; set; } = string.Empty;

        [BsonElement("employee_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? EmployeeId { get; set; } // Maps to employee_id (null or ObjectId)

        [BsonElement("department")]
        public string? Department { get; set; } // Can be null
    }
}