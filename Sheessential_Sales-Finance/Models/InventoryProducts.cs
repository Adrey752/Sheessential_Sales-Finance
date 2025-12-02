using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Sheessential_Sales_Finance.Models
{
    public class InventoryProducts
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("productName")]
        public string ProductName { get; set; }

        [BsonElement("productDesc")]
        public string ProductDesc { get; set; }

        [BsonElement("productCategory")]
        public string ProductCategory { get; set; }

        [BsonElement("baseIngredients")]
        public string BaseIngredients { get; set; }

        [BsonElement("productImg")]
        public string ProductImg { get; set; }

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
