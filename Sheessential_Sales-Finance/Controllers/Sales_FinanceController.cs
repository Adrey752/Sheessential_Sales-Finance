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
using MongoDB.Driver.Linq;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;

namespace Sheessential_Sales_Finance.Controllers
{
    public class Sales_FinanceController : Controller
    {
        private readonly MongoHelper _mongo;
        private readonly ILogger<AuthController> _logger;
        private readonly IConverter _converter;
        private readonly ICompositeViewEngine _viewEngine; // For rendering the template
        private readonly IWebHostEnvironment _env;


        public Sales_FinanceController(ICompositeViewEngine viewEngine, MongoHelper mongo, ILogger<AuthController> logger, IConverter converter, IWebHostEnvironment env)
        {
            _mongo = mongo;
            _logger = logger;
            _converter = converter;
            _env = env;
            _viewEngine = viewEngine;
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
            double revenue = (double)invoices.Where(i => i.Status == "Paid").Sum(i => i.Total);
            ViewBag.Revenue = revenue;

            // Fix for CS1061: Replace SumAsync with manual summation after fetching the data
            var expenses = await _mongo.Expenses
                .Find(e => e.Status == "Approved")
                .ToListAsync(); // Fetch the data as a list first
            double expense = (double)expenses.Sum(e => e.Amount); // Perform the summation on the list
            ViewBag.Expense = expense;
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
            ViewBag.NetProfit = revenue - expense;

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
            int pageSize = 5;

            // --- 1. PRE-FETCH ALL PRODUCT DATA & SALES DATA ---
            var allProducts = _mongo.ProductInventory.Find(_ => true)
                .ToList()
                .ToDictionary(p => p.Id, p => p);

            var sales = _mongo.ProductSalesInventory.Find(_ => true).ToList();

            // --- NEW: CALCULATE TOTAL ORDERS AND SALES ---

            // 1. Calculate Total Orders
            int totalOrders = sales.Count;

            // 2. Calculate Total Sales
            decimal totalSales = Math.Round(sales.Sum(s => decimal.Parse(s.SalePrice)), 2);

            // --- NEW: CALCULATE ORDERS COUNT PER VARIANT ---
            // Group the sales records by their VariantId and count the records in each group.
            // NOTE: This assumes your ProductSalesInventory model has a property named 'VariantId'
            // that links back to the ProductVariant.
            var variantSalesCounts = sales
                .GroupBy(s => s.VariantId) // Group by the ID of the variant being sold
                .ToDictionary(
                    g => g.Key,  // The key is the VariantId (the ID we grouped by)
                    g => g.Count() // The value is the total number of sales/orders for that variant
                );

            // --- 2. FETCH PAGINATED PRODUCT VARIANTS ---
            var totalProducts = (int)_mongo.ProductVariantInventory.CountDocuments(_ => true);
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            var pagedVariants = _mongo.ProductVariantInventory.Find(_ => true)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            // --- 3. ENRICH VARIANTS WITH CATEGORY, DESCRIPTION, AND ORDERS COUNT ---
            foreach (var variant in pagedVariants)
            {
                // 3a. Enrich with Product Data (Category and Description)
                if (allProducts.TryGetValue(variant.ProductId, out var product))
                {
                    variant.Category = product.ProductCategory;
                    variant.Description = product.ProductDesc;
                }
                else
                {
                    variant.Category = "N/A";
                    variant.Description = "No description available";
                }

                // 3b. Enrich with Orders Count
                // Check if the variant's ID exists in the sales count dictionary
                if (variantSalesCounts.TryGetValue(variant.Id, out int count))
                {
                    variant.OrdersCount = count;
                }
                else
                {
                    variant.OrdersCount = 0; // Set to 0 if no sales records are found
                }
            }

            // --- 4. PASS DATA TO VIEW BAGS ---
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalSales = $"₱{totalSales:N2}";

            return View(pagedVariants);
        }










        //public async Task<IActionResult> Invoices()
        //{
        //    // Fetch all invoices first
        //    var invoices = await _mongo.Invoices
        //        .Find(invoice => !invoice.IsArchived)
        //        .SortByDescending(i => i.CreatedAt)
        //        .ToListAsync();

        //    // Update overdue invoices
        //    var now = DateTime.UtcNow;
        //    var overdueInvoices = invoices
        //        .Where(i => i.Status == "Unpaid" && i.DueDate.HasValue && i.DueDate.Value < now)
        //        .ToList();

        //    if (overdueInvoices.Any())
        //    {
        //        foreach (var invoice in overdueInvoices)
        //        {
        //            invoice.Status = "Overdue";
        //            invoice.UpdatedAt = now;

        //            // Update in MongoDB
        //            var filter = Builders<Invoice>.Filter.Eq(i => i.Id, invoice.Id);
        //            var update = Builders<Invoice>.Update
        //                .Set(i => i.Status, "Overdue")
        //                .Set(i => i.UpdatedAt, now);

        //            await _mongo.Invoices.UpdateOneAsync(filter, update);
        //        }
        //    }

        //    // Calculate totals
        //    var overdueAmount = invoices.Where(i => i.Status == "Overdue").Sum(i => i.Total);
        //    var openAmount = invoices.Where(i => i.Status == "Unpaid").Sum(i => i.Total);
        //    var draftedAmount = invoices.Where(i => i.Status == "Draft").Sum(i => i.Total);

        //    //  Replace billedTo with readable name
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

        //    //  Fetch customer list
        //    var customers = await _mongo.Users
        //        .Find(u => u.Role.ToLower() == "customer")
        //        .SortBy(u => u.FirstName)
        //        .ToListAsync();

        //    var viewModel = new InvoiceListViewModel
        //    {
        //        Invoices = invoices,
        //        OverdueAmount = overdueAmount,
        //        OpenAmount = openAmount,
        //        DraftedAmount = draftedAmount,
        //        AvailableProducts = availableProducts,
        //        Customers = customers,
        //    };

        //    ViewBag.NextInvoiceNumber = await GenerateInvoiceNumber();
        //    ViewBag.ActiveUsers = _mongo.Users.Find(u => u.Status.ToLower() == "active").ToList().Count;
        //    return View(viewModel);
        //}

