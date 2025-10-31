namespace Sheessential_Sales_Finance.Models
{
    public class ExpensesWithBalanceViewModel
    {
        public List<Expenses> Expenses { get; set; } = new List<Expenses>();
        public Balance Balance { get; set; } = new Balance();
    }
}
