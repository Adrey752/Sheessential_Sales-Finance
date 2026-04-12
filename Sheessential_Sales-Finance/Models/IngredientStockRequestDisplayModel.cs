using System;

namespace Sheessential_Sales_Finance.Models
{
    public class IngredientStockRequestDisplayModel
    {
        // Fields for processing/action
        public string Id { get; set; }
        public string? ExpenseId { get; set; }
        public string RequestStatus { get; set; }

        public decimal TotalCost { get; set; }
        public string RequestedByUserId { get; set; } // For actions if needed

        // Display Fields (from lookups or direct)
        public DateTime? RequestDate { get; set; }
        public string IngredientName { get; set; } // Lookup
        public string SupplierName { get; set; } // Lookup
        public string RequestedBy { get; set; }
        public int QuantityRequested { get; set; }
        public string Unit { get; set; }
        public int CurrentStockAtRequest { get; set; }
        public string Instructions { get; set; }
    }
}