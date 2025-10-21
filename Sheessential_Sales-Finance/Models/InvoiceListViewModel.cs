namespace Sheessential_Sales_Finance.Models
{
    public class InvoiceListViewModel
    {
        public List<Invoice> Invoices { get; set; } = new();
        public List<Product> AvailableProducts { get; set; } = new();
        public List<User> Customers { get; set; } = new();

        // Summary card amounts
        public decimal OverdueAmount { get; set; }
        public decimal OpenAmount { get; set; }
        public decimal DraftedAmount { get; set; }

    }
}
