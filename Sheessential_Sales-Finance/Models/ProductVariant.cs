using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization; // <-- added

[BsonIgnoreExtraElements]
public class ProductVariant
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("productId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("variantName")]
    public string VariantName { get; set; } = string.Empty;

    [BsonElement("size")]
    public string Size { get; set; } = string.Empty;

    [BsonElement("color")]
    public string Color { get; set; } = string.Empty;

    public string? Category { get; set; } = string.Empty;

    [BsonElement("sku")]
    public string SKU { get; set; } = string.Empty;

    [BsonElement("price")]
    [BsonIgnoreIfNull]
    public decimal Price { get; set; }
    public int OrdersCount { get; set; }

    [BsonElement("stockQuantity")]
    public int? StockQuantity { get; set; }

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("totalSold")]
    public int? TotalSold { get; set; }

    [BsonElement("minimumStock")]
    public int? MinimumStock { get; set; }

    [BsonElement("weight")]
    [BsonIgnoreIfNull]
    public decimal? Weight { get; set; }

    [BsonElement("dimensions")]
    public string Dimensions { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; }

    [BsonElement("variantImg")]
    public string VariantImg { get; set; } = string.Empty;

    [BsonElement("shelfLifeYears")]
    [BsonIgnoreIfNull]
    public int? ShelfLifeYears { get; set; }

    [BsonElement("location")]
    public string Location { get; set; } = string.Empty;

    [BsonElement("isArchived")]
    public bool IsArchive { get; set; } = false;

    // Accept any BSON values (string, document, array). This prevents deserialization errors
    // when collection contains mixed types.
    [BsonElement("variantImgUrls")]
    [JsonIgnore] // <-- prevent System.Text.Json from trying to serialize BsonValue objects
    public List<BsonValue>? ImgUrlsRaw { get; set; } = new List<BsonValue>();

    // Exposed string view consumers expect. Converts mixed BSON to a list of URLs/strings.
    [BsonIgnore]
    public List<string> ImgUrls
    {
        get
        {
            if (ImgUrlsRaw == null || ImgUrlsRaw.Count == 0)
                return new List<string>();

            var result = new List<string>();

            foreach (var bv in ImgUrlsRaw)
            {
                if (bv == null) continue;

                if (bv.IsString)
                {
                    var s = bv.AsString;
                    if (!string.IsNullOrWhiteSpace(s)) result.Add(s);
                    continue;
                }

                if (bv.IsBsonDocument)
                {
                    var doc = bv.AsBsonDocument;

                    // common property names
                    if (doc.TryGetValue("url", out var urlVal) && urlVal.IsString)
                    {
                        result.Add(urlVal.AsString);
                        continue;
                    }

                    if (doc.TryGetValue("path", out var pathVal) && pathVal.IsString)
                    {
                        result.Add(pathVal.AsString);
                        continue;
                    }

                    if (doc.TryGetValue("src", out var srcVal) && srcVal.IsString)
                    {
                        result.Add(srcVal.AsString);
                        continue;
                    }

                    // single-field document with string value
                    if (doc.ElementCount == 1)
                    {
                        var el = doc.GetElement(0);
                        if (el.Value.IsString)
                        {
                            result.Add(el.Value.AsString);
                            continue;
                        }
                    }

                    // fallback: JSON representation
                    result.Add(doc.ToJson());
                    continue;
                }

                if (bv.IsBsonArray)
                {
                    foreach (var inner in bv.AsBsonArray)
                    {
                        if (inner.IsString && !string.IsNullOrWhiteSpace(inner.AsString))
                            result.Add(inner.AsString);
                    }
                    continue;
                }

                // other types -> fallback to string
                try
                {
                    var s = bv.ToString();
                    if (!string.IsNullOrWhiteSpace(s)) result.Add(s);
                }
                catch { /* ignore */ }
            }

            return result.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
        }
    }
}