using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class IngredientStockRequests
{
    // Primary ID
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    // Foreign Keys / References
    [BsonRepresentation(BsonType.ObjectId)]

    [BsonElement("ingredientID")]
    public ObjectId IngredientId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("supplierID")]
    public ObjectId SupplierId { get; set; }    
    
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("ExpenseId")]
    public ObjectId? ExpenseId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]

    [BsonElement("requestedByUserId")]
    public ObjectId RequestedByUserId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]

    [BsonElement("processedByUserId")]
    public ObjectId ProcessedByUserId { get; set; }

    // Quantity and Unit (Stored as string in the provided JSON)
    [BsonElement("quantityRequested")]
    public int QuantityRequested { get; set; }

    [BsonElement("unit")]
    public string Unit { get; set; }

    // Date/Time Fields
    [BsonElement("requestDate")]
    public DateTime? RequestDate { get; set; }

    [BsonElement("expectedDeliveryDate")]
    public DateTime? ExpectedDeliveryDate { get; set; }

    [BsonElement("actualDeliveryDate")]
    public DateTime? ActualDeliveryDate { get; set; } // Nullable as it is null in the example

    [BsonElement("statusUpdatedDate")]
    public DateTime? StatusUpdatedDate { get; set; }

    [BsonElement("emailSentDate")]
    public DateTime? EmailSentDate { get; set; }

    [BsonElement("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    // Status and User Information
    [BsonElement("requestedBy")]
    public string RequestedBy { get; set; }

    [BsonElement("requestStatus")]
    public string RequestStatus { get; set; }

    [BsonElement("instructions")]
    public string Instructions { get; set; }

    [BsonElement("processedBy")]
    public string ProcessedBy { get; set; }

    [BsonElement("rejectionReason")]
    public string RejectionReason { get; set; }

    [BsonElement("priority")]
    public string Priority { get; set; }

    // Financial Fields (Use Decimal128 for precision)
    [BsonElement("totalCost")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TotalCost { get; set; }

    [BsonElement("unitPrice")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal UnitPrice { get; set; }

    // Stock Levels (Stored as string in the provided JSON)
    [BsonElement("currentStockAtRequest")]
    public int CurrentStockAtRequest { get; set; }

    [BsonElement("minimumStockLevel")]
    public int MinimumStockLevel { get; set; }

    // Boolean Flags
    [BsonElement("emailSent")]
    public bool EmailSent { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; }
}