namespace Sheessential_Sales_Finance.Models
{
    public class FinancePdfPayload
    {
        public string BarChart { get; set; }
        public string GeneratedBy { get; set; }
        public string LogoBase64 { get; set; }

        public string DoughnutChart { get; set; }
        public string Period { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public SummaryData Summary { get; set; }
        public List<FinanceRow> TableRows { get; set; }
    }

    public class SummaryData
    {
        public string Income { get; set; }
        public string Expenses { get; set; }
        public string Profit { get; set; }
        public string ProfitPct { get; set; }
    }

}
