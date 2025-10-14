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

        [HttpGet]
        public IActionResult GetMonthlySalesData()
        {
            var sales = _mongo.Sales.Find(_ => true).ToList();

            // Group by month (based on transactionDate)
            var monthlyData = sales
                .GroupBy(s => s.TransactionDate.ToString("MMM"))
                .Select(g => new
                {
                    Month = g.Key,
                    TotalRevenue = g.Sum(x => (double)x.SalePrice),
                    TotalExpense = g.Sum(x => (double)(x.SaleTax + x.SaleDiscounts))
                })
                .OrderBy(x => DateTime.ParseExact(x.Month, "MMM", null))
                .ToList();

            return Json(monthlyData);
        }

        public IActionResult Products(int page = 1)
        {
            int pageSize = 5; // Show 10 products per page

            // ✅ Fetch all sales data
            var sales = _mongo.Sales.Find(_ => true).ToList();

            // ✅ Group by item name and calculate total quantity sold
            var topProduct = sales
                .GroupBy(s => s.Item)
                .Select(g => new
                {
                    Item = g.Key,
                    TotalQuantity = g.Sum(x => Convert.ToInt32(x.Quantity)),
                    TotalRevenue = g.Sum(x =>
                    {
                        double.TryParse(x.SalePrice.ToString(), out double price);
                        return price;
                    })
                })
                .OrderByDescending(x => x.TotalQuantity)
                .FirstOrDefault();

            // ✅ Pass top product to view
            ViewBag.TopProduct = topProduct;

            // ✅ Fetch inventory list with pagination
            var totalProducts = (int)_mongo.Inventories.CountDocuments(_ => true);
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            var pagedProducts = _mongo.Inventories.Find(_ => true)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            // ✅ Pass pagination info to View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(pagedProducts);
        }


    }
}
