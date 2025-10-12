using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Sheessential_Sales_Finance.Models
{
    public class Inventory
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("item")]
        public required string Item { get; set; }

        [BsonElement("sku")]
        public required string SKU { get; set; } 

        [BsonElement("category")]
        public required string Category { get; set; }

        [BsonElement("stockQuantity")]
        public required int StockQuantity { get; set; }

        [BsonElement("unitPrice")]
        public required decimal UnitPrice { get; set; }

        [BsonElement("srp")]
        public required decimal SRP { get; set; }

        [BsonElement("supplier")]
        public required string Supplier { get; set; }

        [BsonElement("lastUpdated")]
        public required DateTime LastUpdated { get; set; }
    }
}
