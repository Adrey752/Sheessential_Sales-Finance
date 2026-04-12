using Sheessential_Sales_Finance.Models;

namespace Sheessential_Sales_Finance.Models
{
    public class DashboardViewModel
    {
        public double Revenue { get; set; }
        public double Expense { get; set; }
        public double NetProfit { get; set; }
        public int SalesCount { get; set; }
        public string UserName { get; set; } = "User";
        public int TotalOrders { get; set; }
        public int PaidOrders { get; set; }
        public int UnpaidOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public decimal OrderRevenue { get; set; }
        public List<TbOrder> RecentOrders { get; set; } = new();
    }
}