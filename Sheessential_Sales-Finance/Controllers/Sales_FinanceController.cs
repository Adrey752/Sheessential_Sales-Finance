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

            //GetMonthlySalesData

            return View();
        }



        public async Task<IActionResult> Dashboard()
        {
            var userName = HttpContext.Session.GetString("UserName") ?? "User";

            // --- Example Metrics ---
            var invoices = await _mongo.Invoices.Find(i => !i.IsArchived).ToListAsync();
            ViewBag.Revenue = invoices.Where(i => i.Status == "Paid").Sum(i => i.Total);
            ViewBag.Expense = 0m; // replace with real data if needed
            ViewBag.Sales = invoices.Count;

            // --- Fetch Recent Logs ---
            var logs = await _mongo.ActionLog
                .Find(_ => true)
                .SortByDescending(l => l.TimeStamp)
                .Limit(10)
                .ToListAsync();

            // --- Get all involved users ---
            var userIds = logs.Select(l => l.UserId).Distinct().ToList();
            var users = await _mongo.Users
                .Find(u => userIds.Contains(u.Id))
                .ToListAsync();

            // --- Map logs with user names ---
            var enrichedLogs = logs.Select(log =>
            {
                var user = users.FirstOrDefault(u => u.Id == log.UserId);
                return new
                {
                    UserName = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown User",
                    log.Action,
                    log.Entity,
                    log.Description,
                    log.TimeStamp
                };
            }).ToList();

            ViewBag.UserName = userName;
            ViewBag.ActionLogs = enrichedLogs;

            return View();
        }


        //[HttpGet]
        //public IActionResult GetMonthlySalesData()
        //{
        //    var sales = _mongo.ProductSales.Find(_ => true).ToList();

        //    // Group by month (based on transactionDate)
        //    var monthlyData = sales
        //        .GroupBy(s => s.TransactionDate.ToString("MMM"))
        //        .Select(g => new
        //        {
        //            Month = g.Key,
        //            TotalRevenue = g.Sum(x => (double)x.SalePrice),
        //            TotalExpense = g.Sum(x => (double)(x.SaleTax + x.SaleDiscounts))
        //        })
        //        .OrderBy(x => DateTime.ParseExact(x.Month, "MMM", null))
        //        .ToList();

        //    return Json(monthlyData);
        //}


        [HttpGet]
        public IActionResult GetSalesData(string period = "monthly")
        {
            var sales = _mongo.ProductSales.Find(_ => true).ToList();

            var grouped = period.ToLower() switch
            {
                "weekly" => sales
                    .GroupBy(s => System.Globalization.CultureInfo.CurrentCulture.Calendar
                        .GetWeekOfYear(s.TransactionDate, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                    .OrderBy(g => g.Key)
                    .Select(g => new {
                        Label = $"Week {g.Key}",
                        TotalRevenue = g.Sum(x => (double)x.SalePrice),
                        TotalExpense = g.Sum(x => (double)(x.SaleTax + x.SaleDiscounts))
                    }),

                "quarterly" => sales
                    .GroupBy(s => (s.TransactionDate.Month - 1) / 3 + 1)
                    .OrderBy(g => g.Key)
                    .Select(g => new {
                        Label = $"Q{g.Key}",
                        TotalRevenue = g.Sum(x => (double)x.SalePrice),
                        TotalExpense = g.Sum(x => (double)(x.SaleTax + x.SaleDiscounts))
                    }),

                "yearly" => sales
                    .GroupBy(s => s.TransactionDate.Year)
                    .OrderBy(g => g.Key)
                    .Select(g => new {
                        Label = g.Key.ToString(),
                        TotalRevenue = g.Sum(x => (double)x.SalePrice),
                        TotalExpense = g.Sum(x => (double)(x.SaleTax + x.SaleDiscounts))
                    }),

                _ => sales // Default: Monthly
                    .GroupBy(s => new { s.TransactionDate.Year, s.TransactionDate.Month })
                    .OrderBy(g => g.Key.Year)
                    .ThenBy(g => g.Key.Month)
                    .Select(g => new {
                        Label = $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Key.Month)} {g.Key.Year}",
                        TotalRevenue = g.Sum(x => (double)x.SalePrice),
                        TotalExpense = g.Sum(x => (double)(x.SaleTax + x.SaleDiscounts))
                    })
            };

            return Json(grouped.ToList());
        }

        [HttpGet]
        public IActionResult GetSalesReportData(string period = "monthly")
        {
            var sales = _mongo.ProductSales.Find(_ => true).ToList();

            var grouped = period.ToLower() switch
            {
                "weekly" => sales
                    .GroupBy(s => System.Globalization.CultureInfo.CurrentCulture.Calendar
                        .GetWeekOfYear(s.TransactionDate, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                    .OrderBy(g => g.Key)
                    .Select(g => new {
                        Label = $"Week {g.Key}",
                        TotalSales = g.Sum(x => (double)x.SalePrice)
                    }),

                "quarterly" => sales
                    .GroupBy(s => (s.TransactionDate.Month - 1) / 3 + 1)
                    .OrderBy(g => g.Key)
                    .Select(g => new {
                        Label = $"Q{g.Key}",
                        TotalSales = g.Sum(x => (double)x.SalePrice)
                    }),

                "yearly" => sales
                    .GroupBy(s => s.TransactionDate.Year)
                    .OrderBy(g => g.Key)
                    .Select(g => new {
                        Label = g.Key.ToString(),
                        TotalSales = g.Sum(x => (double)x.SalePrice)
                    }),

                _ => sales // Default: Monthly
                    .GroupBy(s => new { s.TransactionDate.Year, s.TransactionDate.Month })
                    .OrderBy(g => g.Key.Year)
                    .ThenBy(g => g.Key.Month)
                    .Select(g => new {
                        Label = $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Key.Month)} {g.Key.Year}",
                        TotalSales = g.Sum(x => (double)x.SalePrice)
                    })
            };

            return Json(grouped.ToList());
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
            // Fetch all invoices first
            var invoices = await _mongo.Invoices
                .Find(invoice => !invoice.IsArchived)
                .SortByDescending(i => i.CreatedAt)
                .ToListAsync();

            // Update overdue invoices
            var now = DateTime.UtcNow;
            var overdueInvoices = invoices
                .Where(i => i.Status == "Unpaid" && i.DueDate.HasValue && i.DueDate.Value < now)
                .ToList();

            if (overdueInvoices.Any())
            {
                foreach (var invoice in overdueInvoices)
                {
                    invoice.Status = "Overdue";
                    invoice.UpdatedAt = now;

                    // Update in MongoDB
                    var filter = Builders<Invoice>.Filter.Eq(i => i.Id, invoice.Id);
                    var update = Builders<Invoice>.Update
                        .Set(i => i.Status, "Overdue")
                        .Set(i => i.UpdatedAt, now);

                    await _mongo.Invoices.UpdateOneAsync(filter, update);
                }
            }

            // Calculate totals
            var overdueAmount = invoices.Where(i => i.Status == "Overdue").Sum(i => i.Total);
            var openAmount = invoices.Where(i => i.Status == "Unpaid").Sum(i => i.Total);
            var draftedAmount = invoices.Where(i => i.Status == "Draft").Sum(i => i.Total);

            //  Replace billedTo with readable name
            foreach (var invoice in invoices)
            {
                var billedTo = await _mongo.Users.Find(u => u.Id == invoice.BilledTo).FirstOrDefaultAsync();
                invoice.BilledTo = billedTo?.FullName ?? "Unknown Customer";
            }

            // Fetch available products
            var availableProducts = await _mongo.Inventories
                .Find(_ => true)
                .SortBy(p => p.Item)
                .ToListAsync();

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

            ViewBag.NextInvoiceNumber = await GenerateInvoiceNumber();
            return View(viewModel);
        }






        [HttpPost]
        public async Task<IActionResult> DeleteInvoice(string id)
        {
            _logger.LogInformation($"\n\n Archiving invoice with Id: {id} \n\n");

            // Find the invoice
            var invoice = await _mongo.Invoices.Find(i => i.Id == id).FirstOrDefaultAsync();
            if (invoice == null)
                return NotFound(new { success = false, message = $"Invoice not found. Id: {id}" });

            // Mark as archived instead of deleting
            var update = Builders<Invoice>.Update
                .Set(i => i.IsArchived, true)
                .Set(i => i.UpdatedAt, DateTime.UtcNow);

            await _mongo.Invoices.UpdateOneAsync(i => i.Id == id, update);

            // Log the action
            var userId = HttpContext.Session.GetString("UserId") ?? "unknown";
            var actionLog = new ActionLog
            {
                UserId = userId,
                Entity = "Invoice",
                EntityId = id,
                Action = "ARCHIVE",
                Description = $"Archived invoice #{invoice.InvoiceNumber}",
                TimeStamp = DateTime.UtcNow
            };

            await _mongo.ActionLog.InsertOneAsync(actionLog);

            return Json(new { success = true, message = "Invoice archived successfully." });
        }



        [HttpPost]
        public async Task<IActionResult> CreateInvoice(Invoice invoice)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Remove items with zero or negative quantity
            invoice.Items = invoice.Items
                .Where(i => i.Quantity > 0)
                .ToList();

            if (invoice.Items.Count == 0)
                return BadRequest("Invoice must contain at least one item with quantity greater than 0.");

            // Auto-generate invoice number
            invoice.InvoiceNumber = await GenerateInvoiceNumber();
            invoice.CreatedAt = DateTime.UtcNow;
            invoice.UpdatedAt = null;
            invoice.Status = "Unpaid";

            if (invoice.Items == null)
                invoice.Items = new List<ProductSale>();

            // ✅ Insert the invoice
            await _mongo.Invoices.InsertOneAsync(invoice);

            // ✅ After insert, invoice.Id now holds the generated ObjectId
            var userId = HttpContext.Session.GetString("UserId");
            var actionLog = new ActionLog(
                userId: userId, // or your actual logged-in user’s ID
                entity: "Invoice",
                entityId: invoice.Id!, // use the generated Id here
                action: "CREATE",
                description: $"Created invoice #{invoice.InvoiceNumber}"
            );

            await _mongo.ActionLog.InsertOneAsync(actionLog);

            ViewBag.NextInvoiceNumber = await GenerateInvoiceNumber();
            return RedirectToAction("Invoices");
        }


        private async Task<string> GenerateInvoiceNumber()
        {
            var lastInvoice = await _mongo.Invoices
                .Find(_ => true)
                .SortByDescending(i => i.CreatedAt)
                .Limit(1)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastInvoice != null && !string.IsNullOrEmpty(lastInvoice.InvoiceNumber))
            {
                var numericPart = new string(lastInvoice.InvoiceNumber.Where(char.IsDigit).ToArray());
                if (int.TryParse(numericPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"INV-{nextNumber:D5}";
        }


        //[HttpGet]
        //public IActionResult GetProductSales(string productId, string period)
        //{
        //    try
        //    {
        //        _logger.LogInformation("📊 GetProductSales called for ProductId: {ProductId}, Period: {Period}", productId, period);

        //        if (string.IsNullOrEmpty(productId))
        //            return BadRequest("Invalid productId");

        //        // ✅ Convert to ObjectId safely


        //        // ✅ Find all invoices containing this product
        //        var invoices = _mongo.Invoices
        //            .Find(i => i.Items.Any(item => item.ProductId.ToString() == productId))
        //            .ToList();

        //        if (!invoices.Any())
        //            return Json(new { message = "No invoices found for this product." });

        //        // ✅ Extract matching product sales
        //        var sales = invoices
        //            .SelectMany(i => i.Items.Where(item => item.ProductId.ToString() == productId))
        //            .ToList();

        //        if (!sales.Any())
        //            return Json(new { message = "No sales data found." });

        //        IEnumerable<object> grouped;

        //        switch (period.ToLower())
        //        {
        //            case "week":
        //                grouped = sales
        //                    .GroupBy(s => s.TransactionDate.ToLocalTime().ToString("ddd"))
        //                    .Select(g => new { Label = g.Key, Total = g.Sum(x => x.Quantity) })
        //                    .OrderBy(g => g.Label)
        //                    .ToList();
        //                break;

        //            case "month":
        //                grouped = sales
        //                    .GroupBy(s => s.TransactionDate.ToLocalTime().ToString("MMM dd"))
        //                    .Select(g => new { Label = g.Key, Total = g.Sum(x => x.Quantity) })
        //                    .OrderBy(g => g.Label)
        //                    .ToList();
        //                break;

        //            case "year":
        //                grouped = sales
        //                    .GroupBy(s => s.TransactionDate.ToLocalTime().ToString("MMM"))
        //                    .Select(g => new { Label = g.Key, Total = g.Sum(x => x.Quantity) })
        //                    .OrderBy(g => g.Label)
        //                    .ToList();
        //                break;

        //            default:
        //                grouped = Enumerable.Empty<object>();
        //                break;
        //        }

        //        _logger.LogInformation("✅ Found {Count} grouped sales records", grouped.Count());
        //        return Json(grouped);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "❌ Error in GetProductSales for ProductId: {ProductId}", productId);
        //        return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        //    }
        //}

        //[HttpGet]
        //public IActionResult GetProductSales(string productId, string period)
        //{
        //    try
        //    {
        //        _logger.LogInformation("📊 GetProductSales called for ProductId: {ProductId}, Period: {Period}", productId, period);

        //        if (string.IsNullOrEmpty(productId))
        //            return BadRequest("Invalid productId");

        //        // ✅ Find all invoices containing this product
        //        var invoices = _mongo.Invoices
        //            .Find(i => i.Items.Any(item => item.ProductId.ToString() == productId))
        //            .ToList();

        //        if (!invoices.Any())
        //            return Json(new { message = "No invoices found for this product." });

        //        // ✅ Extract matching product sales
        //        var sales = invoices
        //            .SelectMany(i => i.Items.Where(item => item.ProductId.ToString() == productId))
        //            .ToList();

        //        if (!sales.Any())
        //            return Json(new { message = "No sales data found." });

        //        IEnumerable<object> grouped;

        //        switch (period.ToLower())
        //        {
        //            case "week":
        //                grouped = sales
        //                    .GroupBy(s => s.TransactionDate.Date)
        //                    .Select(g => new
        //                    {
        //                        Label = g.Key.ToString("ddd"),
        //                        OrderKey = g.Key.DayOfWeek,
        //                        Total = g.Sum(x => x.Quantity)
        //                    })
        //                    .OrderBy(g => g.OrderKey)
        //                    .Select(g => new { g.Label, g.Total })
        //                    .ToList();
        //                break;

        //            case "month":
        //                grouped = sales
        //                    .GroupBy(s => s.TransactionDate.Date)
        //                    .Select(g => new
        //                    {
        //                        Label = g.Key.ToString("MMM dd"),
        //                        OrderKey = g.Key,
        //                        Total = g.Sum(x => x.Quantity)
        //                    })
        //                    .OrderBy(g => g.OrderKey)
        //                    .Select(g => new { g.Label, g.Total })
        //                    .ToList();
        //                break;

        //            case "year":
        //                grouped = sales
        //                    .GroupBy(s => new { g = s.TransactionDate.Year, m = s.TransactionDate.Month })
        //                    .Select(g => new
        //                    {
        //                        Label = new DateTime(g.Key.g, g.Key.m, 1).ToString("MMM"),
        //                        OrderKey = g.Key.m,
        //                        Total = g.Sum(x => x.Quantity)
        //                    })
        //                    .OrderBy(g => g.OrderKey)
        //                    .Select(g => new { g.Label, g.Total })
        //                    .ToList();
        //                break;

        //            default:
        //                grouped = Enumerable.Empty<object>();
        //                break;
        //        }

        //        _logger.LogInformation("✅ Found {Count} grouped sales records", grouped.Count());
        //        return Json(grouped);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "❌ Error in GetProductSales for ProductId: {ProductId}", productId);
        //        return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        //    }
        //}

        //[HttpGet]
        //public IActionResult GetProductSales(string productId, string period)
        //{
        //    try
        //    {
        //        _logger.LogInformation("📊 GetProductSales called for ProductId: {ProductId}, Period: {Period}", productId, period);

        //        if (string.IsNullOrEmpty(productId))
        //            return BadRequest("Invalid productId");

        //        var invoices = _mongo.Invoices
        //            .Find(i => i.Items.Any(item => item.ProductId.ToString() == productId))
        //            .ToList();

        //        if (!invoices.Any())
        //            return Json(new { message = "No invoices found for this product." });

        //        var sales = invoices
        //            .SelectMany(i => i.Items.Where(item => item.ProductId.ToString() == productId))
        //            .ToList();

        //        if (!sales.Any())
        //            return Json(new { message = "No sales data found." });

        //        IEnumerable<object> grouped;

        //        switch (period.ToLower())
        //        {
        //            case "week":
        //                // ✅ Group all Mondays together, all Tuesdays together, etc.
        //                grouped = sales
        //                    .GroupBy(s => s.TransactionDate.DayOfWeek)
        //                    .Select(g => new
        //                    {
        //                        Label = g.Key.ToString(),
        //                        Total = g.Sum(x => x.Quantity)
        //                    })
        //                    .OrderBy(g => (int)Enum.Parse(typeof(DayOfWeek), g.Label))
        //                    .ToList();
        //                break;

        //            case "month":
        //                grouped = sales
        //                    .GroupBy(s => s.TransactionDate.Date)
        //                    .Select(g => new
        //                    {
        //                        Label = g.Key.ToString("MMM dd"),
        //                        OrderKey = g.Key,
        //                        Total = g.Sum(x => x.Quantity)
        //                    })
        //                    .OrderBy(g => g.OrderKey)
        //                    .Select(g => new { g.Label, g.Total })
        //                    .ToList();
        //                break;

        //            case "year":
        //                grouped = sales
        //                    .GroupBy(s => new { s.TransactionDate.Year, s.TransactionDate.Month })
        //                    .Select(g => new
        //                    {
        //                        Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
        //                        OrderKey = g.Key.Month,
        //                        Total = g.Sum(x => x.Quantity)
        //                    })
        //                    .OrderBy(g => g.OrderKey)
        //                    .Select(g => new { g.Label, g.Total })
        //                    .ToList();
        //                break;

        //            default:
        //                grouped = Enumerable.Empty<object>();
        //                break;
        //        }

        //        _logger.LogInformation("✅ Found {Count} grouped sales records", grouped.Count());
        //        return Json(grouped);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "❌ Error in GetProductSales for ProductId: {ProductId}", productId);
        //        return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        //    }
        //}

        [HttpGet]
        public IActionResult GetProductSales(string productId, string period)
        {
            try
            {
                _logger.LogInformation("📊 GetProductSales called for ProductId: {ProductId}, Period: {Period}", productId, period);

                if (string.IsNullOrEmpty(productId))
                    return BadRequest("Invalid productId");

                var invoices = _mongo.Invoices
                    .Find(i => i.Items.Any(item => item.ProductId.ToString() == productId))
                    .ToList();

                if (!invoices.Any())
                    return Json(new { message = "No invoices found for this product." });

                var sales = invoices
                    .SelectMany(i => i.Items.Where(item => item.ProductId.ToString() == productId))
                    .ToList();

                if (!sales.Any())
                    return Json(new { message = "No sales data found." });

                // 🕒 Determine the date range based on period
                var now = DateTime.Now;
                DateTime startDate;

                switch (period.ToLower())
                {
                    case "week":
                        int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                        startDate = now.Date.AddDays(-diff);
                        break;

                    case "month":
                        startDate = new DateTime(now.Year, now.Month, 1);
                        break;

                    case "year":
                        startDate = new DateTime(now.Year, 1, 1);
                        break;

                    default:
                        return BadRequest("Invalid period specified.");
                }

                // ✅ Filter only within the range
                sales = sales.Where(s => s.TransactionDate >= startDate && s.TransactionDate <= now).ToList();

                IEnumerable<object> grouped;

                switch (period.ToLower())
                {
                    case "week":
                        grouped = sales
                            .GroupBy(s => s.TransactionDate.DayOfWeek)
                            .Select(g => new
                            {
                                Label = g.Key.ToString(),
                                Total = g.Sum(x => x.Quantity)
                            })
                            .OrderBy(g => (int)Enum.Parse(typeof(DayOfWeek), g.Label))
                            .ToList();
                        break;

                    case "month":
                        grouped = sales
                            .GroupBy(s => s.TransactionDate.Date)
                            .Select(g => new
                            {
                                Label = g.Key.ToString("MMM dd"),
                                OrderKey = g.Key,
                                Total = g.Sum(x => x.Quantity)
                            })
                            .OrderBy(g => g.OrderKey)
                            .Select(g => new { g.Label, g.Total })
                            .ToList();
                        break;

                    case "year":
                        grouped = sales
                            .GroupBy(s => new { s.TransactionDate.Year, s.TransactionDate.Month })
                            .Select(g => new
                            {
                                Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                                OrderKey = g.Key.Month,
                                Total = g.Sum(x => x.Quantity)
                            })
                            .OrderBy(g => g.OrderKey)
                            .Select(g => new { g.Label, g.Total })
                            .ToList();
                        break;

                    default:
                        grouped = Enumerable.Empty<object>();
                        break;
                }

                _logger.LogInformation("✅ Found {Count} grouped sales records", grouped.Count());
                return Json(grouped);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetProductSales for ProductId: {ProductId}", productId);
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }



        //

        //Update

        //[HttpGet]
        //public async Task<IActionResult> GetNextInvoiceNumber()
        //{
        //    var last = await _mongo.Invoices
        //        .Find(_ => true)
        //        .SortByDescending(i => i.CreatedAt)
        //        .FirstOrDefaultAsync();

        //    int next = 1;
        //    if (last != null && last.InvoiceNumber.StartsWith("INV-"))
        //    {
        //        var num = last.InvoiceNumber.Replace("INV-", "");
        //        if (int.TryParse(num, out var n))
        //            next = n + 1;
        //    }

        //    var invoiceNumber = $"INV-{next:D5}";
        //    return Json(new { invoiceNumber });
        //}

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string id, string newStatus)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(newStatus))
                return BadRequest("Invalid invoice ID or status.");

            var invoice = _mongo.Invoices.Find(i => i.Id == id).FirstOrDefault();
            if (invoice == null) return NotFound("Noto foundo");
            var filter = Builders<Invoice>.Filter.Eq(i => i.Id, id);
            var update = Builders<Invoice>.Update
                .Set(i => i.Status, newStatus)
                .Set(i => i.UpdatedAt, DateTime.UtcNow);

            var result = await _mongo.Invoices.UpdateOneAsync(filter, update);

            if (result.MatchedCount == 0)
                return NotFound("Invoice not found.");

            var userId = HttpContext.Session.GetString("UserId");
            var actionLog = new ActionLog(
                userId: userId, 
                entity: "Invoice",
                entityId: id, 
                action: "Update",
                description: $"Updated status of invoice #{invoice.InvoiceNumber} to {newStatus}"
            );
            await _mongo.ActionLog.InsertOneAsync(actionLog);

            return Ok(new { success = true, message = "Invoice status updated successfully." });
        }
        //delete

        //Fetch Customers in customers page with search function
        public IActionResult Customers(string? searchQuery, string? selectedId)
        {
            // ✅ Base filter: only customers
            var filter = Builders<User>.Filter.Where(u => u.Role.ToLower() == "customer");

            // ✅ Add search filter if user types something
            if (!string.IsNullOrEmpty(searchQuery))
            {
                var searchFilter = Builders<User>.Filter.Or(
                    Builders<User>.Filter.Regex(u => u.FirstName, new MongoDB.Bson.BsonRegularExpression(searchQuery, "i")),
                    Builders<User>.Filter.Regex(u => u.LastName, new MongoDB.Bson.BsonRegularExpression(searchQuery, "i")),
                    Builders<User>.Filter.Regex(u => u.Email, new MongoDB.Bson.BsonRegularExpression(searchQuery, "i")),
                    Builders<User>.Filter.Regex(u => u.Phone, new MongoDB.Bson.BsonRegularExpression(searchQuery, "i"))
                );

                filter = Builders<User>.Filter.And(filter, searchFilter);
            }

            // ✅ Apply the filter (instead of ignoring it)
            var users = _mongo.Users.Find(filter).ToList();

            // ✅ Compute stats based on the same filtered list
            ViewBag.TotalCustomers = users.Count;
            ViewBag.ActiveCustomers = users.Count(u => u.Status.ToLower() == "active");
            ViewBag.InactiveCustomers = users.Count(u => u.Status.ToLower() == "inactive");

            // ✅ Select user for modal preview (if any)
            ViewBag.SelectedUser = !string.IsNullOrEmpty(selectedId)
                ? users.FirstOrDefault(u => u.Id == selectedId)
                : null;

            return View(users);
        }


        public IActionResult Reports()
        {
            var productSalesList = _mongo.ProductSales.Find(_ => true).ToList();

            if (productSalesList == null || productSalesList.Count == 0)
            {
                ViewBag.Revenue = 0;
                ViewBag.Expense = 0;
                ViewBag.TotalTransactions = 0;
                return View(new List<ProductSale>());
            }

            ViewBag.Revenue = productSalesList.Sum(x => x.SalePrice);
            ViewBag.Expense = productSalesList.Sum(x => x.SaleTax + x.SaleDiscounts);
            ViewBag.TotalTransactions = productSalesList.Count;

            var monthlyData = productSalesList
                .GroupBy(s => s.TransactionDate.ToLocalTime().ToString("MMM yyyy"))
                .Select(g => new
                {
                    Month = g.Key,
                    TotalRevenue = g.Sum(x => x.SalePrice),
                    TotalExpense = g.Sum(x => x.SaleTax + x.SaleDiscounts)
                })
                .OrderBy(x => DateTime.ParseExact(x.Month, "MMM yyyy", null))
                .ToList();

            ViewBag.MonthlyData = monthlyData;

            return View(productSalesList);
        }
        public IActionResult Expenses()
        {
            // Get all invoices from MongoDB
            var invoices = _mongo.Invoices.Find(_ => true).ToList();

            if (invoices == null || invoices.Count == 0)
            {
                ViewBag.TotalExpenses = 0;
                ViewBag.PendingBills = 0;
                ViewBag.PaidBills = 0;
                ViewBag.PurchasesThisMonth = 0;
                return View(new List<Invoice>());
            }

            // Compute totals
            ViewBag.TotalExpenses = invoices.Sum(i => i.Total);
            ViewBag.PendingBills = invoices.Where(i => i.Status == "Unpaid" || i.Status == "Pending").Sum(i => i.Total);
            ViewBag.PaidBills = invoices.Where(i => i.Status == "Paid").Sum(i => i.Total);

            // Purchases this month
            var now = DateTime.UtcNow;
            ViewBag.PurchasesThisMonth = invoices.Count(i => i.IssuedAt.Month == now.Month && i.IssuedAt.Year == now.Year);

            return View(invoices);
        }


        //public IActionResult Vendors()
        //{
        //    var vendors = _mongo.Vendors.Find(_ => true).ToList();

        //    if (vendors == null || vendors.Count == 0)
        //    {   
        //        ViewBag.TotalVendors = 0;
        //        ViewBag.ActiveVendors = 0;
        //        ViewBag.InactiveVendors = 0;
        //        ViewBag.PendingBills = 0;
        //        return View(new List<Vendor>());
        //    }

        //    // Compute vendor statistics
        //    ViewBag.TotalVendors = vendors.Count;
        //    ViewBag.ActiveVendors = vendors.Count(v => v.Status == "Active");
        //    ViewBag.InactiveVendors = vendors.Count(v => v.Status == "Inactive");

        //    // (Optional) Static placeholder for now
        //    ViewBag.PendingBills = 12500; // Replace with real computation later

        //    return View(vendors);
        //}


        //// Add new Vendor
        //[HttpPost]
        //public IActionResult AddVendor(Vendor vendor)
        //{
        //    if (vendor == null)
        //    {
        //        TempData["ErrorMessage"] = "Vendor data is missing.";
        //        return RedirectToAction("Vendors");
        //    }

        //    // Auto-generate VendorId if not set
        //    vendor.VendorId = "VND-" + DateTime.Now.Ticks.ToString().Substring(10);
        //    vendor.Status = "Active";
        //    vendor.TotalPurchases = 0;
        //    vendor.CreatedAt = DateTime.Now;
        //    vendor.UpdatedAt = DateTime.Now;

        //    _mongo.Vendors.InsertOne(vendor);

        //    TempData["SuccessMessage"] = "Vendor added successfully!";
        //    return RedirectToAction("Vendors");
        //}


    }




}
