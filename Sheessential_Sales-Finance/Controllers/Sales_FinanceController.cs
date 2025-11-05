using Microsoft.AspNetCore.Mvc;
using Sheessential_Sales_Finance.helpers;
using MongoDB.Driver;
using Sheessential_Sales_Finance.Models;
using MongoDB.Bson;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;

namespace Sheessential_Sales_Finance.Controllers
{
    public class Sales_FinanceController : Controller
    {
        private readonly MongoHelper _mongo;
        private readonly ILogger<AuthController> _logger;
        private readonly IConverter _converter;

        public Sales_FinanceController(MongoHelper mongo, ILogger<AuthController> logger, IConverter converter)
        {
            _mongo = mongo;
            _logger = logger;
            _converter = converter;
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



        [HttpGet] // Sales vs Expenses Chart
        public IActionResult GetSalesData(string period = "monthly")
        {
            var invoices = _mongo.Invoices.Find(_ => true).ToList();
            var sales = invoices.SelectMany(invoice => invoice.Items).ToList();
            var expenses = _mongo.Expenses.Find(_ => _.Status == "Approved").ToList();

            DateTime today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            var grouped = period.ToLower() switch
            {
                //  WEEKLY (show Mon → Today, label per day)
                "weekly" => sales
                    .Where(s => s.TransactionDate >= startOfWeek)
                    .GroupBy(s => s.TransactionDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Label = g.Key.ToString("ddd"), // Mon, Tue, Wed...
                        TotalRevenue = g.Sum(x => (double)x.SalePrice),
                        TotalExpense = expenses
                            .Where(e => e.RequestedAt.Date == g.Key)
                            .Sum(e => (double)e.Amount)
                    }),

                //  YEARLY (show Jan → Current month, label per month)
                "yearly" => sales
                    .Where(s => s.TransactionDate >= startOfYear)
                    .GroupBy(s => s.TransactionDate.Month)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Label = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Key),
                        TotalRevenue = g.Sum(x => (double)x.SalePrice),
                        TotalExpense = expenses
                            .Where(e => e.RequestedAt.Month == g.Key)
                            .Sum(e => (double)e.Amount)
                    }),

