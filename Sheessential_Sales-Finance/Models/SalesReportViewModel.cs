namespace Sheessential_Sales_Finance.Models
{

    public class SalesReportViewModel
    {
        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public int ActiveCustomers { get; set; }

        public List<string> ChartLabels { get; set; } = new();
        public List<decimal> ChartValues { get; set; } = new();

        public List<TopProductDto> TopProducts { get; set; } = new();
        public List<ProductSalesRow> SalesRows { get; set; } = new();
    }

    public class TopProductDto
    {
        public string ProductName { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ProductSalesRow
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime TransactionDate { get; set; }
    }


}
