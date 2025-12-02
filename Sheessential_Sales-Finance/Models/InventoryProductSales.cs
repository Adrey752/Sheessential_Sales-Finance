using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Sheessential_Sales_Finance.Models
{
    public class InventoryProductSales
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // VariantId is the product Id
        [BsonElement("variantId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string VariantId { get; set; }

        [BsonElement("quantity")]
        public int Quantity { get; set; } // int32

        [BsonElement("salePrice")]
        public string SalePrice { get; set; }

        [BsonElement("saleTax")]
        public string SaleTax { get; set; }

        [BsonElement("saleDiscounts")]
        public string SaleDiscounts { get; set; }

        [BsonElement("transactionDate")]
        public DateTime TransactionDate { get; set; }

        [BsonElement("srp")]
        public string SRP { get; set; }
    }
}
