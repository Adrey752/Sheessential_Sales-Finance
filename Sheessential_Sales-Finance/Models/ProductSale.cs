using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Sheessential_Sales_Finance.Models
{
    public class ProductSale
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("productId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public required string ProductId { get; set; }
      
        [BsonElement("quantity")]
        public required int Quantity { get; set; }

        [BsonElement("salePrice")]
        public required decimal SalePrice { get; set; }

        [BsonElement("saleTax")]
        public required decimal SaleTax { get; set; }

        [BsonElement("saleDiscounts")]
        public required decimal SaleDiscounts { get; set; }

        [BsonElement("transactionDate")]
        public required DateTime TransactionDate { get; set; }

        [BsonElement("srp")]
        public required decimal SRP { get; set; }
    }
}
