using MongoDB.Bson.Serialization.Attributes;

namespace Sheessential_Sales_Finance.Models
{
    public class Address
    {
        [BsonElement("street")]
        public required string Street { get; set; }

        [BsonElement("city")]
        public required string City { get; set; }

        [BsonElement("province")]
        public required string Province { get; set; }

        [BsonElement("postalCode")]
        public required string PostalCode { get; set; }

        [BsonElement("country")]
        public required string Country { get; set; }
    }
}
