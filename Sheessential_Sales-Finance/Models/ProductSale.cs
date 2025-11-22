using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Sheessential_Sales_Finance.Models
{
    public class ProductSale
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("variantId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string VariantId { get; set; } = string.Empty;

        [BsonElement("quantity")]
        public int Quantity { get; set; }
         
        [BsonElement("salePrice")]
        public string SalePrice { get; set; } = string.Empty;

        [BsonElement("saleTax")]
        public string SaleTax { get; set; } = string.Empty;

        [BsonElement("saleDiscounts")]
        public string SaleDiscounts { get; set; } = string.Empty;

        [BsonElement("transactionDate")]
        public DateTime TransactionDate { get; set; }

        [BsonElement("srp")]
        public string SRP { get; set; } = string.Empty;
    }
}
