using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace Sheessential_Sales_Finance.Models
{
    // --- Nested Model for Items Array ---
    public class OrderItem
    {
        [BsonElement("product_id")]
        public string ProductId { get; set; } = string.Empty;

        [BsonElement("product_name")]
        public string ProductName { get; set; } = string.Empty;

        [BsonElement("product_image")]
        public string ProductImage { get; set; } = string.Empty;

        [BsonElement("quantity")]
        public int Quantity { get; set; }

        [BsonElement("price")]
        public decimal Price { get; set; }

        [BsonElement("subtotal")]
        public decimal Subtotal { get; set; }
    }

    // --- Nested Model for Shipping Address ---
    public class ShippingAddress
    {
        [BsonElement("first_name")]
        public string FirstName { get; set; } = string.Empty;

        [BsonElement("middle_name")]
        public string MiddleName { get; set; } = string.Empty;

        [BsonElement("last_name")]
        public string LastName { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("phone")]
        public string Phone { get; set; } = string.Empty;

        [BsonElement("street")]
        public string Street { get; set; } = string.Empty;

        [BsonElement("apartment")]
        public string Apartment { get; set; } = string.Empty;

        [BsonElement("city")]
        public string City { get; set; } = string.Empty;

        [BsonElement("state")]
        public string State { get; set; } = string.Empty;

        [BsonElement("zip_code")]
        public string ZipCode { get; set; } = string.Empty;

        [BsonElement("country")]
        public string Country { get; set; } = string.Empty;

        [BsonElement("full_address")]
        public string FullAddress { get; set; } = string.Empty;
    }

    // --- Main Model ---
    public class TbOrder
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("user_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? UserId { get; set; }

        [BsonElement("order_number")]
        public string OrderNumber { get; set; } = string.Empty;

        [BsonElement("items")]
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();

        [BsonElement("shipping_address")]
        public ShippingAddress ShippingAddress { get; set; } = new ShippingAddress();

        [BsonElement("subtotal")]
        public decimal Subtotal { get; set; }

        [BsonElement("shipping_fee")]
        public decimal ShippingFee { get; set; }

        [BsonElement("tax")]
        public decimal Tax { get; set; }

        [BsonElement("total_amount")]
        public decimal TotalAmount { get; set; }

        [BsonElement("payment_method")]
        public string PaymentMethod { get; set; } = string.Empty;

        [BsonElement("payment_status")]
        public string PaymentStatus { get; set; } = string.Empty;

        [BsonElement("order_status")]
        public string OrderStatus { get; set; } = string.Empty;

        [BsonElement("transaction_id")]
        public string TransactionId { get; set; } = string.Empty;

        [BsonElement("created_at")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [BsonElement("is_archived")]
        public bool IsArchive { get; set; } = false;
    }
}