        public async Task<IActionResult> Invoices()
        {
            // 1. Fetch all Orders (replacing Invoices)
            // We filter by not archived if you have an IsArchived field, otherwise fetch all
            var orders = await _mongo.TbOrder
                .Find(_ => true)
                .SortByDescending(o => o.CreatedAt)
                .ToListAsync();

            /* NOTE: The "Update Overdue" logic below is commented out because 
               TbOrder does not have a 'DueDate' field to compare against DateTime.UtcNow.
               If you want this, you need to add a DueDate to TbOrder or calculate it 
               (e.g., CreatedAt + 30 days).
            */
            /*
            var now = DateTime.UtcNow;
            var overdueOrders = orders
                .Where(o => o.PaymentStatus == "Unpaid" && [Add DueDate Logic Here] < now)
                .ToList();

            if (overdueOrders.Any())
            {
                foreach (var order in overdueOrders)
                {
                    // Update logic here...
                }
            }
            */

            // 2. Calculate totals using TbOrder fields
            // Assuming 'Unpaid' means Open, 'Paid' is closed. 
            // If you have a specific 'Overdue' status in PaymentStatus, it will sum here.
            var overdueAmount = orders.Where(o => o.PaymentStatus == "Overdue").Sum(o => o.TotalAmount);
            var openAmount = orders.Where(o => o.PaymentStatus == "Unpaid").Sum(o => o.TotalAmount);

            // Assuming 'Pending' order status or 'Processing' payment status counts as Drafted/In-Progress
            var draftedAmount = orders.Where(o => o.PaymentStatus == "Pending" || o.OrderStatus == "Processing").Sum(o => o.TotalAmount);



            // 4. Combine Products and Variants into the combined ViewModel
            var availableProducts = await _mongo.ProductVariantInventory
                .Find(v => v.StockQuantity > 0)
                .ToListAsync();


            // 4. Fetch customer list (TbUser)
            var customers = await _mongo.TbUserCollection
                .Find(u => u.Role.ToLower() == "customer")
                .SortBy(u => u.FirstName)
                .ToListAsync();

            // 5. Prepare ViewModel
            var viewModel = new InvoiceListViewModel
            {
                Orders = orders, // Now passing TbOrder list
                OverdueAmount = overdueAmount,
                OpenAmount = openAmount,
                DraftedAmount = draftedAmount,
                AvailableProducts = availableProducts,
                Customers = customers
            };

            // 6. ViewBags
            // Ensure GenerateInvoiceNumber() is updated to handle Order Numbers if needed
            ViewBag.NextInvoiceNumber = await GenerateInvoiceNumber();

            // Count active users based on IsEmailVerified or other logic in TbUser
            ViewBag.ActiveUsers = await _mongo.TbUserCollection.CountDocumentsAsync(u => u.IsEmailVerified == true);

            return View(viewModel);
        }

