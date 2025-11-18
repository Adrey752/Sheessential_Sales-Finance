namespace Sheessential_Sales_Finance.Models
{
    // DTO for sending data TO the JavaScript
    public class SalesReportDataDto
    {
        public ReportSummary Summary { get; set; }
        public List<ChartDataPoint> SalesTrend { get; set; }
        public List<ProductChartPoint> TopProductsChart { get; set; }
        public List<TransactionItem> TopProductsTable { get; set; }
    }

    public class ReportSummary
    {
        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public int ActiveCustomers { get; set; }
        public string TopPerformingProduct { get; set; }
    }

    public class ChartDataPoint
    {
        public string Label { get; set; }
        public double Total { get; set; }
    }

    public class ProductChartPoint
    {
        public string Name { get; set; }
        public double Percentage { get; set; }
    }


    // Payload class for receiving data FROM JavaScript for the PDF
    public class SalesPdfload
    {
        // Report Metadata
        public string ReportPeriod { get; set; }
        public string DateGenerated { get; set; }
        public string LogoBase64 { get; set; }

        // Summary Stats
        public ReportSummary Summary { get; set; }

        // Chart Images (as Base64 strings)
        public string SalesTrendChartBase64 { get; set; }
        public string TopProductsChartBase64 { get; set; }

        // Table Data
        public List<TransactionItem> TopProductsTable { get; set; }
    }

    // Re-usable model for the transactions table
    public class TransactionItem
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string Category { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
