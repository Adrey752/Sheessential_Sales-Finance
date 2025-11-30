namespace Sheessential_Sales_Finance.Models
{
    namespace Sheessential_Sales_Finance.Models
    {
        using MongoDB.Bson;
        using MongoDB.Bson.Serialization.Attributes;
        using System;

        // ==========================================
        // 1. The Employee Profile (Static Data)
        // ==========================================
        public class Employee
        {
            [BsonId]
            [BsonRepresentation(BsonType.ObjectId)]
            public string Id { get; set; }

            [BsonElement("departmentId")]
            [BsonRepresentation(BsonType.ObjectId)]
            public string DepartmentId { get; set; }


            [BsonElement("position")]
            public string Position { get; set; }   
            
            [BsonElement("firstName")]
            public string FirstName { get; set; }

            [BsonElement("lastName")]
            public string LastName { get; set; }

            [BsonElement("dateOfBirth")]
            public DateTime Birthdate { get; set; }

            [BsonElement("hourlyRate")]
            public decimal HourlyRate { get; set; }

            [BsonElement("statutoryIds")]
            public StatutoryDetails GovernmentIds { get; set; }
        }

        public class StatutoryDetails
        {
            public string? SssNumber { get; set; }
            public string? TinNumber { get; set; }
            public string? PagIbigNumber { get; set; } // Note: PagIbigNumber was missing but is critical
            public string? PhilHealthNumber { get; set; }
        }
    }

}
