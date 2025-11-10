namespace Sheessential_Sales_Finance.Models
{
    public class SalesPdfPayload
    {
        public string LogoBase64 { get; set; }
        public string Period { get; set; }
        public string GeneratedBy { get; set; }
        public string DateGenerated { get; set; }
        public string PeriodText { get; set; } // 🆕 add this



        public string RevenueChart { get; set; }
        public string TopProductsChart { get; set; }
        public string TableImage { get; set; }

        public SalesSummary Summary { get; set; }
    }

    public class SalesSummary
    {
        public string TotalSales { get; set; }
        public string TotalOrders { get; set; }
        public string ActiveCustomers { get; set; }
        public string TopProduct { get; set; }
    }

}
