using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Sheessential_Sales_Finance.Models
{
    

    public class Invoice
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("invoiceNumber")]
        public required string InvoiceNumber { get; set; } = "INvi_00";


        [BsonElement("billedTo")]
        [BsonRepresentation(BsonType.ObjectId)]
        public required string BilledTo { get; set; } // customerId

        [BsonElement("issuedAt")]
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("dueDate")]
        public DateTime? DueDate { get; set; }

        [BsonElement("items")]
        public List<ProductSale> Items { get; set; } = new();

        [BsonElement("subtotal")]
        public decimal Subtotal => Items.Sum(i => i.SalePrice * i.Quantity);

        [BsonElement("tax")]
        public decimal Tax => Items.Sum(i => i.SaleTax);

        [BsonElement("discount")]
        public decimal Discount => Items.Sum(i => i.SaleDiscounts);

        [BsonElement("total")]
        public decimal Total => Subtotal + Tax - Discount;

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public string Status { get; set; } = "Unpaid";

        [BsonElement("notes")]
        public string? Notes { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [BsonElement("is_archived")]
        public bool IsArchived { get; set; } = false;

    }
}
