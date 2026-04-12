namespace Sheessential_Sales_Finance.Models
{
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;
    using System;
    using System.Collections.Generic;

    public class Payroll
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("employeeId")]
        public string EmployeeId { get; set; }

        [BsonElement("payPeriodStart")]
        public DateTime PayPeriodStart { get; set; }

        [BsonElement("payPeriodEnd")]
        public DateTime PayPeriodEnd { get; set; }

        [BsonElement("hoursWorked")]
        public decimal HoursWorked { get; set; }

        [BsonElement("hourlyRate")]
        public decimal HourlyRate { get; set; }

        [BsonElement("overtimeHours")]
        public decimal OvertimeHours { get; set; }

        [BsonElement("grossSalary")]
        public decimal GrossSalary { get; set; }

        [BsonElement("netSalary")]
        public decimal NetSalary { get; set; }

        [BsonElement("processed")]
        public bool Processed { get; set; }

        [BsonElement("deductions")]
        public TaxSummary Deductions { get; set; }
    }

}
