namespace Sheessential_Sales_Finance.Models
{
    public class ChartPayload
    {
        public string SalesImage { get; set; }
        public string ExpenseImage { get; set; }
        public string CashFlowImage { get; set; }
        public string GeneratedAt { get; set; }
        public string Revenue { get; set; }
        public string Expense { get; set; }
        public string TotalTransactions { get; set; }
    }
}