        public async Task<IActionResult> InvoiceArchieves()
        {
            // 1. Fetch "Archived" Orders
            // Strategy: treating "Cancelled" or "Failed" orders as the "Archive" list
            // If you add an 'IsArchived' boolean to TbOrder later, change this query.
            var orders = await _mongo.TbOrder
                .Find(o => o.OrderStatus == "Cancelled")
                .SortByDescending(i => i.CreatedAt)
                .ToListAsync();

            /* NOTE: Overdue logic commented out. 
               TbOrder does not have 'DueDate'. If you need this, add logic based on 
               CreatedAt + X days, or add a DueDate field to the model.
            */
            /*
            var now = DateTime.UtcNow;
            var overdueOrders = orders
                .Where(o => o.PaymentStatus == "Unpaid" && [DueDate Logic] < now)
                .ToList();

            if (overdueOrders.Any()) { ... update logic ... }
            */

            // 2. Calculate totals based on TbOrder properties
            var overdueAmount = orders.Where(o => o.PaymentStatus == "Overdue").Sum(o => o.TotalAmount);
            var openAmount = orders.Where(o => o.PaymentStatus == "Unpaid").Sum(o => o.TotalAmount);

            // Treat Pending/Processing as Draft/In-Progress
            var draftedAmount = orders.Where(o => o.OrderStatus == "Processing").Sum(o => o.TotalAmount);


            // Fetch all active variants
            // 3. Fetch Products (for JS/ViewModel)
            var availableProducts = await _mongo.ProductVariantInventory
                .Find(v => v.IsActive == true && v.StockQuantity > 0)
                .ToListAsync();

            // 4. Fetch Customers (TbUser)
            var customers = await _mongo.TbUserCollection
                .Find(u => u.Role.ToLower() == "customer")
                .SortBy(u => u.FirstName)
                .ToListAsync();

            // 5. Construct ViewModel
            var viewModel = new InvoiceListViewModel
            {
                Orders = orders, // Updated property
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
            _logger.LogInformation($"\n\n Archiving order with Id: {id} \n\n");

            // 1. Find the Order (using TbOrder)
            var order = await _mongo.TbOrder.Find(i => i.Id == id).FirstOrDefaultAsync();

            if (order == null)
                return NotFound(new { success = false, message = $"Order not found. Id: {id}" });

            // 2. Mark as archived
            var update = Builders<TbOrder>.Update
                // Set the new IsArchive property to true
                .Set(o => o.IsArchive, true)
                .Set(o => o.UpdatedAt, DateTime.UtcNow);

            await _mongo.TbOrder.UpdateOneAsync(i => i.Id == id, update);

            // 3. Log the action
            var userId = HttpContext.Session.GetString("UserId") ?? "unknown";
            var actionLog = new ActionLog
            {
                UserId = userId,
                Entity = "Order", // Updated entity name
                EntityId = id,
                Action = "ARCHIVE",
                Description = $"Archived order #{order.OrderNumber}",
                TimeStamp = DateTime.UtcNow
            };

            await _mongo.ActionLog.InsertOneAsync(actionLog);

            return Json(new { success = true, message = "Order archived successfully." });
        }


        //[HttpPost]
        //public async Task<IActionResult> CreateInvoice(Invoice invoice)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    // Remove items with zero or negative quantity
        //    invoice.Items = invoice.Items
        //        .Where(i => i.Quantity > 0)
        //        .ToList();

        //    if (invoice.Items.Count == 0)
        //        return BadRequest("Invoice must contain at least one item with quantity greater than 0.");

        //    // Auto-generate invoice number
        //    invoice.InvoiceNumber = await GenerateInvoiceNumber();
        //    invoice.CreatedAt = DateTime.UtcNow;
        //    invoice.UpdatedAt = null;
        //    invoice.Status = "Unpaid";

        //    if (invoice.Items == null)
        //        invoice.Items = new List<ProductSales>();

        //    // ✅ Insert the invoice
        //    await _mongo.Invoices.InsertOneAsync(invoice);

        //    // ✅ After insert, invoice.Id now holds the generated ObjectId
        //    var userId = HttpContext.Session.GetString("UserId");
        //    var actionLog = new ActionLog(
        //        userId: userId, // or your actual logged-in user’s ID
        //        entity: "Invoice",
        //        entityId: invoice.Id!, // use the generated Id here
        //        action: "CREATE",
        //        description: $"Created invoice #{invoice.InvoiceNumber}"
        //    );

        //    await _mongo.ActionLog.InsertOneAsync(actionLog);

        //    ViewBag.NextInvoiceNumber = await GenerateInvoiceNumber();
        //    return RedirectToAction("Invoices");
        //}
        [HttpPost]
        public async Task<IActionResult> CreateInvoice(TbOrder order, string BilledTo)
        {
            // 1. Remove items with zero/negative quantity
            order.Items = order.Items
                .Where(i => i.Quantity > 0)
                .ToList();

            if (order.Items.Count == 0)
                return BadRequest("Order must contain at least one item with quantity greater than 0.");

            // 2. Fetch Customer Details
            var customer = await _mongo.TbUserCollection
                .Find(u => u.Id == BilledTo)
                .FirstOrDefaultAsync();

            if (customer == null)
                return BadRequest("Invalid Customer Selected.");

            // Map Customer Address
            order.UserId = customer.Id;
            order.ShippingAddress = new ShippingAddress
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                Phone = customer.Phone,
                Street = customer.Address?.Street ?? "",
                City = customer.Address?.City ?? "",
                State = customer.Address?.State ?? "",
                Country = customer.Address?.Country ?? "",
                ZipCode = customer.ZipCode,
                FullAddress = customer.Address?.FullAddress ?? ""
            };

            // 3. Set Auto-Generated Fields for Order
            order.OrderNumber = await GenerateInvoiceNumber();
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            order.PaymentStatus = "Unpaid";
            order.OrderStatus = "Processing";

            // 4. Calculate Totals & Prepare Inventory Updates
            decimal subtotal = 0;
            var productSalesList = new List<ProductSales>();

            foreach (var item in order.Items)
            {
                // A. Calculate Subtotal
                item.Subtotal = item.Price * item.Quantity;
                subtotal += item.Subtotal;

                // B. Prepare ProductSales Record
                // Note: We use the VariantId (item.ProductId) here. 
                var saleRecord = new ProductSales
                {
                    ProductId = item.ProductId, // This corresponds to the Variant Id
                    Item = item.ProductName,
                    Quantity = item.Quantity,
                    SalePrice = item.Price,
                    SRP = item.Price, // Assuming SRP is the same as selling price for this transaction
                    TransactionDate = DateTime.UtcNow,
                    SaleTax = 0, // Default to 0 unless you calculate tax per item
                    SaleDiscounts = 0 // Default to 0 unless you calculate discount per item
                };
                productSalesList.Add(saleRecord);

                // C. Decrement Stock Immediately (or you can use BulkWrite for optimization)
                var filter = Builders<ProductVariant>.Filter.Eq(v => v.Id, item.ProductId);
                var update = Builders<ProductVariant>.Update.Inc(v => v.StockQuantity, -item.Quantity);

                // Ensure we don't go below zero? (Optional validation, currently just decrements)
                await _mongo.ProductVariantInventory.UpdateOneAsync(filter, update);
            }

            order.Subtotal = subtotal;
            order.Tax = 0;
            order.ShippingFee = 0;
            order.TotalAmount = subtotal + order.Tax + order.ShippingFee;

            // 5. Insert Records into MongoDB

            // A. Insert the Order
            await _mongo.TbOrder.InsertOneAsync(order);

            // B. Insert the Product Sales Records (Batch Insert)
            if (productSalesList.Count > 0)
            {
                await _mongo.ProductSales.InsertManyAsync(productSalesList);
            }

            // 6. Log Action
            var userId = HttpContext.Session.GetString("UserId");
            var actionLog = new ActionLog(
                userId: userId ?? "System",
                entity: "Order",
                entityId: order.Id!,
                action: "CREATE",
                description: $"Created order #{order.OrderNumber}"
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

            var productSales = _mongo.ProductSalesInventory.Find(product => product.Id == productId).ToList();

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
                return BadRequest("Invalid order ID or status.");

            // 1. Find the Order (using TbOrder)
            var order = await _mongo.TbOrder.Find(i => i.Id == id).FirstOrDefaultAsync();
            _logger.LogInformation("\n\n\n\n" + order.OrderNumber + "\n\n\n");


            if (order == null)
                return NotFound("Order not found.");

            // 2. Prepare Update
            var filter = Builders<TbOrder>.Filter.Eq(i => i.Id, id);

            // We update 'PaymentStatus' because the UI dropdown selects Paid/Unpaid.
            // We also update 'UpdatedAt'.
            var update = Builders<TbOrder>.Update
                .Set(i => i.PaymentStatus, newStatus)
                .Set(i => i.UpdatedAt, DateTime.UtcNow);

            // Optional: If you want logic where "Paid" also automatically sets OrderStatus to "Processing" or "Completed", add it here.
            // e.g. if (newStatus == "Paid") update = update.Set(i => i.OrderStatus, "Processing");

            // 3. Execute Update
            var result = await _mongo.TbOrder.UpdateOneAsync(filter, update);
            _logger.LogInformation("\n\n\n\\n\n\n\nI'm  n\n\n");

            if (result.MatchedCount == 0)
                return NotFound("Order not found.");

            // 4. Log Action
            var userId = HttpContext.Session.GetString("UserId");
            var actionLog = new ActionLog(
                userId: userId ?? "System",
                entity: "Order", // Changed from Invoice
                entityId: id,
                action: "Update",
                description: $"Updated payment status of order #{order.OrderNumber} to {newStatus}"
            );

            await _mongo.ActionLog.InsertOneAsync(actionLog);

            _logger.LogInformation("\n\n\n\\n\n\n\nI'm here inserting log n\n\n");
            // 4. Log Action...
            return View("Invoices");
        } // Correct JSON response        }
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
        // Note: You must include a using statement for the new Display Model
        // using Sheessential_Sales_Finance.Models; 

        public IActionResult Expenses(
    // Expense filters
    string? sortOrder,
    string[]? departments,
    string[]? status,
    decimal? minAmount,
    decimal? maxAmount,
    DateTime? startDate,
    DateTime? endDate,

    // Ingredient filters
    string[]? ingredientStatus,
    DateTime? ingredientStartDate,
    DateTime? ingredientEndDate,
    int? minQty,
    int? maxQty,
    string? supplier,

              // Payroll filters
    string[]? payrollStatus,
    DateTime? startPeriod,
    DateTime? endPeriod,
    string? payType,
    int? minEmployees,
    int? maxEmployees,
    decimal? minGross,
    decimal? maxGross,
    decimal? minNet,
    decimal? maxNet
)
        {
            try
            {
                // --------------------
                // 1. FILTER EXPENSES
                // --------------------
                var expenseFilter = Builders<Expenses>.Filter.Eq(e => e.isIngredientsRequest, false);

                if (status != null && status.Length > 0)
                    expenseFilter &= Builders<Expenses>.Filter.In(e => e.Status, status);

                if (departments != null && departments.Length > 0)
                    expenseFilter &= Builders<Expenses>.Filter.In(e => e.Department, departments);

                if (minAmount.HasValue)
                    expenseFilter &= Builders<Expenses>.Filter.Gte(e => e.Amount, minAmount.Value);
                if (maxAmount.HasValue)
                    expenseFilter &= Builders<Expenses>.Filter.Lte(e => e.Amount, maxAmount.Value);

                if (startDate.HasValue)
                    expenseFilter &= Builders<Expenses>.Filter.Gte(e => e.RequestedAt, startDate.Value);
                if (endDate.HasValue)
                    expenseFilter &= Builders<Expenses>.Filter.Lte(e => e.RequestedAt, endDate.Value.AddDays(1).AddSeconds(-1));

                var expenses = _mongo.Expenses.Find(expenseFilter).ToList();

                // Sorting Expenses
                expenses = sortOrder switch
                {
                    "date_desc" => expenses.OrderByDescending(e => e.RequestedAt).ToList(),
                    "date_asc" => expenses.OrderBy(e => e.RequestedAt).ToList(),
                    "amount_desc" => expenses.OrderByDescending(e => e.Amount).ToList(),
                    "amount_asc" => expenses.OrderBy(e => e.Amount).ToList(),
                    _ => expenses.OrderByDescending(e => e.RequestedAt).ToList()
                };

                // ------------------------------
                // 2. FILTER INGREDIENT REQUESTS
                // ------------------------------
                var ingredientFilter = Builders<IngredientStockRequests>.Filter.Empty;

                if (ingredientStatus != null && ingredientStatus.Length > 0)
                    ingredientFilter &= Builders<IngredientStockRequests>.Filter.In(r => r.RequestStatus, ingredientStatus);

                if (ingredientStartDate.HasValue)
                    ingredientFilter &= Builders<IngredientStockRequests>.Filter.Gte(r => r.RequestDate, ingredientStartDate.Value);

                if (ingredientEndDate.HasValue)
                    ingredientFilter &= Builders<IngredientStockRequests>.Filter.Lte(r => r.RequestDate, ingredientEndDate.Value.AddDays(1).AddSeconds(-1));

                if (minQty.HasValue)
                    ingredientFilter &= Builders<IngredientStockRequests>.Filter.Gte(r => r.QuantityRequested, minQty.Value);

                if (maxQty.HasValue)
                    ingredientFilter &= Builders<IngredientStockRequests>.Filter.Lte(r => r.QuantityRequested, maxQty.Value);

                if (!string.IsNullOrEmpty(supplier))
                {
                    if (ObjectId.TryParse(supplier, out ObjectId supplierId))
                    {
                        ingredientFilter &= Builders<IngredientStockRequests>.Filter.Eq(r => r.SupplierId, supplierId);
                    }
                }

                var rawRequests = _mongo.IngredientsStockRequests.Find(ingredientFilter).ToList();


                // --------------------
                // 6. FILTER PAYROLL RUNS
                // --------------------
                var payrollFilter = Builders<PayrollRun>.Filter.Empty;

                if (payrollStatus != null && payrollStatus.Length > 0)
                    payrollFilter &= Builders<PayrollRun>.Filter.In(p => p.Status, payrollStatus);

                if (startPeriod.HasValue)
                    payrollFilter &= Builders<PayrollRun>.Filter.Gte(p => p.PayPeriodStart, startPeriod.Value);

                if (endPeriod.HasValue)
                    payrollFilter &= Builders<PayrollRun>.Filter.Lte(p => p.PayPeriodEnd, endPeriod.Value);

                if (!string.IsNullOrEmpty(payType))
                    payrollFilter &= Builders<PayrollRun>.Filter.Eq(p => p.PayPeriodType, payType);

                if (minEmployees.HasValue)
                    payrollFilter &= Builders<PayrollRun>.Filter.Gte(p => p.TotalEmployees, minEmployees.Value);

                if (maxEmployees.HasValue)
                    payrollFilter &= Builders<PayrollRun>.Filter.Lte(p => p.TotalEmployees, maxEmployees.Value);

                if (minGross.HasValue)
                    payrollFilter &= Builders<PayrollRun>.Filter.Gte(p => p.TotalGrossSalary, minGross.Value);

                if (maxGross.HasValue)
                    payrollFilter &= Builders<PayrollRun>.Filter.Lte(p => p.TotalGrossSalary, maxGross.Value);

                if (minNet.HasValue)
                    payrollFilter &= Builders<PayrollRun>.Filter.Gte(p => p.TotalNetSalary, minNet.Value);

                if (maxNet.HasValue)
                    payrollFilter &= Builders<PayrollRun>.Filter.Lte(p => p.TotalNetSalary, maxNet.Value);

                // Fetch filtered payroll runs
                var payrollRuns = _mongo.ParyrollRuns.Find(payrollFilter).ToList();

                // Lookup dictionaries for display
                var allIngredients = _mongo.Ingredients.Find(_ => true).ToList().ToDictionary(i => i.Id, i => i);
                var allSuppliers = _mongo.Suppliers.Find(_ => true).ToList().ToDictionary(s => s.Id, s => s);

                var displayRequests = rawRequests.Select(r => new IngredientStockRequestDisplayModel
                {
                    Id = r.Id,
                    ExpenseId = r.ExpenseId?.ToString(),
                    RequestStatus = r.RequestStatus,
                    TotalCost = r.TotalCost,
                    RequestDate = r.RequestDate,
                    RequestedBy = r.RequestedBy,
                    QuantityRequested = r.QuantityRequested,
                    Unit = r.Unit,
                    CurrentStockAtRequest = r.CurrentStockAtRequest,
                    Instructions = r.Instructions,
                    IngredientName = allIngredients.GetValueOrDefault(r.IngredientId.ToString())?.IngredientName ?? "Unknown Ingredient",
                    SupplierName = allSuppliers.GetValueOrDefault(r.SupplierId.ToString())?.SupplierName ?? "Unknown Supplier"
                }).OrderByDescending(r => r.RequestDate).ToList();

                // --------------------
                // 3. FETCH BALANCE
                // --------------------
                var balance = _mongo.Balance.Find(_ => true).FirstOrDefault() ?? new Balance();

                // --------------------
                // 4. CALCULATE TOTALS
                // --------------------
                ViewBag.TotalExpenses = expenses.Sum(e => e?.Amount ?? 0);
                ViewBag.PendingTotal = expenses.Where(e => e.Status == "Pending").Sum(e => e.Amount);
                ViewBag.ApprovedTotal = expenses.Where(e => e.Status == "Approved").Sum(e => e.Amount);
                ViewBag.DeclinedTotal = expenses.Where(e => e.Status == "Declined").Sum(e => e.Amount);
                ViewBag.TotalStockRequestCost = displayRequests.Sum(r => r.TotalCost);

                // --------------------
                // 5. BUILD VIEW MODEL
                // --------------------
                var viewModel = new ExpensesWithBalanceViewModel
                {
                    Expenses = expenses,
                    Balance = balance,
                    StockRequests = displayRequests,
                    PayrollRuns = payrollRuns // filtered runs
                };


                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Expenses page with filters.");
                return View(new ExpensesWithBalanceViewModel());
            }
        }





        [HttpPost]
        public IActionResult DeclineExpense(string id, bool isStockRequest, string DeclineReason)
        {
            _logger.LogInformation("\n\n\n\n\n\n I'm here brooooooo we're declining \n\n\n\n\n\n");

            bool updated = false;

            if (isStockRequest)
            {
                // Update IngredientStockRequests
            _logger.LogInformation("\n\n\n\n\n\n Updating: "+id+ "\n\n\n\n\n\n");
                var filter = Builders<IngredientStockRequests>.Filter.Eq(r => r.Id, id);

                var update = Builders<IngredientStockRequests>.Update
                    .Set(r => r.RequestStatus, "Declined")
                    .Set(r => r.RejectionReason, DeclineReason)
                    .Set(r => r.StatusUpdatedDate, DateTime.UtcNow);

                var result = _mongo.IngredientsStockRequests.UpdateOne(filter, update);

                updated = result.MatchedCount > 0 && result.ModifiedCount > 0;

                _logger.LogInformation($"StockRequest Updated? {updated} | Matched: {result.MatchedCount} | Modified: {result.ModifiedCount}");
            }
            else
            {
                // Update Expenses collection
                var filter = Builders<Expenses>.Filter.Eq(e => e.Id, id);

                var update = Builders<Expenses>.Update
                    .Set(e => e.Status, "Declined")
                    .Set(e => e.Notes, DeclineReason);

                var result = _mongo.Expenses.UpdateOne(filter, update);

                updated = result.MatchedCount > 0 && result.ModifiedCount > 0;

                _logger.LogInformation($"Expense Updated? {updated} | Matched: {result.MatchedCount} | Modified: {result.ModifiedCount}");
            }

            if (!updated)
            {
                TempData["DeclineError"] = "❌ Decline failed — no document was updated.";
            }
            else
            {
                TempData["DeclineSuccess"] = "✔️ Declined successfully.";
            }

            return RedirectToAction("Expenses");
        }



        ///
        [HttpPost]
        public IActionResult UpdatedStockRequestStatus(string id, string expenseId)
        {
            _logger.LogInformation("\n\n\n\n\n\n I'm approving \n\n\n\n");
            var filter = Builders<IngredientStockRequests>.Filter.Eq(r => r.Id, id);

            var update = Builders<IngredientStockRequests>.Update
                .Set(r => r.RequestStatus, "Approved by Finance")
                .Set(r => r.ExpenseId, string.IsNullOrWhiteSpace(expenseId) ? null : new ObjectId(expenseId));

            _mongo.IngredientsStockRequests.UpdateOne(filter, update);

            return Json(new { success = true, message = "Stock request approved." });
        }




        // add the logic for updating stock request
        //accept expense
        [HttpPost]
        public IActionResult ApproveExpense(
           string id,
           string requestId,
           string TransferTo,
           string PaymentMethod,
           string ReferenceNumber,
           string TransferNotes)
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

                // ✅ 2. Find the current balance
                var balance = _mongo.Balance.Find(_ => true).FirstOrDefault();

                if (balance != null)
                {
                    // ✅ 3. Check sufficiency
                    if (balance.CurrentBalance >= expense.Amount)
                    {
                        // --- TRANSACTION START ---

                        // A. Deduct the amount
                        balance.CurrentBalance -= expense.Amount;

                        // B. Update Balance in DB
                        var balanceFilter = Builders<Balance>.Filter.Eq(b => b.Id, balance.Id);
                        var balanceUpdate = Builders<Balance>.Update.Set(b => b.CurrentBalance, balance.CurrentBalance);
                        _mongo.Balance.UpdateOne(balanceFilter, balanceUpdate);                        
                        
                        
                        // B. Update Balance in DB
                        var stockRequestFilter = Builders<IngredientStockRequests>.Filter.Eq(b => b.Id, balance.Id);
                        // Replace this line:
                        // var stockUpdate = Builders<IngredientStockRequests>.Update.Set(b => b.ExpenseId, id);

                        // With this corrected line:
                        var stockUpdate = Builders<IngredientStockRequests>.Update.Set("ExpenseId", id);
                        _mongo.IngredientsStockRequests.UpdateOne(stockRequestFilter, stockUpdate);

                        // C. CREATE THE PAYMENT TRANSACTION RECORD (New Logic)
                        var newTransaction = new PaymentTransaction
                        {
                            ExpenseId = id,
                            Amount = expense.Amount,
                            PaymentDate = DateTime.UtcNow,
                            TransferTo = TransferTo,
                            PaymentMethod = PaymentMethod,
                            ReferenceNumber = ReferenceNumber ?? "N/A", // Handle nulls if optional
                            Notes = TransferNotes ?? ""
                        };

                        // Save transaction to a new collection (e.g., PaymentTransactions)
                        _mongo.PaymentTransactions.InsertOne(newTransaction);
                        if (newTransaction.Id != null)
                        {
                            _logger.LogInformation("\n\n\n we inserted" + newTransaction.Id);
                        }
                        else
                        {
                            _logger.LogInformation("\n\\n\nBobo\n\n\n");
                        }
                        // D. Update Expense Status
                        var expenseFilter = Builders<Expenses>.Filter.Eq(e => e.Id, id);
                        var expenseUpdate = Builders<Expenses>.Update
                            .Set(e => e.Status, "Approved")
                            .Set(e => e.DateApproved, DateTime.UtcNow); // Or maybe add a "PaidAt" field?

                        _mongo.Expenses.UpdateOne(expenseFilter, expenseUpdate);

                        _logger.LogInformation($"Expense {id} paid to {TransferTo}.");
                    }
                    else
                    {
                        TempData["Error"] = "Insufficient balance to approve this expense.";
                    }
                }
                else
                {
                    _logger.LogWarning("No balance record found.");
                }

                return RedirectToAction("Expenses");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error approving expense {id}");
                TempData["Error"] = "An error occurred.";
                return RedirectToAction("Expenses");
            }
        }


