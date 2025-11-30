namespace Sheessential_Sales_Finance.Models
{
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    public class Department
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("departmentName")]
        public string DepartmentName { get; set; }
    }

}
