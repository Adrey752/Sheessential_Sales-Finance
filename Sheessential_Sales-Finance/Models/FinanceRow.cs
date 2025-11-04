namespace Sheessential_Sales_Finance.Models
{
    public class FinanceRow
    {
        public string Reference { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Income" or "Expense"
        public string Category { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

}
