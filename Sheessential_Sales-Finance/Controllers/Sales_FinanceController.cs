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
        //public async Task<IActionResult> Invoices()
        //{
        //    var invoices = await _mongo.Invoices
        //        .Find(_ => true)
        //        .SortByDescending(i => i.CreatedAt)
        //        .ToListAsync();

        //    var overdueAmount = invoices
        //        .Where(i => i.Status == InvoiceStatus.Overdue)
        //        .Sum(i => i.Total);

        //    var openAmount = invoices
        //        .Where(i => i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.Partial)
        //        .Sum(i => i.Total);

        //    var draftedAmount = invoices
        //        .Where(i => i.Status == InvoiceStatus.Draft)
        //        .Sum(i => i.Total);



        //    // Populate user display names
        //    foreach (var invoice in invoices)
        //    {
        //        var billedTo = await _mongo.Users.Find(u => u.Id == invoice.BilledTo).FirstOrDefaultAsync();
        //        invoice.BilledTo = billedTo?.FullName ?? "Unknown Customer";
        //    }

        //    // Fetch available products
        //    var availableProducts = await _mongo.Inventories
        //        .Find(_ => true)
        //        .SortBy(p => p.Item)
        //        .ToListAsync();

        //    var viewModel = new InvoiceListViewModel
        //    {
        //        Invoices = invoices,
        //        OverdueAmount = overdueAmount,
        //        OpenAmount = openAmount,
        //        DraftedAmount = draftedAmount,
        //        AvailableProducts = availableProducts //ass to modal
        //    };

        //    return View(viewModel);
        //}


        public async Task<IActionResult> Invoices()
        {
            var invoices = await _mongo.Invoices
                .Find(_ => true)
                .SortByDescending(i => i.CreatedAt)
                .ToListAsync();

            var overdueAmount = invoices.Where(i => i.Status == InvoiceStatus.Overdue).Sum(i => i.Total);
            var openAmount = invoices.Where(i => i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.Partial).Sum(i => i.Total);
            var draftedAmount = invoices.Where(i => i.Status == InvoiceStatus.Draft).Sum(i => i.Total);

            foreach (var invoice in invoices)
            {
                var billedTo = await _mongo.Users.Find(u => u.Id == invoice.BilledTo).FirstOrDefaultAsync();
                invoice.BilledTo = billedTo?.FullName ?? "Unknown Customer";
            }

            var availableProducts = await _mongo.Inventories.Find(_ => true).SortBy(p => p.Item).ToListAsync();

            //  Fetch customer list
            var customers = await _mongo.Users
                .Find(u => u.Role.ToLower() == "customer")
                .SortBy(u => u.FirstName)
                .ToListAsync();


            var viewModel = new InvoiceListViewModel
            {
                Invoices = invoices,
                OverdueAmount = overdueAmount,
                OpenAmount = openAmount,
                DraftedAmount = draftedAmount,
                AvailableProducts = availableProducts,
                Customers = customers,
            };

            return View(viewModel);
        }





        //// POST: I'll use later
        //[HttpPost]
        //public async Task<IActionResult> CreateInvoice(Invoice invoice)
        //{
        //    if (!ModelState.IsValid)
        //        return View(invoice);

        //    invoice.CreatedAt = DateTime.UtcNow;
        //    invoice.Status = InvoiceStatus.Draft;
        //    await _mongo.Invoices.InsertOneAsync(invoice);

        //    return RedirectToAction(nameof(Index));
        //}

        [HttpPost]
        public async Task<IActionResult> DeleteInvoice(string id)
        {
            _logger.LogInformation("\n\n\n\nThis is your id you bum!!! Id:" + id + "\n\n\n\n");

            var filter = Builders<Invoice>.Filter.Eq(i => i.Id, id);
            var result = await _mongo.Invoices.DeleteOneAsync(filter);

            if (result.DeletedCount == 0)
                return NotFound(new { success = false, message = "Invoice not found Id: " + id });

            return Json(new { success = true });
        }




        [HttpPost]
        public async Task<IActionResult> CreateInvoice(Invoice invoice)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 🔹 Auto-generate InvoiceNumber (format: INV-00001, INV-00002, ...)
            var lastInvoice = await _mongo.Invoices
                .Find(_ => true)
                .SortByDescending(i => i.CreatedAt)
                .Limit(1)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastInvoice != null && !string.IsNullOrEmpty(lastInvoice.InvoiceNumber))
            {
                // extract numeric part
                var numericPart = new string(lastInvoice.InvoiceNumber.Where(char.IsDigit).ToArray());
                if (int.TryParse(numericPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            invoice.InvoiceNumber = $"INV-{nextNumber:D5}";

            // 🔹 Set timestamps
            invoice.CreatedAt = DateTime.UtcNow;
            invoice.UpdatedAt = null;

            // 🔹 Default status
            invoice.Status = InvoiceStatus.Unpaid;

            // 🔹 Ensure server-side computation (Mongo ignores client values)
            if (invoice.Items == null) invoice.Items = new List<ProductSale>();

            await _mongo.Invoices.InsertOneAsync(invoice);

            // You can redirect to a details page or return JSON depending on usage
            return RedirectToAction("Invoices");
        }

        [HttpGet]
        public async Task<IActionResult> GetNextInvoiceNumber()
        {
            var last = await _mongo.Invoices
                .Find(_ => true)
                .SortByDescending(i => i.CreatedAt)
                .FirstOrDefaultAsync();

            int next = 1;
            if (last != null && last.InvoiceNumber.StartsWith("INV-"))
            {
                var num = last.InvoiceNumber.Replace("INV-", "");
                if (int.TryParse(num, out var n))
                    next = n + 1;
            }

            var invoiceNumber = $"INV-{next:D5}";
            return Json(new { invoiceNumber });
        }


    }

}
