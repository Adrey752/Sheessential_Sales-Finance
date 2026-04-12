namespace Sheessential_Sales_Finance.Models
{
    public class ExpensesWithBalanceViewModel
    {
        public List<Expenses> Expenses { get; set; } = new List<Expenses>();
        public Balance Balance { get; set; } = new Balance();
        public List<IngredientStockRequestDisplayModel> StockRequests { get; set; } = new List<IngredientStockRequestDisplayModel>();
        public List<PayrollRun> PayrollRuns { get; set; } = new List<PayrollRun>();
        public List<PayrollSnapshot> PayrollSnapshots { get; set; } = new List<PayrollSnapshot>();
    }
}
