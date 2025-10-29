using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Sheessential_Sales_Finance.Models
{
    public class Vendor
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("vendorId")]
        public string VendorId { get; set; } = string.Empty;

        [BsonElement("vendorName")]
        public string VendorName { get; set; } = string.Empty;

        [BsonElement("companyName")]
        public string CompanyName { get; set; } = string.Empty;

        [BsonElement("address")]
        public string Address { get; set; } = string.Empty;

        [BsonElement("contactPerson")]
        public string ContactPerson { get; set; } = string.Empty;

        [BsonElement("contactNumber")]
        public string ContactNumber { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("tin")]
        public string Tin { get; set; } = string.Empty;

        [BsonElement("paymentTerms")]
        public string PaymentTerms { get; set; } = string.Empty;

        [BsonElement("notes")]
        public string Notes { get; set; } = string.Empty;

        [BsonElement("status")]
        public string Status { get; set; } = "Active";

        [BsonElement("totalPurchases")]
        public decimal TotalPurchases { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [BsonElement("IsArchived")]
        public bool IsArchived { get; set; } = false;

    }
}
