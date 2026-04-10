using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

[BsonIgnoreExtraElements]
public class Ingredient
{
    // Primary ID
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    // Ingredient Details
    [BsonElement("ingredientName")]
    public string IngredientName { get; set; }

    [BsonElement("unit")]
    public string Unit { get; set; }

    [BsonElement("sku")]
    public string Sku { get; set; }

    // Financial/Stock Fields (Use Decimal128 for precision)
    [BsonElement("costPerUnit")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal CostPerUnit { get; set; }

    [BsonElement("currentStock")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal CurrentStock { get; set; }

    [BsonElement("minimumStock")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal MinimumStock { get; set; }

    // Reference
    [BsonElement("supplierId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string SupplierId { get; set; }

    // Date/Time Fields
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    // Boolean Flag
    [BsonElement("isActive")]
    public bool IsActive { get; set; }
}