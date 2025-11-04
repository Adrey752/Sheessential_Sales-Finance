namespace Sheessential_Sales_Finance.Models
{
    public class FinanceReportResponse
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetProfit { get; set; }
        public decimal NetProfitPercent { get; set; }

        public List<string> Labels { get; set; } = new();
        public List<decimal> Revenue { get; set; } = new();
        public List<decimal> Expense { get; set; } = new();

        public List<string> BreakdownLabels { get; set; } = new();
        public List<decimal> BreakdownData { get; set; } = new();

        public List<FinanceRow> Rows { get; set; } = new();
        public string StartDateIso { get; set; } = string.Empty;
        public string EndDateIso { get; set; } = string.Empty;
    }

}
