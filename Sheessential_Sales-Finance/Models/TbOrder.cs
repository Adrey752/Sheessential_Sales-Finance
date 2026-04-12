using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Sheessential_Sales_Finance.Models
{
    [BsonIgnoreExtraElements] // ignore unexpected fields like "discount" when deserializing
    public class TbOrder
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string? Id { get; set; }

        [BsonElement("user_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? UserId { get; set; }

        [BsonElement("order_number")]
        public string? OrderNumber { get; set; }

        [BsonElement("items")]
        public List<OrderItem> Items { get; set; } = new();

        [BsonElement("shipping_address")]
        public ShippingAddress? ShippingAddress { get; set; }

        [BsonElement("subtotal")]
        public decimal? Subtotal { get; set; }

        [BsonElement("shipping_fee")]
        public decimal? ShippingFee { get; set; }

        [BsonElement("discount")]
        public decimal? Discount { get; set; }

        [BsonElement("tax")]
        public decimal? Tax { get; set; }

        [BsonElement("total_amount")]
        public decimal TotalAmount { get; set; }

        [BsonElement("payment_method")]
        public string? PaymentMethod { get; set; }

        [BsonElement("payment_status")]
        public string? PaymentStatus { get; set; }

        [BsonElement("order_status")]
        public string? OrderStatus { get; set; }

        [BsonElement("transaction_id")]
        public string? TransactionId { get; set; }

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Map your archived flag (documents use is_archived)
        [BsonElement("is_archived")]
        public bool IsArchive { get; set; } = false;
    }

    public class OrderItem
    {
        [BsonElement("product_id")]
        [BsonRepresentation(BsonType.String)]
        public string? ProductId { get; set; }

        [BsonElement("product_name")]
        public string? ProductName { get; set; }

        [BsonElement("product_image")]
        public string? ProductImage { get; set; }

        [BsonElement("quantity")]
        public int Quantity { get; set; }

        [BsonElement("price")]
        public decimal Price { get; set; }

        [BsonElement("subtotal")]
        public decimal? Subtotal { get; set; }
    }

    public class ShippingAddress
    {
        [BsonElement("first_name")]
        public string? FirstName { get; set; }

        [BsonElement("middle_name")]
        public string? MiddleName { get; set; }

        [BsonElement("last_name")]
        public string? LastName { get; set; }

        [BsonElement("email")]
        public string? Email { get; set; }

        [BsonElement("phone")]
        public string? Phone { get; set; }

        [BsonElement("street")]
        public string? Street { get; set; }

        [BsonElement("apartment")]
        public string? Apartment { get; set; }

        [BsonElement("city")]
        public string? City { get; set; }

        [BsonElement("state")]
        public string? State { get; set; }

        [BsonElement("zip_code")]
        public string? ZipCode { get; set; }

        [BsonElement("country")]
        public string? Country { get; set; }

        [BsonElement("full_address")]
        public string? FullAddress { get; set; }
    }
}