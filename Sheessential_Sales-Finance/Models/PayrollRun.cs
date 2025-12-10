namespace Sheessential_Sales_Finance.Models
{
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;
    using System;
    using System.Collections.Generic;

    public class PayrollRun
    {
        // Document ID (Primary Key)
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("payslipsGeneratedAt")]
        public DateTime? PayrollDate { get; set; }        
        
        [BsonElement("payslipsGeneratedBy")]
        public string? Paygeneratedby { get; set; }

        // Period & Dates
        [BsonElement("payPeriodStart")]
        public DateTime PayPeriodStart { get; set; }

        [BsonElement("payPeriodEnd")]
        public DateTime PayPeriodEnd { get; set; }

        [BsonElement("payDate")]
        public DateTime PayDate { get; set; }

        [BsonElement("cutoffDate")]
        public DateTime CutoffDate { get; set; }

        // Run Metadata
        [BsonElement("payPeriodType")]
        public string PayPeriodType { get; set; } // e.g., "Semi-Monthly", "Monthly"

        [BsonElement("payRunNumber")]
        public string PayRunNumber { get; set; }        
        
        [BsonElement("paidBy")]
        public string PaidBy { get; set; }        
        
        
        [BsonElement("paidAt")]
        public string PaidAt { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("totalEmployees")]
        public int TotalEmployees { get; set; }    
        
        [BsonElement("isPaid")]
        public bool IsPaid { get; set; }

        // Summary Totals
        [BsonElement("totalGrossSalary")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalGrossSalary { get; set; }

        [BsonElement("totalDeductions")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalDeductions { get; set; }

        [BsonElement("totalNetSalary")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalNetSalary { get; set; }

        [BsonElement("totalOvertimePay")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalOvertimePay { get; set; }

        [BsonElement("totalStatutoryDeductions")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalStatutoryDeductions { get; set; }

        [BsonElement("totalLoanDeductions")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalLoanDeductions { get; set; }

        // Status & Audit
        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("isFinalized")]
        public bool IsFinalized { get; set; }

        [BsonElement("isSentToFinance")]
        public bool IsSentToFinance { get; set; }

        [BsonElement("isPayslipsGenerated")]
        public bool IsPayslipsGenerated { get; set; }

        [BsonElement("reviewedBy")]
        public string? ReviewedBy { get; set; }

        [BsonElement("approvedBy")]
        public string? ApprovedBy { get; set; }        
        
        [BsonElement("approvalComments")]
        public string? ApprovalComments { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("createdBy")]
        public string CreatedBy { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; }

        // EMBEDDED LIST: Individual Payslip Records
        [BsonElement("items")]
        public List<PayslipItem> Items { get; set; }

        [BsonElement("reviewedAt")]
        public DateTime? ReviewedAt { get; set; }
        [BsonElement("approvedAt")]
        public DateTime? ApprovedAt { get; set; }        
        
        [BsonElement("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class PayslipItem
    {
        // Employee References
        [BsonElement("employeeId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string EmployeeId { get; set; }

        [BsonElement("employeeName")]
        public string EmployeeName { get; set; }

        [BsonElement("department")]
        public string Department { get; set; }        
        
        [BsonElement("daysLate")]
        public int DaystLate { get; set; }        [
            
        BsonElement("lateMinutes")]
        public int MinutesLate { get; set; }

        [BsonElement("position")]
        public string Position { get; set; }        
        


        // Time & Attendance
        [BsonElement("totalWorkingDays")]
        public int TotalWorkingDays { get; set; }

        [BsonElement("daysPresent")]
        public int DaysPresent { get; set; }        
        


        [BsonElement("daysAbsent")]
        public int DaysAbsent { get; set; }

        [BsonElement("unpaidLeaveDays")]
        public int UnpaidLeaveDays { get; set; }

        // Time & Money (Hours should be decimal for accuracy)
        [BsonElement("regularOvertimeHours")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal RegularOvertimeHours { get; set; }

        [BsonElement("holidayOvertimeHours")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal HolidayOvertimeHours { get; set; }

        [BsonElement("nightDifferentialHours")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal NightDifferentialHours { get; set; }

        // Earnings Components
        [BsonElement("basicSalary")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BasicSalary { get; set; } // The employee's standard pay rate/base

        [BsonElement("proratedBasicSalary")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ProratedBasicSalary { get; set; }

        [BsonElement("allowances")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Allowances { get; set; }

        [BsonElement("overtimePay")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal OvertimePay { get; set; }

        [BsonElement("holidayPay")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal HolidayPay { get; set; }

        [BsonElement("nightDifferentialPay")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal NightDifferentialPay { get; set; }

        [BsonElement("bonuses")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Bonuses { get; set; }

        [BsonElement("otherEarnings")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal OtherEarnings { get; set; }

        [BsonElement("grossSalary")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal GrossSalary { get; set; }

        // Deduction Components
        [BsonElement("sssDeduction")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal SssDeduction { get; set; }

        [BsonElement("philHealthDeduction")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PhilHealthDeduction { get; set; }

        [BsonElement("pagIbigDeduction")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PagIbigDeduction { get; set; }

        [BsonElement("withholdingTax")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal WithholdingTax { get; set; }

        [BsonElement("sssLoan")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal SssLoan { get; set; }

        [BsonElement("pagIbigLoan")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PagIbigLoan { get; set; }

        [BsonElement("companyLoan")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CompanyLoan { get; set; }

        [BsonElement("absencePenalty")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal AbsencePenalty { get; set; }

        [BsonElement("latePenalty")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LatePenalty { get; set; }

        [BsonElement("unpaidLeaveDeduction")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal UnpaidLeaveDeduction { get; set; }

        [BsonElement("otherDeductions")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal OtherDeductions { get; set; }

        [BsonElement("totalDeductions")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TotalDeductions { get; set; }

        [BsonElement("netSalary")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal NetSalary { get; set; }

        // Audit/Status Fields
        [BsonElement("remarks")]
        public string? Remarks { get; set; }

        [BsonElement("status")]
        public string Status { get; set; }

        [BsonElement("isManuallyAdjusted")]
        public bool IsManuallyAdjusted { get; set; }

        [BsonElement("adjustmentHistory")]
        public object? AdjustmentHistory { get; set; }
    }

}