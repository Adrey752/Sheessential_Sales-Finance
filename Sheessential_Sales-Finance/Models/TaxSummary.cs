namespace Sheessential_Sales_Finance.Models
{
    using MongoDB.Bson.Serialization.Attributes;

    public class TaxSummary
    {
        [BsonElement("grossPay")]
        public decimal GrossPay { get; set; }

        [BsonElement("sssDeduction")]
        public decimal SssDeduction { get; set; }

        [BsonElement("pagibigDeduction")]
        public decimal PagIbigDeduction { get; set; }

        [BsonElement("philhealthDeduction")]
        public decimal PhilHealthDeduction { get; set; }

        // Other Deductions (e.g., loans, company benefits)
        [BsonElement("otherDeductions")]
        public decimal OtherDeductions { get; set; } = 0.00m; // Initialize to zero

        // Tax Components
        [BsonElement("totalContributions")]
        public decimal TotalContributions { get; set; } // SSS + PagIbig + PhilHealth

        [BsonElement("taxableIncome")]
        public decimal TaxableIncome { get; set; }

        [BsonElement("birTaxDue")]
        public decimal BirTaxDue { get; set; }

        // Final Pay
        [BsonElement("totalDeductions")]
        public decimal TotalDeductions { get; set; } // Total Contributions + BIR Tax + Other Deductions

        [BsonElement("netPay")]
        public decimal NetPay { get; set; }
    }
}
