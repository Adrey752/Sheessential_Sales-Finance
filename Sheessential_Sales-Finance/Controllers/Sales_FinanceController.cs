using Microsoft.AspNetCore.Mvc;
using Sheessential_Sales_Finance.helpers;
using MongoDB.Driver;

namespace Sheessential_Sales_Finance.Controllers
{
    public class Sales_FinanceController : Controller
    {
        private readonly MongoHelper _mongo;

        public Sales_FinanceController(MongoHelper mongo) {
            _mongo = mongo;

        }
        public IActionResult Index()
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");

            ViewBag.UserName = userName;
            ViewBag.UserRole = userRole;

            // ✅ Fetch latest 5 active customers
            var latestCustomers = _mongo.Users
                .Find(u => u.Role == "customer" && u.Status == "active")
                .SortByDescending(u => u.CreatedAt)
                .Limit(5)
                .ToList();

            ViewBag.LatestCustomers = latestCustomers;

            // ✅ Fetch all sales
            var sales = _mongo.Sales.Find(_ => true).ToList();

            if (sales.Count > 0)
            {
                // Convert string prices to numeric safely
                double totalRevenue = sales.Sum(s =>
                {
                    double.TryParse(s.SalePrice.ToString(), out double price);
                    return price;
                });

                double totalExpense = sales.Sum(s =>
                {
                    double.TryParse(s.SaleTax.ToString(), out double tax);
                    double.TryParse(s.SaleDiscounts.ToString(), out double discount);
                    return tax + discount;
                });

                int totalSales = sales.Count;

                ViewBag.Revenue = totalRevenue;
                ViewBag.Expense = totalExpense;
                ViewBag.Sales = totalSales;
            }
            else
            {
                ViewBag.Revenue = 0;
                ViewBag.Expense = 0;
                ViewBag.Sales = 0;
            }

            return View();
        }
        public IActionResult Products()
        {
            var products = _mongo.Inventories.Find(_ => true).ToList();
            ViewBag.TopProduct = products.OrderByDescending(p => p.StockQuantity).FirstOrDefault();
            return View(products);
        }

    }
}
