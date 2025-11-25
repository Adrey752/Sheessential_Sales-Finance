//namespace Sheessential_Sales_Finance.Models
//{
//    public class InvoiceListViewModel
//    {
//        public List<Invoice> Invoices { get; set; } = new();
//        public List<Products> AvailableProducts { get; set; } = new();
//        public List<User> Customers { get; set; } = new();

//        // Summary card amounts
//        public decimal OverdueAmount { get; set; }
//        public decimal OpenAmount { get; set; }
//        public decimal DraftedAmount { get; set; }

//    }
//}



namespace Sheessential_Sales_Finance.Models
{
    public class InvoiceListViewModel
    {
        // Changed from Invoice -> TbOrder
        public List<TbOrder> Orders { get; set; } = new();

        // Changed from User -> TbUser
        public List<TbUser> Customers { get; set; } = new();

        public List<ProductVariant> AvailableProducts { get; set; } = new();

        // Summary amounts
        public decimal OverdueAmount { get; set; }
        public decimal OpenAmount { get; set; }
        public decimal DraftedAmount { get; set; }
    }
}