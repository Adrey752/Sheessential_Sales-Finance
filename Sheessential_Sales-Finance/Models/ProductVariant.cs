using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Sheessential_Sales_Finance.Models
{
    public class ProductVariant
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("productId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ProductId { get; set; } = string.Empty;

        [BsonElement("variantName")]
        public string VariantName { get; set; } = string.Empty;

        [BsonElement("size")]
        public string Size { get; set; } = string.Empty;

        [BsonElement("color")]
        public string Color { get; set; } = string.Empty;        
        
        public string? Category { get; set; } = string.Empty;

        [BsonElement("sku")]
        public string SKU { get; set; } = string.Empty;

        [BsonElement("price")]
        [BsonIgnoreIfNull]
        public decimal Price { get; set; }
        public int OrdersCount { get; set; }

        [BsonElement("stockQuantity")]
        public int StockQuantity { get; set; }        
        
        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("minimumStock")]
        public int MinimumStock { get; set; }

        [BsonElement("weight")]
        [BsonIgnoreIfNull]
        public decimal Weight { get; set; }

        [BsonElement("dimensions")]
        public string Dimensions { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; }

        [BsonElement("variantImg")]
        public string VariantImg { get; set; } = string.Empty;

        [BsonElement("shelfLifeYears")]
        [BsonIgnoreIfNull]
        public int ShelfLifeYears { get; set; }

        [BsonElement("location")]
        public string Location { get; set; } = string.Empty;

        [BsonElement("isArchived")]
        public bool IsArchive { get; set; } = false;

        [BsonElement("variantImgUrls")]
        public List<string> ImgUrls { get; set; } = new List<string>(); // Fixed naming, nullability, and initialization
    }
}