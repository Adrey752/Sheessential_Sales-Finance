using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Sheessential_Sales_Finance.Models
{
    [BsonIgnoreExtraElements]
    public class InventoryProducts
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("productImg")]
        public BsonValue ProductImgRaw { get; set; }

        [BsonElement("productImgString")]
        public string ProductImg { get; set; }

        [BsonIgnore]
        public string? ProductImgAsBase64
        {
            get
            {
                if (ProductImgRaw == null || ProductImgRaw.IsBsonNull) return null;
                if (ProductImgRaw.IsString) return ProductImgRaw.AsString;
                if (ProductImgRaw.IsBsonBinaryData) return Convert.ToBase64String(ProductImgRaw.AsBsonBinaryData.Bytes);
                return null;
            }
        }
        [BsonElement("productName")]
        public string ProductName { get; set; }

        [BsonElement("productDesc")]
        public string ProductDesc { get; set; }

        [BsonElement("productCategory")]
        public string ProductCategory { get; set; }

        [BsonElement("baseIngredients")]
        public string BaseIngredients { get; set; }

        [BsonElement("productVal")]
        public int ProductVal { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("isApprove")]
        public bool IsApprove { get; set; }

        [BsonElement("ProductImage")]
        public string ProductImage { get; set; }

    }
}