        // Add new expense
        [HttpPost]
        public IActionResult AddExpense(Expenses newExpense)
        {
            _logger.LogInformation("\n\n\n\n\n Im in Add expense \n\n\n");
            var lastExpense = _mongo.Expenses
                .Find(_ => true)
                .SortByDescending(e => e.ExpenseId)
                .FirstOrDefault();

            int nextNumber = 1;
            if (lastExpense != null && !string.IsNullOrEmpty(lastExpense.ExpenseId))
            {
                string lastNumberPart = lastExpense.ExpenseId.Replace("EXP-", "");
                if (int.TryParse(lastNumberPart, out int lastNumber))
                    nextNumber = lastNumber + 1;
            }

            newExpense.ExpenseId = $"EXP-{nextNumber:D4}";
            newExpense.Status = "Pending";
            newExpense.RequestedAt = DateTime.UtcNow;

            _mongo.Expenses.InsertOne(newExpense);

            return Json(new { success = true, expenseId = newExpense.Id });
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


        [HttpGet]
        public IActionResult GetExpenseAndPaymentDetails(string expenseId)
        {
            _logger.LogInformation("I'm in Get ikspinis mitod");
            _logger.LogInformation("\n\n\n\n"+expenseId+"\n\n\n\n");
            try
            {
                // 1. Fetch the Expense
                var expense = _mongo.Expenses.Find(e => e.Id == expenseId).FirstOrDefault();

                if (expense == null)
                {
                    _logger.LogInformation("No found bruuhhh");
                    return NotFound(new { success = false, message = "Expense not found." });
                }

                // 2. Fetch the corresponding Payment Transaction using the ExpenseId
                var paymentTransaction = _mongo.PaymentTransactions
                                               .Find(p => p.ExpenseId == expenseId)
                                               .FirstOrDefault();
                _logger.LogInformation("I'm should be succed");

                // 3. Return both objects as a combined result
                return Json(new
                {

                    success = true,
                    expense = expense,
                    payment = paymentTransaction // Will be null if payment hasn't been recorded yet
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching expense and payment details for ID {expenseId}");
                return StatusCode(500, new { success = false, message = "Internal server error." });
            }
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
            ViewBag.GeneratedBy = HttpContext.Session.GetString("UserName");

            return View();
        }
        public async Task<IActionResult> SalesReport()
        {
            var categories = await _mongo.Inventories
                .Distinct<string>("category", filter: Builders<Products>.Filter.Empty)
                .ToListAsync();

            ViewBag.Categories = categories;

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
                "week" => now.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday), // start of week (Monday)
                "month" => new DateTime(now.Year, now.Month, 1),                     // start of month
                "year" => new DateTime(now.Year, 1, 1),                              // Jan 1
                "all" => DateTime.MinValue,                                          // all data
                _ => now.AddDays(-7)
            };

            // ✅ Fetch paid invoices within period
            var ProductSales = await _mongo.ProductSalesInventory.Find(i =>
                i.TransactionDate >= start && i.TransactionDate <= now)
                .ToListAsync();

            var viewModel = new SalesReportViewModel
            {
                TotalSales = ProductSales.Sum(sale => decimal.Parse(sale.SalePrice)),
                TotalOrders = ProductSales.Count,
            };

            // ✅ Flatten items and filter by date range


            // ✅ Collect distinct ProductIds
            var productIds = ProductSales.Select(i => i.Id).Distinct().ToList();

            // ✅ Fetch product names and categories
            var products = await _mongo.ProductVariantInventory
                .Find(p => productIds.Contains(p.Id))
                .ToListAsync();

            // ✅ Lookup dictionaries
            var productLookup = products.ToDictionary(p => p.Id, p => p.VariantName);
            var categoryLookup = products.ToDictionary(p => p.Id, p => p.Category);

            // ✅ Chart data
            if (period == "all" || period == "year")
            {
                viewModel.ChartLabels = ProductSales
                    .GroupBy(i => new { i.TransactionDate.Year, i.TransactionDate.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"))
                    .ToList();

                viewModel.ChartValues = ProductSales
                    .GroupBy(i => new { i.TransactionDate.Year, i.TransactionDate.Month })
                    .Select(g => g.Sum(x => decimal.Parse(x.SalePrice) * x.Quantity))
                    .ToList();
            }
            else
            {
                viewModel.ChartLabels = ProductSales
                    .GroupBy(i => i.TransactionDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => g.Key.ToString("MMM dd"))
                    .ToList();

                viewModel.ChartValues = ProductSales
                    .GroupBy(i => i.TransactionDate.Date)
                    .Select(g => g.Sum(x => decimal.Parse(x.SalePrice) * x.Quantity))
                    .ToList();
            }

            // ✅ Top Products
            viewModel.TopProducts = ProductSales
                .GroupBy(i => i.VariantId)
                .Select(g => new TopProductDto
                {
                    ProductName = productLookup.ContainsKey(g.Key)
                        ? productLookup[g.Key]
                        : "(Unknown Product)",
                    TotalAmount = g.Sum(x => decimal.Parse(x.SalePrice) * x.Quantity)
                })
                .OrderByDescending(p => p.TotalAmount)
                .Take(5)
                .ToList();

            // ✅ Product Sales Table (added Category)
            viewModel.SalesRows = ProductSales
                .GroupBy(x => x.VariantId)
                .Select(g => new ProductSalesRow
                {
                    ProductId = g.Key ?? "(Unknown)",
                    ProductName = productLookup.ContainsKey(g.Key)
                        ? productLookup[g.Key]
                        : "(Unknown Product)",
                    Category = categoryLookup.ContainsKey(g.Key)
                        ? categoryLookup[g.Key]
                        : "(Unknown)",
                    UnitPrice = g.Average(x => decimal.Parse(x.SalePrice)),
                    Quantity = g.Sum(x => x.Quantity),
                    TotalAmount = g.Sum(x => decimal.Parse(x.SalePrice) * x.Quantity)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            // ✅ Period text
            viewModel.PeriodText = $"{start.ToLocalTime():MMMM d, yyyy} – {now.ToLocalTime():MMMM d, yyyy}";

            return Json(viewModel);
        }



        [HttpPost]
        public IActionResult ExportPdfPreview([FromBody] FinancePdfPayload payload)
        {
            _logger.LogInformation("Generating PDF Preview...");
            var logoPath = Path.Combine(_env.WebRootPath, "images/Logo.png");
            var logoBytes = System.IO.File.ReadAllBytes(logoPath);
            var base64Logo = Convert.ToBase64String(logoBytes);

            payload.LogoBase64 = $"data:image/png;base64,{base64Logo}";


            var html = RenderViewToString("FinancePdfTemplate", payload);

            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = new GlobalSettings
                {
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings { Top = 10, Bottom = 10 }
                },
                Objects = {
            new ObjectSettings {
                HtmlContent = html,
                WebSettings = { DefaultEncoding = "utf-8", LoadImages = true }
            }
        }
            };

            var pdf = _converter.Convert(doc);

            return File(pdf, "application/pdf");
        }

        [HttpPost]
        public IActionResult ExportProductSalesPdfPreview([FromBody] SalesPdfPayload payload)
        {
            _logger.LogInformation("Generating Sales PDF...");

            var logoPath = Path.Combine(_env.WebRootPath, "images/Logo.png");
            var logoBytes = System.IO.File.ReadAllBytes(logoPath);
            var base64Logo = Convert.ToBase64String(logoBytes);

            payload.LogoBase64 = $"data:image/png;base64,{base64Logo}";

            var html = RenderViewToString("SalesPdfTemplate", payload);

            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = new GlobalSettings
                {
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings { Top = 10, Bottom = 10 }
                },
                Objects = {
            new ObjectSettings {
                HtmlContent = html,
                WebSettings = { DefaultEncoding = "utf-8", LoadImages = true }
            }
        }
            };

            var pdf = _converter.Convert(doc);
            return File(pdf, "application/pdf");
        }
        public IActionResult Payroll()
        {
            var payroll = new List<PayrollViewModel>
    {
        new() { EmployeeId = "EMP001", FullName = "Juan C. Dela Cruz", Department = "Marketing", Position = "Marketing Assistant", GrossPay = 32000, IsComputed = false },
        new() { EmployeeId = "EMP002", FullName = "John Rivera", Department = "Finance", Position = "Accounting Staff", GrossPay = 28500, IsComputed = true },
        new() { EmployeeId = "EMP003", FullName = "Looney Tunes", Department = "Sales", Position = "Sales Executive", GrossPay = 30000, IsComputed = false },
        new() { EmployeeId = "EMP004", FullName = "Mickey Mouse", Department = "Human Resource", Position = "HR Officer", GrossPay = 35000, IsComputed = true },
        new() { EmployeeId = "EMP005", FullName = "Marian Grey", Department = "Inventory", Position = "Warehouse Clerk", GrossPay = 26800, IsComputed = false },
        new() { EmployeeId = "EMP006", FullName = "Michael Jordan", Department = "IT", Position = "System Analyst", GrossPay = 40000, IsComputed = true }
    };

            // ✅ Make sure to pass the payroll list to the View
            return View(payroll);
        }


        [HttpGet]
        public async Task<IActionResult> GetChartData(string period = "year")
        {
            DateTime utcNow = DateTime.UtcNow;
            string periodLower = period.ToLower();

            // --- FOR ALL TIME VIEW (Multi-year comparison) ---
            if (periodLower == "alltime")
            {
                // 1. Fetch ALL data
                var allInvoicesTask = _mongo.ProductSalesInventory.AsQueryable()
                    .ToListAsync();

                var allExpensesTask = _mongo.Expenses.AsQueryable()
                    .ToListAsync();

                await Task.WhenAll(allInvoicesTask, allExpensesTask);

                var allInvoices = allInvoicesTask.Result;
                var allExpenses = allExpensesTask.Result;

                // 2. Process Expense Breakdown (Doughnut)
                var expenseBreakdown = allExpenses
                    .GroupBy(e => e.ExpenseType)
                    .Select(g => new { Label = g.Key, Data = g.Sum(e => e.Amount) })
                    .ToList();

                // 3. Process Revenue Trend (Line Chart)
                // Group by Year, then create a dataset for each year
                var revenueDatasets = allInvoices
                    .GroupBy(i => i.TransactionDate.Year)
                    .OrderBy(g => g.Key)
                    .Select(yearGroup => new
                    {
                        Label = yearGroup.Key.ToString(), // e.g., "2023", "2024"
                        Data = Enumerable.Range(1, 12)
                            .Select(month => yearGroup
                                .Where(i => i.TransactionDate.Month == month)
                                .Sum(i => decimal.Parse(i.SalePrice)))
                            .ToList()
                    })
                    .ToList();

                // Labels are always months for "All Time" view
                var revenueLabels = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames
                    .Take(12).ToList(); // "Jan", "Feb", ...

                // 4. Return JSON for "All Time"
                return Json(new
                {
                    revenueTrend = new // Renamed from profitTrend
                    {
                        Labels = revenueLabels,
                        Datasets = revenueDatasets // This is an array of objects
                    },
                    expenseBreakdown = new
                    {
                        Labels = expenseBreakdown.Select(e => e.Label).ToList(),
                        Datasets = new[] { new { Data = expenseBreakdown.Select(e => e.Data).ToList() } }
                    }
                });
            }

            // --- FOR "WEEK", "MONTH", "YEAR" VIEWS (Single revenue line) ---

            // 1. Determine Date Range (existing logic)
            DateTime startDate;
            DateTime endDate;
            switch (periodLower)
            {
                case "week":
                    startDate = utcNow.Date.AddDays(-(int)utcNow.DayOfWeek);
                    endDate = startDate.AddDays(7);
                    break;
                case "month":
                    startDate = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                    endDate = startDate.AddMonths(1);
                    break;
                default: // "year"
                    startDate = new DateTime(utcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    endDate = startDate.AddYears(1);
                    break;
            }

            // 2. Fetch Filtered Data
            var invoicesTask = _mongo.Invoices.AsQueryable()
                .Where(i => i.IssuedAt >= startDate && i.IssuedAt < endDate && !i.IsArchived)
                .ToListAsync();

            var expensesTask = _mongo.Expenses.AsQueryable()
                .Where(e => e.RequestedAt >= startDate && e.RequestedAt < endDate)
                .ToListAsync();

            await Task.WhenAll(invoicesTask, expensesTask);

            var relevantInvoices = invoicesTask.Result;
            var relevantExpenses = expensesTask.Result;

            // 3. Process Expense Breakdown (Doughnut)
            var filteredExpenseBreakdown = relevantExpenses
                .GroupBy(e => e.ExpenseType)
                .Select(g => new { Label = g.Key, Data = g.Sum(e => e.Amount) })
                .ToList();

            // 4. Process Revenue Trend (Line Chart)
            List<string> lineChartLabels;
            List<decimal> revenueData; // Changed from salesData

            if (periodLower == "year")
            {
                lineChartLabels = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames.Take(12).ToList();
                revenueData = Enumerable.Range(1, 12)
                    .Select(month => relevantInvoices.Where(i => i.IssuedAt.Month == month).Sum(i => i.Total))
                    .ToList();
            }
            else if (periodLower == "month")
            {
                int daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
                lineChartLabels = Enumerable.Range(1, daysInMonth).Select(d => d.ToString()).ToList();
                revenueData = Enumerable.Range(1, daysInMonth)
                    .Select(day => relevantInvoices.Where(i => i.IssuedAt.Day == day).Sum(i => i.Total))
                    .ToList();
            }
            else // "week"
            {
                lineChartLabels = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames.ToList();
                revenueData = Enumerable.Range(0, 7)
                    .Select(day => relevantInvoices.Where(i => (int)i.IssuedAt.DayOfWeek == day).Sum(i => i.Total))
                    .ToList();
            }

            // 5. Return JSON for "week", "month", "year"
            return Json(new
            {
                revenueTrend = new // Renamed from profitTrend
                {
                    Labels = lineChartLabels,
                    // Note: Datasets is an array with ONE object
                    Datasets = new[]
                    {
                new { Label = "Revenue", Data = revenueData }
            }
                },
                expenseBreakdown = new
                {
                    Labels = filteredExpenseBreakdown.Select(e => e.Label).ToList(),
                    Datasets = new[] { new { Data = filteredExpenseBreakdown.Select(e => e.Data).ToList() } }
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> ExportProductSalesPdf([FromBody] SalesPdfload payload)
        {

            _logger.LogInformation("\n\n\n\n\n\nGenerating Sales PDF...\n\n\n\n\n\n");
            // Get Logo
            var logoPath = Path.Combine(_env.WebRootPath, "images/Logo.jpg"); // Make sure you have this logo
            var logoBytes = System.IO.File.ReadAllBytes(logoPath);
            payload.LogoBase64 = $"data:image/png;base64,{Convert.ToBase64String(logoBytes)}";

            // Set generation date
            payload.DateGenerated = DateTime.Now.ToString("MM/dd/yyyy, h:mm:ss tt");

            // Render Razor View to HTML
            var html = await RenderViewToStringAsync("NewSalesPdfTemplate", payload);

            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = new GlobalSettings
                {
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 }
                },
                Objects = {
                    new ObjectSettings {
                        HtmlContent = html,
                        WebSettings = { DefaultEncoding = "utf-8", LoadImages = true, EnableJavascript = true }
                    }
                }
            };

            var pdf = _converter.Convert(doc);
            return File(pdf, "application/pdf", $"Sales-Report-{DateTime.Now.Ticks}.pdf");
        }
        private async Task<string> RenderViewToStringAsync(string viewName, object model)
        {
            ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var viewResult = _viewEngine.FindView(ControllerContext, viewName, false);
                if (viewResult.View == null)
                {
                    throw new ArgumentNullException($"{viewName} does not match any available view");
                }

                var viewContext = new ViewContext(
                    ControllerContext,
                    viewResult.View,
                    ViewData,
                    TempData,
                    sw,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return sw.ToString();
            }
        }

        [HttpGet]
        public IActionResult GetSalesReportDatatry(string period)
        {
            // In a real app, you would query your database based on the 'period'
            // For this example, I'm returning mock data based on your PDF

            _logger.LogInformation("\n\n\n\nI'm in Sales Report Data Try \n\n\n\n");
            var reportData = new SalesReportDataDto
            {
                // Summary Stats
                Summary = new ReportSummary
                {
                    TotalSales = 1374.5m,
                    TotalOrders = 12,
                    ActiveCustomers = 2,
                    TopPerformingProduct = "Aloe Vera Gel 150ml"
                },
                // Data for the Sales Trend Line Chart
                SalesTrend = new List<ChartDataPoint>
                {
                    new ChartDataPoint { Label = "Oct 2025", Total = 350 },
                    new ChartDataPoint { Label = "Nov 2025", Total = 1024.5 }
                },
                // Data for the Top Products Doughnut Chart
                TopProductsChart = new List<ProductChartPoint>
                {
                    new ProductChartPoint { Name = "Aloe Vera Gel 150ml", Percentage = 34.9 },
                    new ProductChartPoint { Name = "Facial Toner 200ml", Percentage = 22.4 },
                    new ProductChartPoint { Name = "Moisturizing Face Cream", Percentage = 20.0 },
                    new ProductChartPoint { Name = "Argan Oil Hair Serum 100ml", Percentage = 13.3 },
                    new ProductChartPoint { Name = "Body Lotion - Lavender 250ml", Percentage = 9.5 }
                },
                // Data for the Top 5 Products Table
                TopProductsTable = new List<TransactionItem>
                {
                    new TransactionItem { ProductId = "68df7ff4e9a574db041d6950", ProductName = "Aloe Vera Gel 150ml", Category = "Skincare", UnitPrice = 12.5m, Quantity = 37, TotalAmount = 462.5m },
                    new TransactionItem { ProductId = "68df7ff4e9a574db041d6957", ProductName = "Facial Toner 200ml", Category = "Skincare", UnitPrice = 16.5m, Quantity = 18, TotalAmount = 297.0m },
                    new TransactionItem { ProductId = "68df7fe4e9a574db041d6941", ProductName = "Moisturizing Face Cream", Category = "Skincare", UnitPrice = 26.5m, Quantity = 10, TotalAmount = 265.0m },
                    new TransactionItem { ProductId = "68df7ff4e9a574db041d6952", ProductName = "Argan Oil Hair Serum 100ml", Category = "Haircare", UnitPrice = 22.0m, Quantity = 8, TotalAmount = 176.0m },
                    new TransactionItem { ProductId = "68df7ff4e9a574db041d6956", ProductName = "Body Lotion - Lavender 250ml", Category = "Bodycare", UnitPrice = 18.0m, Quantity = 7, TotalAmount = 126.0m }
                }
            };

            return Json(reportData);
        }


        [HttpPost]
        public async Task<IActionResult> ReleasePayroll(string id)
        {
            _logger.LogInformation("ReleasePayroll triggered for PayrollRun Id: {Id}", id);

            // 1. Find the payroll run
            var filter = Builders<PayrollRun>.Filter.Eq(r => r.Id, id);
            var payrollRun = await _mongo.ParyrollRuns.Find(filter).FirstOrDefaultAsync();

            if (payrollRun == null)
                return Json(new { success = false, message = "PayrollRun not found." });

            // 2. Decrement balance by total gross salary
            var balance = _mongo.Balance.Find(_ => true).FirstOrDefault();
            if (balance != null)
            {
                balance.CurrentBalance -= payrollRun.TotalGrossSalary;
                var balanceFilter = Builders<Balance>.Filter.Eq(b => b.Id, balance.Id);
                var balanceUpdate = Builders<Balance>.Update
                    .Set(b => b.CurrentBalance, balance.CurrentBalance)
                    .Set(b => b.LastUpdated, DateTime.UtcNow);
                _mongo.Balance.UpdateOne(balanceFilter, balanceUpdate);
            }

            // 3. Update payroll run status
            var update = Builders<PayrollRun>.Update
                .Set(r => r.Status, "Released")
                .Set(r => r.IsFinalized, true)
                .Set(r => r.IsSentToFinance, true)
                .Set(r => r.IsPayslipsGenerated, true)
                .Set(r => r.ReviewedBy, "Adrial")
                .Set(r => r.ReviewedAt, DateTime.UtcNow)
                .Set(r => r.ApprovedBy, "Adrial")
                .Set(r => r.ApprovalComments, "")
                .Set(r => r.UpdatedAt, DateTime.UtcNow);

            var result = await _mongo.ParyrollRuns.UpdateOneAsync(filter, update);
            if (result.ModifiedCount > 0)
                return Json(new { success = true });

            return Json(new { success = false });
        }

    }
}
