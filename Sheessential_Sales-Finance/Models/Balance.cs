using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Sheessential_Sales_Finance.Models
{
    public class Balance
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Bank information
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string AccountHolder { get; set; }

        // Current balance
        public decimal CurrentBalance { get; set; }

        // Optional: track when balance was last updated
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Optional: currency
        public string Currency { get; set; } = "PHP";
    }
}
