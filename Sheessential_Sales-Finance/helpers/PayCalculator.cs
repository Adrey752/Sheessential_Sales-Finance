using Sheessential_Sales_Finance.Models;
using Sheessential_Sales_Finance.Models.Sheessential_Sales_Finance.Models;

namespace Sheessential_Sales_Finance.helpers
{
    public class PayrollCalculator
    {
        private const decimal SSS_RATE = 0.045m;
        private const decimal PHILHEALTH_RATE = 0.04m;
        private const decimal PAGIBIG_CONTRIBUTION = 100.00m;

        /// <summary>
        /// Calculates all payroll components for a semi-monthly period and returns a summary model.
        /// </summary>
        /// <param name="employee">The employee model with rates and IDs.</param>
        /// <param name="hoursWorked">The total hours worked within the 15-day period.</param>
        /// <returns>A TaxSummary model containing all calculated components (Gross, Deductions, Net).</returns>
        public TaxSummary CalculatePaySummarySemiMonthly(Employee employee, decimal hoursWorked)
        {
            var summary = new TaxSummary();

            // 1. Calculate Gross Income
            summary.GrossPay = employee.HourlyRate * hoursWorked;

            // 2. Calculate Statutory Deductions (Uses helper methods from previous response)
            summary.SssDeduction = CalculateSssDeduction(summary.GrossPay, employee.GovernmentIds.SssNumber);
            summary.PhilHealthDeduction = CalculatePhilHealthDeduction(summary.GrossPay, employee.GovernmentIds.PhilHealthNumber);
            summary.PagIbigDeduction = CalculatePagIbigDeduction(employee.GovernmentIds.PagIbigNumber);

            // Assuming no "OtherDeductions" for this example, set to 0.00m. 
            // If the Employee model had HmoPremiumDeduction, you'd add it here.
            summary.OtherDeductions = 0.00m;

            // 3. Calculate Total Statutory Contributions
            summary.TotalContributions = summary.SssDeduction + summary.PhilHealthDeduction + summary.PagIbigDeduction;

            // 4. Determine Taxable Income
            summary.TaxableIncome = summary.GrossPay - summary.TotalContributions;

            // 5. Calculate BIR Tax Due (Semi-Monthly Brackets)
            summary.BirTaxDue = CalculateWithholdingTax(summary.TaxableIncome);

            // 6. Calculate Final Pay Components
            summary.TotalDeductions = summary.TotalContributions + summary.BirTaxDue + summary.OtherDeductions;
            summary.NetPay = summary.GrossPay - summary.TotalDeductions;

            return summary;
        }

        // --- Deduction Helper Methods (Using Null Check Logic from last response) ---

        private decimal CalculateSssDeduction(decimal grossPay, string? sssNumber)
        {
            // Note: In a real system, you'd deduct even if ID is missing.
            return string.IsNullOrEmpty(sssNumber) ? 0.00m : grossPay * SSS_RATE;
        }

        private decimal CalculatePhilHealthDeduction(decimal grossPay, string? philHealthNumber)
        {
            return string.IsNullOrEmpty(philHealthNumber) ? 0.00m : (grossPay * PHILHEALTH_RATE) / 2.0m;
        }

        private decimal CalculatePagIbigDeduction(string? pagIbigNumber)
        {
            return string.IsNullOrEmpty(pagIbigNumber) ? 0.00m : PAGIBIG_CONTRIBUTION;
        }

        // --- Withholding Tax Logic (Unchanged Semi-Monthly Logic) ---

        private decimal CalculateWithholdingTax(decimal income)
        {
            const decimal divisor = 2.0m;
            decimal tier1 = 20833m / divisor;
            decimal tier2 = 33333m / divisor;
            decimal tier3 = 66667m / divisor;
            decimal tier4 = 166667m / divisor;
            decimal tier5 = 666667m / divisor;

            decimal baseTax2 = 1875m / divisor;
            decimal baseTax3 = 8541.80m / divisor;
            decimal baseTax4 = 33541.80m / divisor;
            decimal baseTax5 = 183541.80m / divisor;

            if (income <= tier1) return 0;
            else if (income <= tier2) return (income - tier1) * 0.15m;
            else if (income <= tier3) return baseTax2 + ((income - tier2) * 0.20m);
            else if (income <= tier4) return baseTax3 + ((income - tier3) * 0.25m);
            else if (income <= tier5) return baseTax4 + ((income - tier4) * 0.30m);
            else return baseTax5 + ((income - tier5) * 0.35m);
        }
    }
}
