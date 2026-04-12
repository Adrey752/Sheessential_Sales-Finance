using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class InventoryUser
{
    // Primary ID
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    // User Details
    [BsonElement("name")]
    public string Name { get; set; }

    [BsonElement("email")]
    public string Email { get; set; }

    [BsonElement("role")]
    public string Role { get; set; }

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; }

    [BsonElement("shortPass")]
    public string ShortPass { get; set; }

    // Face Recognition Fields
    // Note: Nullable string is used for faceHash as it is null in the example
    [BsonElement("faceEncoding")]
    public string FaceEncoding { get; set; }

    [BsonElement("faceHash")]
    public string? FaceHash { get; set; }

    // Date/Time Fields
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("lastLogin")]
    public DateTime? LastLogin { get; set; } // Nullable as it may not exist yet

    // Boolean Flag
    [BsonElement("isActive")]
    public bool IsActive { get; set; }
}