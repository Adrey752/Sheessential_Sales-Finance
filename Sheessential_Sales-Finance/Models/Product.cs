using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Sheessential_Sales_Finance.Models
{
    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("productName")]
        public string ProductName { get; set; } = string.Empty;

        [BsonElement("productDesc")]
        public string ProductDesc { get; set; } = string.Empty;

        [BsonElement("productCategory")]
        public string ProductCategory { get; set; } = string.Empty;

        [BsonElement("baseIngredients")]
        public string BaseIngredients { get; set; } = string.Empty;

        [BsonElement("productImg")]
        public string ProductImg { get; set; } = string.Empty;

        [BsonElement("productVal")]
        public decimal ProductVal { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = string.Empty;

        [BsonElement("isApprove")]
        public bool IsApprove { get; set; }

        [BsonElement("ProductImage")]
        public string ProductImage { get; set; } = string.Empty;
    }
}
