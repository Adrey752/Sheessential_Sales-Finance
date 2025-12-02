namespace Sheessential_Sales_Finance.Models
{
    public class InventoryView
    {
        public required IEnumerable<InventoryProducts> Products { get; set; }
        public required IEnumerable<InventoryProductSales> ProductSales { get; set; }
    }
}
