using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Sheessential_Sales_Finance.Models
{
    public class ActionLog
    {
        public ActionLog() { }

        public ActionLog(string userId, string entity, string entityId, string action, string description)
        {
            UserId = userId;
            Entity = entity;
            EntityId = entityId;
            Action = action;
            Description = description;
            TimeStamp = DateTime.UtcNow;
        }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("user_id")]
        public  string UserId { get; set; }

        [BsonElement("entity")]
        public  string Entity { get; set; }

        [BsonElement("entity_id")]
        public  string EntityId { get; set; }

        [BsonElement("action")]
        public  string Action { get; set; }

        [BsonElement("description")]
        public string? Description { get; set; }

        [BsonElement("time_stamp")]
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}
