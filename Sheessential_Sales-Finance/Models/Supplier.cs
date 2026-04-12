using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Supplier
{
    // Primary ID
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    // Supplier Details
    [BsonElement("supName")]
    public string SupplierName { get; set; } // Renamed to SupplierName for clarity

    [BsonElement("supContactPer")]
    public string ContactPerson { get; set; } // Renamed to ContactPerson for clarity

    [BsonElement("supAddress")]
    public string Address { get; set; } // Renamed to Address for clarity

    [BsonElement("supContactNo")]
    public string ContactNo { get; set; } // Renamed to ContactNo for clarity

    [BsonElement("supEmail")]
    public string Email { get; set; } // Renamed to Email for clarity

    // Date/Time Fields
    [BsonElement("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    // Boolean Flag
    [BsonElement("isActive")]
    public bool IsActive { get; set; }
}