                // MONTHLY (default: 1st day → Today, label per day number)
                _ => sales
                    .Where(s => s.TransactionDate >= startOfMonth)
                    .GroupBy(s => s.TransactionDate.Day)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Label = g.Key.ToString(), // 1, 2, 3...
                        TotalRevenue = g.Sum(x => (double)x.SalePrice),
                        TotalExpense = expenses
                            .Where(e => e.RequestedAt.Day == g.Key)
                            .Sum(e => (double)e.Amount)
                    }),
            };

            return Json(grouped.ToList());
        }




        [HttpGet] // From Reports Page
        public IActionResult GetSalesReportData(string period = "monthly")
        {
            var invoices = _mongo.Invoices.Find(_ => true).ToList();
            var sales = invoices.SelectMany(invoice => invoice.Items).ToList();

            DateTime today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            var grouped = period.ToLower() switch
            {
                // WEEKLY: Monday → today (Group by each day)
                "weekly" => sales
                    .Where(s => s.TransactionDate >= startOfWeek)
                    .GroupBy(s => s.TransactionDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Label = g.Key.ToString("ddd"), // Mon, Tue, Wed...
                        TotalSales = g.Sum(x => (double)x.SalePrice)
                    }),

                // YEARLY: January → current month (Group by month)
                "yearly" => sales
                    .Where(s => s.TransactionDate >= startOfYear)
                    .GroupBy(s => s.TransactionDate.Month)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Label = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Key),
                        TotalSales = g.Sum(x => (double)x.SalePrice)
                    }),

                // MONTHLY (default): 1 → today (Group by day of month)
                _ => sales
                    .Where(s => s.TransactionDate >= startOfMonth)
                    .GroupBy(s => s.TransactionDate.Day)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Label = g.Key.ToString(), // 1, 2, 3 ...
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

        public async Task<IActionResult> InvoiceArchieves()
        {
            // Fetch all invoices first
            var invoices = await _mongo.Invoices
                .Find(invoice => invoice.IsArchived)
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
        public async Task<IActionResult> Restore(string id)
        {
            var filter = Builders<Invoice>.Filter.Eq(i => i.Id, id);
            var update = Builders<Invoice>.Update
                .Set(invoice => invoice.IsArchived, false);
            TempData["Restored"] = true;
            _logger.LogInformation("\n\n\nRestore bruhh\n\n\n\n");
            await _mongo.Invoices.UpdateOneAsync(filter, update);
            return RedirectToAction("InvoiceArchieves");
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

        [HttpGet]
        public IActionResult GetProductSales(string productId, string period = "month")
        {
            if (string.IsNullOrEmpty(productId))
                return Json(new { message = "Missing product ID" });

            var productSales = _mongo.Invoices.Find(_ => true).ToList()
                .SelectMany(inv => inv.Items)
                .Where(item => item.ProductId == productId)
                .ToList();

            if (!productSales.Any())
                return Json(new { message = "No sales data found" });

            DateTime today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            IEnumerable<object> grouped;

            switch (period.ToLower())
            {
                // WEEKLY: Monday → Today, label on days (Mon, Tue, Wed)
                case "week":
                    grouped = productSales
                        .Where(s => s.TransactionDate >= startOfWeek)
                        .GroupBy(s => s.TransactionDate.Date)
                        .OrderBy(g => g.Key)
                        .Select(g => new
                        {
                            Label = g.Key.ToString("ddd"),  // Mon, Tue, Wed...
                            Total = g.Sum(x => x.Quantity)
                        });
                    break;

                //  YEARLY: January → Current Month, grouped by month
                case "year":
                    grouped = productSales
                        .Where(s => s.TransactionDate >= startOfYear)
                        .GroupBy(s => s.TransactionDate.Month)
                        .OrderBy(g => g.Key)
                        .Select(g => new
                        {
                            Label = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Key),
                            Total = g.Sum(x => x.Quantity)
                        });
                    break;

                //  MONTHLY : 1 → Today, grouped by day-of-month
                default:
                    grouped = productSales
                        .Where(s => s.TransactionDate >= startOfMonth)
                        .GroupBy(s => s.TransactionDate.Day)
                        .OrderBy(g => g.Key)
                        .Select(g => new
                        {
                            Label = g.Key.ToString(),  // Day number 1,2,3...
                            Total = g.Sum(x => x.Quantity)
                        });
                    break;
            }

            return Json(grouped);
        }





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


        [HttpGet]
        public IActionResult Reports()
        {
            // fitch all invoices (with embedded product sales)
            var invoices = _mongo.Invoices.Find(_ => true).ToList();

            // Handol mt data
            if (invoices == null || invoices.Count == 0)
            {
                ViewBag.Revenue = 0;
                ViewBag.Expense = 0;
                ViewBag.TotalTransactions = 0;
                return View();
            }

            // get product sales from each inboyesesesldkflskdjf
            var productSalesList = invoices.SelectMany(inv => inv.Items).ToList();

            if (productSalesList.Count == 0)
            {
                ViewBag.Revenue = 0;
                ViewBag.Expense = 0;
                ViewBag.TotalTransactions = 0;
                return View();
            }


            ViewBag.Revenue = productSalesList.Sum(x => x.SalePrice * x.Quantity);
            ViewBag.Expense = productSalesList.Sum(x => x.SaleTax + x.SaleDiscounts);
            ViewBag.TotalTransactions = productSalesList.Count;


            return View();
        }

        //Expenses in expense page
        public IActionResult Expenses(string status, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                // ✅ Default to "Pending" if no filter is provided
                if (string.IsNullOrEmpty(status))
                    status = "Pending";

                var filter = Builders<Expenses>.Filter.Empty;

                // ✅ Apply Status Filter
                if (!string.IsNullOrEmpty(status))
                {
                    filter &= Builders<Expenses>.Filter.Eq(e => e.Status, status);
                }

                // ✅ Apply Date Range Filter
                if (startDate.HasValue && endDate.HasValue)
                {
                    filter &= Builders<Expenses>.Filter.Gte(e => e.RequestedAt, startDate.Value)
                           & Builders<Expenses>.Filter.Lte(e => e.RequestedAt, endDate.Value);
                }

                // ✅ Fetch filtered expenses
                var expenses = _mongo.Expenses.Find(filter).ToList() ?? new List<Expenses>();

                // ✅ Fetch current balance
                var balance = _mongo.Balance.Find(_ => true).FirstOrDefault();

                // ✅ Totals
                ViewBag.TotalExpenses = expenses.Sum(e => e?.Amount ?? 0);
                ViewBag.PendingTotal = expenses.Where(e => e.Status == "Pending").Sum(e => e.Amount);
                ViewBag.ApprovedTotal = expenses.Where(e => e.Status == "Approved").Sum(e => e.Amount);
                ViewBag.DeclinedTotal = expenses.Where(e => e.Status == "Declined").Sum(e => e.Amount);

                // ✅ Pass the selected filter to the view so dropdown keeps selected value
                ViewBag.SelectedStatus = status;

                var viewModel = new ExpensesWithBalanceViewModel
                {
                    Expenses = expenses,
                    Balance = balance
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Expenses page.");
                return View(new ExpensesWithBalanceViewModel
                {
                    Expenses = new List<Expenses>(),
                    Balance = new Balance { CurrentBalance = 0 }
                });
            }
        }






        //accept expense
        [HttpPost]
        public IActionResult ApproveExpense(string id)
        {
            try
            {
                // ✅ 1. Find the expense record
                var expense = _mongo.Expenses.Find(e => e.Id == id).FirstOrDefault();
                if (expense == null)
                {
                    _logger.LogWarning($"Expense with ID {id} not found.");
                    return RedirectToAction("Expenses");
                }

                // ✅ 2. Find the current balance (assuming only 1 record)
                var balance = _mongo.Balance.Find(_ => true).FirstOrDefault();

                if (balance != null)
                {
                    // ✅ 3. Check if there's enough balance
                    if (balance.CurrentBalance >= expense.Amount)
                    {
                        // Deduct the expense amount
                        balance.CurrentBalance -= expense.Amount;

                        // ✅ 4. Update the Balance collection
                        var balanceFilter = Builders<Balance>.Filter.Eq(b => b.Id, balance.Id);
                        var balanceUpdate = Builders<Balance>.Update
                            .Set(b => b.CurrentBalance, balance.CurrentBalance);
                        _mongo.Balance.UpdateOne(balanceFilter, balanceUpdate);

                        // ✅ 5. Update the Expense status
                        var expenseFilter = Builders<Expenses>.Filter.Eq(e => e.Id, id);
                        var expenseUpdate = Builders<Expenses>.Update
                            .Set(e => e.Status, "Approved")
                            .Set(e => e.RequestedAt, DateTime.UtcNow);
                        _mongo.Expenses.UpdateOne(expenseFilter, expenseUpdate);

                        _logger.LogInformation($"Expense {id} approved and balance updated.");
                    }
                    else
                    {
                        // Not enough balance to approve
                        TempData["Error"] = "Insufficient balance to approve this expense.";
                    }
                }
                else
                {
                    _logger.LogWarning("No balance record found in the database.");
                }

                return RedirectToAction("Expenses");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving expense with ID {id}");
                TempData["Error"] = "An error occurred while approving the expense.";
                return RedirectToAction("Expenses");
            }
        }


        // Add new expense
        [HttpPost]
        public IActionResult AddExpense(Expenses newExpense)
        {
            // ✅ 1. Get the latest expense by ExpenseId
            var lastExpense = _mongo.Expenses
                .Find(_ => true)
                .SortByDescending(e => e.ExpenseId)
                .FirstOrDefault();

            // ✅ 2. Generate the next ExpenseId (EXP-0006, etc.)
            int nextNumber = 1;
            if (lastExpense != null && !string.IsNullOrEmpty(lastExpense.ExpenseId))
            {
                string lastNumberPart = lastExpense.ExpenseId.Replace("EXP-", "");
                if (int.TryParse(lastNumberPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            newExpense.ExpenseId = $"EXP-{nextNumber.ToString("D4")}";
            newExpense.Status = "Pending";
            newExpense.RequestedAt = DateTime.UtcNow;

            // ✅ 3. Save to MongoDB
            _mongo.Expenses.InsertOne(newExpense);

            // ✅ 4. Redirect back to Expense list
            return RedirectToAction("Expenses");
        }


        //Get all Vendors
        public IActionResult Vendors(int page = 1)
        {
            int pageSize = 5; // ✅ show only 5 vendors per page
            int skip = (page - 1) * pageSize;

            // Filter only non-archived vendors
            var vendorsQuery = _mongo.Vendors.Find(v => v.IsArchived == false);

            // Get total count
            var totalVendors = vendorsQuery.CountDocuments();

            // Apply pagination
            var vendors = vendorsQuery
                .Skip(skip)
                .Limit(pageSize)
                .ToList();

            // Archived Vendors for the modal
            var archivedVendors = _mongo.Vendors.Find(v => v.IsArchived == true).ToList();

            // Store pagination data for the view
            ViewBag.TotalVendors = totalVendors;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalVendors / (double)pageSize);

            ViewBag.ActiveVendors = _mongo.Vendors.CountDocuments(v => v.Status == "Active" && !v.IsArchived);
            ViewBag.InactiveVendors = _mongo.Vendors.CountDocuments(v => v.Status == "Inactive" && !v.IsArchived);
            ViewBag.PendingBills = 0; // placeholder
            ViewBag.ArchivedVendors = archivedVendors;

            return View(vendors);
        }







        // Add new Vendor
        [HttpPost]
        public IActionResult AddVendor(Vendor vendor)
        {
            if (vendor == null)
            {
                TempData["ErrorMessage"] = "Vendor data is missing.";
                return RedirectToAction("Vendors");
            }

            // Auto-generate VendorId if not set
            vendor.VendorId = "VND-" + DateTime.Now.Ticks.ToString().Substring(10);
            vendor.Status = "Active";
            vendor.TotalPurchases = 0;
            vendor.CreatedAt = DateTime.Now;
            vendor.UpdatedAt = DateTime.Now;

            _mongo.Vendors.InsertOne(vendor);

            TempData["SuccessMessage"] = "Vendor added successfully!";
            return RedirectToAction("Vendors");
        }

        //Edit Vendor
        [HttpPost]
        public IActionResult EditVendor(Vendor vendor)
        {
            var filter = Builders<Vendor>.Filter.Eq(v => v.Id, vendor.Id);
            var update = Builders<Vendor>.Update
                .Set(v => v.VendorName, vendor.VendorName)
                .Set(v => v.CompanyName, vendor.CompanyName)
                .Set(v => v.Address, vendor.Address)
                .Set(v => v.ContactPerson, vendor.ContactPerson)
                .Set(v => v.ContactNumber, vendor.ContactNumber)
                .Set(v => v.Email, vendor.Email)
                .Set(v => v.PaymentTerms, vendor.PaymentTerms)
                .Set(v => v.Status, vendor.Status)
                .Set(v => v.Notes, vendor.Notes);

            _mongo.Vendors.UpdateOne(filter, update);

            return RedirectToAction("Vendors");
        }

        //Archive Vendor
        [HttpPost]
        public IActionResult ArchiveVendor(string Id)
        {
            if (string.IsNullOrEmpty(Id))
            {
                return BadRequest();
            }

            // Filter for the vendor by Id
            var filter = Builders<Vendor>.Filter.Eq(v => v.Id, Id);

            // Update to set IsArchived to true
            var update = Builders<Vendor>.Update.Set(v => v.IsArchived, true);

            var result = _mongo.Vendors.UpdateOne(filter, update);

            if (result.ModifiedCount > 0)
            {
                TempData["SuccessMessage"] = "Vendor archived successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Vendor not found or already archived.";
            }

            // Redirect back to the Vendors page
            return RedirectToAction("Vendors"); // or your vendor list action
        }

        //Get all Archived Vendors I think we are not using this one
        public IActionResult ArchivedVendors()
        {
            // Only get vendors that are not archived
            var vendors = _mongo.Vendors.Find(v => v.IsArchived == true).ToList();

            ViewBag.TotalVendors = vendors.Count;
            ViewBag.ActiveVendors = vendors.Count(v => v.Status == "Active");
            ViewBag.InactiveVendors = vendors.Count(v => v.Status == "Inactive");
            ViewBag.PendingBills = vendors.Sum(v => v.TotalPurchases);

            return View(vendors);
        }



        // ✅ RESTORE VENDOR
        [HttpPost]
        public IActionResult RestoreVendor(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            try
            {
                var objectId = new MongoDB.Bson.ObjectId(id); // convert string to ObjectId
                var filter = Builders<Vendor>.Filter.Eq(v => v.Id, id);
                var update = Builders<Vendor>.Update.Set(v => v.IsArchived, false);

                var result = _mongo.Vendors.UpdateOne(filter, update);

                if (result.ModifiedCount > 0)
                    TempData["SuccessMessage"] = "Vendor restored successfully.";
                else
                    TempData["ErrorMessage"] = "Vendor not found or already active.";
            }
            catch (FormatException)
            {
                TempData["ErrorMessage"] = "Invalid vendor ID format.";
            }

            return RedirectToAction("Vendors"); // or wherever your restore page is
        }





        public ActionResult ReportPdf()
        {
            return View();
        }



        //[HttpPost]
        //public IActionResult ExportPdfFromImages([FromBody] ChartPayload payload)
        //{

        //    _logger.LogInformation("\n\n\nI'm in Eport to pdf method lil ni....ssan\n\n\n\n");
        //    // Option A: render a Razor view to HTML string (recommended)
        //    var html = RenderViewToString("ReportPdf", payload);

        //    var doc = new HtmlToPdfDocument()
        //    {
        //        GlobalSettings = {
        //        Orientation = Orientation.Portrait,
        //        PaperSize = PaperKind.A4
        //    },
        //        Objects = {
        //        new ObjectSettings {
        //            HtmlContent = html,
        //            WebSettings = { DefaultEncoding = "utf-8", LoadImages = true }
        //        }
        //    }
        //    };

        //    var pdf = _converter.Convert(doc);
        //    return File(pdf, "application/pdf", "Dashboard_Report.pdf");
        //}

        //// helper to render Razor view to string (same helper you had earlier)
        //private string RenderViewToString(string viewName, object model)
        //{
        //    var viewEngine = HttpContext.RequestServices.GetService(typeof(IRazorViewEngine)) as IRazorViewEngine;
        //    var tempDataProvider = HttpContext.RequestServices.GetService(typeof(ITempDataProvider)) as ITempDataProvider;
        //    var actionContext = new ActionContext(HttpContext, RouteData, ControllerContext.ActionDescriptor);
        //    var viewResult = viewEngine.FindView(actionContext, viewName, false);

        //    if (viewResult.View == null) throw new Exception($"View {viewName} not found.");

        //    using var sw = new StringWriter();
        //    var viewContext = new ViewContext(actionContext, viewResult.View, new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model }, new TempDataDictionary(HttpContext, tempDataProvider), sw, new HtmlHelperOptions());
        //    viewResult.View.RenderAsync(viewContext).GetAwaiter().GetResult();
        //    return sw.ToString();
        //}

        [HttpPost]
        public IActionResult ExportPdfFromImages([FromBody] ChartPayload payload)
        {
            _logger.LogInformation("📄 ExportPdfFromImages method triggered...");

            // 1️⃣ Render Razor View to HTML string
            var html = RenderViewToString("ReportPdf", payload);

            // 2️⃣ Configure the PDF document
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = new GlobalSettings
                {
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings { Top = 10, Bottom = 10 },
                },
                Objects = {
                    new ObjectSettings {
                        HtmlContent = html,
                        WebSettings = {
                            DefaultEncoding = "utf-8",
                            LoadImages = true
                        }
                    }
                }
            };

            // 3Convert HTML to PDF
            var pdf = _converter.Convert(doc);

            // 4 Return as downloadable file
            return File(pdf, "application/pdf", "Dashboard_Report.pdf");
        }

        //  Helper method to render Razor view into a string
        private string RenderViewToString(string viewName, object model)
        {
            var viewEngine = HttpContext.RequestServices.GetService(typeof(IRazorViewEngine)) as IRazorViewEngine;
            var tempDataProvider = HttpContext.RequestServices.GetService(typeof(ITempDataProvider)) as ITempDataProvider;
            var actionContext = new ActionContext(HttpContext, RouteData, ControllerContext.ActionDescriptor);

            var viewResult = viewEngine.FindView(actionContext, viewName, false);
            if (viewResult.View == null)
                throw new Exception($"View '{viewName}' not found.");

            using var sw = new StringWriter();
            var viewContext = new ViewContext(
                actionContext,
                viewResult.View,
                new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model },
                new TempDataDictionary(HttpContext, tempDataProvider),
                sw,
                new HtmlHelperOptions()
            );

            viewResult.View.RenderAsync(viewContext).GetAwaiter().GetResult();
            return sw.ToString();
        }

        // Donut
        [HttpGet]
        public IActionResult GetExpenseBreakdown()
        {
            // Get all approved expenses (optional: include others if needed)
            var expenses = _mongo.Expenses.Find(_ => _.Status == "Approved").ToList();

            // Group by ExpenseType and sum up their total amounts
            var breakdown = expenses
                .GroupBy(e => e.ExpenseType)
                .Select(g => new
                {
                    Label = g.Key,
                    Total = g.Sum(x => (double)x.Amount)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            return Json(breakdown);
        }

        public IActionResult ReportsDashBoard()
        {
            return View();
        }

        public IActionResult FinanceReport()
        {
            return View();
        }
        public IActionResult SalesReport()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> FinanceReportData(string period = "week")
        {
            _logger.LogInformation("\n\n\n\nI'm in Finance Report \n\n\n\n");

            // Normalize period string
            period = (period ?? "week").ToLowerInvariant();

            // ✅ Always use local timezone (PH TIME)
            var phTime = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTime).Date;

            DateTime start;
            DateTime end = localNow.AddDays(1).AddTicks(-1); // end of today PH time

            // ==== PERIOD RANGE LOGIC =====
            if (period == "week")
            {
                // Monday start of current week
                int diff = ((int)localNow.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                start = localNow.AddDays(-diff);
            }
            else if (period == "month")
            {
                start = new DateTime(localNow.Year, localNow.Month, 1);
            }
            else if (period == "year")
            {
                start = new DateTime(localNow.Year, 1, 1);
            }
            else // alltime
            {
                var invoiceEarliest = await _mongo.Invoices
                    .Find(FilterDefinition<Invoice>.Empty)
                    .Project(i => i.Items.OrderBy(x => x.TransactionDate).FirstOrDefault().TransactionDate)
                    .SortBy(x => x)
                    .FirstOrDefaultAsync();

                var expenseEarliest = await _mongo.Expenses
                    .Find(FilterDefinition<Expenses>.Empty)
                    .Project(e => e.RequestedAt)
                    .SortBy(x => x)
                    .FirstOrDefaultAsync();

                DateTime earliest = DateTime.MaxValue;
                if (invoiceEarliest != default(DateTime)) earliest = invoiceEarliest < earliest ? invoiceEarliest : earliest;
                if (expenseEarliest != default(DateTime)) earliest = expenseEarliest < earliest ? expenseEarliest : earliest;

                if (earliest == DateTime.MaxValue)
                    earliest = localNow.AddYears(-1);

                earliest = TimeZoneInfo.ConvertTimeFromUtc(earliest, phTime);
                start = new DateTime(earliest.Year, earliest.Month, 1);
            }

            // ==== DB DATA ====

            var builder = Builders<Invoice>.Filter;

            var dateFilter = builder.ElemMatch(i => i.Items,
                it => it.TransactionDate >= start && it.TransactionDate <= end);

            // new paid filter
            var paidFilter = builder.Eq(i => i.Status, "Paid");

            // combine them
            var invoiceFilter = builder.And(dateFilter, paidFilter);

            var invoicesInRange = await _mongo.Invoices.Find(invoiceFilter).ToListAsync();

            var expenseFilter = Builders<Expenses>.Filter.And(
                Builders<Expenses>.Filter.Gte(e => e.RequestedAt, start),
                Builders<Expenses>.Filter.Lte(e => e.RequestedAt, end)
            );

            var expensesInRange = await _mongo.Expenses.Find(expenseFilter).ToListAsync();

            // ==== LABELS & BUCKETS ====
            List<DateTime> bucketStarts = new List<DateTime>();
            List<string> labels = new List<string>();

            if (period == "week")
            {
                for (var d = start; d.Date <= localNow.Date; d = d.AddDays(1))
                {
                    bucketStarts.Add(d.Date);
                    labels.Add(d.ToString("ddd")); // Mon, Tue, ...
                }
            }
            else if (period == "month")
            {
                for (int day = 1; day <= localNow.Day; day++)
                {
                    var d = new DateTime(localNow.Year, localNow.Month, day);
                    bucketStarts.Add(d.Date);
                    labels.Add(day.ToString());
                }
            }
            else
            {
                DateTime bucket = new DateTime(start.Year, start.Month, 1);
                DateTime endBucket = new DateTime(localNow.Year, localNow.Month, 1);

                while (bucket <= endBucket)
                {
                    bucketStarts.Add(bucket);
                    labels.Add(bucket.ToString("MMM yyyy", CultureInfo.InvariantCulture));
                    bucket = bucket.AddMonths(1);
                }
            }

            var revenueBuckets = new decimal[bucketStarts.Count];
            var expenseBuckets = new decimal[bucketStarts.Count];

            // ✅ Converts to PH time before evaluating bucket
            int GetBucketIndex(DateTime dt)
            {
                dt = TimeZoneInfo.ConvertTimeFromUtc(dt, phTime).Date;

                for (int i = 0; i < bucketStarts.Count; i++)
                {
                    var startBucket = bucketStarts[i];
                    DateTime bucketEnd = (i + 1 < bucketStarts.Count)
                        ? bucketStarts[i + 1].AddTicks(-1)
                        : end;

                    if (dt >= startBucket.Date && dt <= bucketEnd.Date)
                        return i;
                }
                return -1;
            }

            // ==== INCOME + ROW GENERATION ====
            List<FinanceRow> rows = new List<FinanceRow>();
            decimal totalIncome = 0;

            foreach (var inv in invoicesInRange)
            {
                foreach (var item in inv.Items)
                {
                    if (item.TransactionDate < start || item.TransactionDate > end) continue;

                    decimal amt = item.SalePrice * item.Quantity;
                    var idx = GetBucketIndex(item.TransactionDate);

                    if (idx >= 0) revenueBuckets[idx] += amt;
                    totalIncome += amt;

                    rows.Add(new FinanceRow
                    {
                        Reference = inv.InvoiceNumber,
                        Date = TimeZoneInfo.ConvertTimeFromUtc(item.TransactionDate, phTime),
                        Description = item.Item ?? "Sale",
                        Type = "Income",
                        Category = "Sales",
                        Department = "—",
                        Amount = amt
                    });
                }
            }

            // ==== EXPENSES ====
            decimal totalExpenses = 0;

            foreach (var exp in expensesInRange)
            {
                var idx = GetBucketIndex(exp.RequestedAt);
                if (idx >= 0) expenseBuckets[idx] += exp.Amount;
                totalExpenses += exp.Amount;

                rows.Add(new FinanceRow
                {
                    Reference = exp.ExpenseId,
                    Date = TimeZoneInfo.ConvertTimeFromUtc(exp.RequestedAt, phTime),
                    Description = exp.Description,
                    Type = "Expense",
                    Category = exp.ExpenseType,
                    Department = exp.Department,
                    Amount = exp.Amount
                });
            }

            // ==== EXPENSE BREAKDOWN ====
            var breakdown = expensesInRange
                .GroupBy(e => e.ExpenseType)
                .Select(g => new { Type = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Amount)
                .ToList();

            // sort rows descending by date
            rows = rows.OrderByDescending(r => r.Date).ToList();

            decimal netProfit = totalIncome - totalExpenses;
            decimal netProfitPct = totalIncome == 0 ? 0 : Math.Round((netProfit / totalIncome) * 100, 2);

            return Ok(new FinanceReportResponse
            {
                TotalIncome = Math.Round(totalIncome, 2),
                TotalExpenses = Math.Round(totalExpenses, 2),
                NetProfit = Math.Round(netProfit, 2),
                NetProfitPercent = netProfitPct,
                Labels = labels,
                Revenue = revenueBuckets.Select(x => Math.Round(x, 2)).ToList(),
                Expense = expenseBuckets.Select(x => Math.Round(x, 2)).ToList(),
                BreakdownLabels = breakdown.Select(b => b.Type).ToList(),
                BreakdownData = breakdown.Select(b => Math.Round(b.Amount, 2)).ToList(),
                Rows = rows,
                StartDateIso = start.ToString("o"),
                EndDateIso = end.ToString("o")
            });
        }

        [HttpGet]
        public async Task<IActionResult> SalesReportData(string period = "week")
        {
            period = period.ToLowerInvariant();
            var now = DateTime.UtcNow;

            // ✅ Determine start date based on dropdown
            DateTime start = period switch
            {
                "week" => now.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday),    // start of week (Monday)
                "month" => new DateTime(now.Year, now.Month, 1),                       // start of month
                "year" => new DateTime(now.Year, 1, 1),                                // Jan 1
                "all" => DateTime.MinValue,                                            // all data
                _ => now.AddDays(-7)
            };

            // Fetch invoices with items in desired date range
            var invoices = await _mongo.Invoices.Find(i =>
                    i.Status == "Paid" &&
                    i.Items.Any(it => it.TransactionDate >= start && it.TransactionDate <= now))
                .ToListAsync();

            var viewModel = new SalesReportViewModel
            {
                TotalSales = invoices.Sum(inv => inv.Total),
                TotalOrders = invoices.Count,
                ActiveCustomers = invoices.Select(inv => inv.BilledTo).Distinct().Count(),
            };

            // Flatten items and filter by selected period
            var items = invoices
                .SelectMany(i => i.Items)
                .Where(i => i.TransactionDate >= start && i.TransactionDate <= now)
                .ToList();

            // ✅ Get distinct ProductIds
            var productIds = items.Select(i => i.ProductId).Distinct().ToList();

            // ✅ Fetch product names from Products collection
            var products = await _mongo.Inventories
                .Find(p => productIds.Contains(p.Id))
                .ToListAsync();

            var productLookup = products.ToDictionary(p => p.Id, p => p.Item);

            // ✅ Chart Label logic adjusts depending on period
            if (period == "all" || period == "year")
            {
                // Group per month for YEAR and ALL TIME
                viewModel.ChartLabels = items
                    .GroupBy(i => new { i.TransactionDate.Year, i.TransactionDate.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"))
                    .ToList();

                viewModel.ChartValues = items
                    .GroupBy(i => new { i.TransactionDate.Year, i.TransactionDate.Month })
                    .Select(g => g.Sum(x => (decimal)x.SalePrice * x.Quantity))
                    .ToList();
            }
            else
            {
                // Week / Month grouping (per day)
                viewModel.ChartLabels = items
                    .GroupBy(i => i.TransactionDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => g.Key.ToString("MMM dd"))
                    .ToList();

                viewModel.ChartValues = items
                    .GroupBy(i => i.TransactionDate.Date)
                    .Select(g => g.Sum(x => (decimal)x.SalePrice * x.Quantity))
                    .ToList();
            }

            // ✅ Top Products
            viewModel.TopProducts = items
                .GroupBy(i => i.ProductId)
                .Select(g => new TopProductDto
                {
                    ProductName = productLookup.ContainsKey(g.Key)
                        ? productLookup[g.Key]
                        : "(Unknown Product)",

                    TotalAmount = g.Sum(x => (decimal)x.SalePrice * x.Quantity)
                })
                .OrderByDescending(p => p.TotalAmount)
                .Take(5)
                .ToList();

            // ✅ Full Sales Table
            viewModel.SalesRows = items
                .Select(x => new ProductSalesRow
                {
                    ProductId = x.ProductId.ToString(),
                    ProductName = productLookup.ContainsKey(x.ProductId)
                        ? productLookup[x.ProductId]
                        : "(Unknown Product)",

                    UnitPrice = (decimal)x.SalePrice,
                    Quantity = x.Quantity,
                    TotalAmount = x.Quantity * (decimal)x.SalePrice,
                    TransactionDate = x.TransactionDate
                })
                .OrderByDescending(x => x.TransactionDate)
                .ToList();

            return Json(viewModel);
        }



    }
}