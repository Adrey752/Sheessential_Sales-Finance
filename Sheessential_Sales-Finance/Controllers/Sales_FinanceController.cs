using Microsoft.AspNetCore.Mvc;
using Sheessential_Sales_Finance.helpers;
using MongoDB.Driver;
using Sheessential_Sales_Finance.Models;
using MongoDB.Bson;
using System.Text.RegularExpressions;

namespace Sheessential_Sales_Finance.Controllers
{
    public class Sales_FinanceController : Controller
    {
        private readonly MongoHelper _mongo;
        private readonly ILogger<AuthController> _logger;

        public Sales_FinanceController(MongoHelper mongo, ILogger<AuthController> logger)
        {
            _mongo = mongo;
            _logger = logger;
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
            var sales = _mongo.ProductSales.Find(_ => true).ToList();

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
            var sales = _mongo.ProductSales.Find(_ => true).ToList();

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
            var sales = _mongo.ProductSales.Find(_ => true).ToList();
            var products = _mongo.Inventories.Find(_ => true).ToList();

            // ✅ Group by item name and calculate total quantity sold
            // Assuming you have these two collections loaded
            // var sales = await _productSalesCollection.Find(_ => true).ToListAsync();
            // var products = await _productsCollection.Find(_ => true).ToListAsync();

            var topProduct = sales
                .Join(
                    products,
                    sale => sale.ProductId,        // match ProductSale.ProductId
                    product => product.Id,         // with Product.Id
                    (sale, product) => new         // combine both objects
                    {
                        ProductName = product.Item,
                        sale.Quantity,
                        sale.SalePrice
                    }
                )
                .GroupBy(x => x.ProductName)
                .Select(g => new
                {
                    Item = g.Key,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.SalePrice * x.Quantity)
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
        public async Task<IActionResult> Invoices()
        {
            var invoices = await _mongo.Invoices
                .Find(_ => true)
                .SortByDescending(i => i.CreatedAt)
                .ToListAsync();

            var overdueAmount = invoices
                .Where(i => i.Status == InvoiceStatus.Overdue)
                .Sum(i => i.Total);

            var openAmount = invoices
                .Where(i => i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.Partial)
                .Sum(i => i.Total);

            var draftedAmount = invoices
                .Where(i => i.Status == InvoiceStatus.Draft)
                .Sum(i => i.Total);



            // Populate user display names
            foreach (var invoice in invoices)
            {
                var billedBy = await _mongo.Users.Find(u => u.Id == invoice.BilledBy).FirstOrDefaultAsync();
                var billedTo = await _mongo.Users.Find(u => u.Id == invoice.BilledTo).FirstOrDefaultAsync();

                invoice.BilledBy = billedBy?.FullName ?? "Unknown Seller";
                invoice.BilledTo = billedTo?.FullName ?? "Unknown Customer";
            }

            // Fetch available products
            var availableProducts = await _mongo.Inventories
                .Find(_ => true)
                .SortBy(p => p.Item)
                .ToListAsync();

            var viewModel = new InvoiceListViewModel
            {
                Invoices = invoices,
                OverdueAmount = overdueAmount,
                OpenAmount = openAmount,
                DraftedAmount = draftedAmount,
                AvailableProducts = availableProducts //ass to modal
            };

            return View(viewModel);
        }



        

        // POST: I'll use later
        [HttpPost]
        public async Task<IActionResult> CreateInvoice(Invoice invoice)
        {
            if (!ModelState.IsValid)
                return View(invoice);

            invoice.CreatedAt = DateTime.UtcNow;
            invoice.Status = InvoiceStatus.Draft;
            await _mongo.Invoices.InsertOneAsync(invoice);

            return RedirectToAction(nameof(Index));
        }
 

    }

}
 