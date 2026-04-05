using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Sheessential_Sales_Finance.Models
{
    [BsonIgnoreExtraElements]
    public class PayrollSnapshot
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("employee_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string EmployeeId { get; set; } = string.Empty;

        [BsonElement("employee_number")]
        public string EmployeeNumber { get; set; } = string.Empty;

        [BsonElement("full_name")]
        public string FullName { get; set; } = string.Empty;

        [BsonElement("department")]
        public string Department { get; set; } = string.Empty;

        [BsonElement("basic_salary")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BasicSalary { get; set; }

        [BsonElement("gross_pay")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal GrossPay { get; set; }

        [BsonElement("net_pay")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal NetPay { get; set; }

        [BsonElement("housing_allowance")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal HousingAllowance { get; set; }

        [BsonElement("transport_allowance")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TransportAllowance { get; set; }

        [BsonElement("meal_allowance")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal MealAllowance { get; set; }

        [BsonElement("other_allowances")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal OtherAllowances { get; set; }

        [BsonElement("total_overtime")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalOvertime { get; set; }

        [BsonElement("excess_days_pay")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ExcessDaysPay { get; set; }

        [BsonElement("holiday_pay")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal HolidayPay { get; set; }

        [BsonElement("special_day_pay")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal SpecialDayPay { get; set; }

        [BsonElement("sss_deduction")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal SssDeduction { get; set; }

        [BsonElement("philhealth_deduction")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PhilhealthDeduction { get; set; }

        [BsonElement("pagibig_deduction")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PagibigDeduction { get; set; }

        [BsonElement("withholding_tax")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal WithholdingTax { get; set; }

        [BsonElement("absence_deduction")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal AbsenceDeduction { get; set; }

        [BsonElement("total_loans")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalLoans { get; set; }

        [BsonElement("total_penalties")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalPenalties { get; set; }

        [BsonElement("total_deductions")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalDeductions { get; set; }

        [BsonElement("days_worked")]
        public int DaysWorked { get; set; }

        [BsonElement("days_present")]
        public int DaysPresent { get; set; }

        [BsonElement("days_absent")]
        public int DaysAbsent { get; set; }

        [BsonElement("pay_period_start")]
        public DateTime PayPeriodStart { get; set; }

        [BsonElement("pay_period_end")]
        public DateTime PayPeriodEnd { get; set; }

        [BsonElement("processed_at")]
        public DateTime ProcessedAt { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = string.Empty;
    }
}