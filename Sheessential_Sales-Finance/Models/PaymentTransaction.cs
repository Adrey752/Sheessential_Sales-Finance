using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Sheessential_Sales_Finance.Models
{
    public class PaymentTransaction
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("expenseId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ExpenseId { get; set; } = string.Empty; // Links back to the specific expense

        [BsonElement("amount")]
        public decimal Amount { get; set; }

        [BsonElement("paymentDate")]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [BsonElement("paymentMethod")]
        public string PaymentMethod { get; set; } = string.Empty; // Cash, Bank Transfer, etc.

        [BsonElement("transferTo")]
        public string TransferTo { get; set; } = string.Empty; // Person or Account Name

        [BsonElement("referenceNumber")]
        public string ReferenceNumber { get; set; } = string.Empty;

        [BsonElement("notes")]
        public string Notes { get; set; } = string.Empty;
    }
}