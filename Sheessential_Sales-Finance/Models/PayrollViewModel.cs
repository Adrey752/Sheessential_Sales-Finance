namespace Sheessential_Sales_Finance.Models
{
    public class PayrollViewModel
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal GrossPay { get; set; }
        public bool IsComputed { get; set; } = false;
    }


}
