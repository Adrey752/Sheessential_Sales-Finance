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
using System.Text.Json;
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

            // --- Fetch Orders from TbOrder ---
            var allOrders = await _mongo.TbOrder
     .Find(_ => true)
     .SortByDescending(o => o.CreatedAt)
     .ToListAsync();

            // --- Calculate Revenue from TbOrder (Paid orders only) ---
            double revenue = (double)allOrders
     .Where(o => o.PaymentStatus == "Paid")
     .Sum(o => o.TotalAmount);

            // --- Fetch Expenses ---
            var expenses = await _mongo.Expenses
                .Find(e => e.Status == "Approved")
                .ToListAsync();
            double expense = (double)expenses.Sum(e => e.Amount);

            // --- Fetch Recent Logs ---
            var logs = await _mongo.ActionLog
                .Find(_ => true)
                .SortByDescending(l => l.TimeStamp)
                .Limit(10)
                .ToListAsync();

            var userIds = logs.Select(l => l.UserId).Distinct().ToList();
            var users = await _mongo.Users
                .Find(u => userIds.Contains(u.Id!))
                .ToListAsync();

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

            ViewBag.ActionLogs = enrichedLogs;

            // --- Build ViewModel ---
            var viewModel = new DashboardViewModel
            {
                Revenue = revenue,
                Expense = expense,
                NetProfit = revenue - expense,
                SalesCount = allOrders.Count,
                UserName = userName,
                TotalOrders = allOrders.Count,
                PaidOrders = allOrders.Count(o => o.PaymentStatus == "Paid"),
                UnpaidOrders = allOrders.Count(o => o.PaymentStatus == "Unpaid"),
                ProcessingOrders = allOrders.Count(o => o.OrderStatus == "Processing"),
                OrderRevenue = allOrders.Where(o => o.PaymentStatus == "Paid").Sum(o => o.TotalAmount),
                RecentOrders = allOrders.Take(5).ToList()
            };

            return View(viewModel);
        }


        // Replace the existing SalesReportData method with this one that handles all periods + custom dates

        [HttpGet]
        public async Task<IActionResult> SalesReportData(string period = "week", string? startDate = null, string? endDate = null)
        {
            period = period.ToLowerInvariant();
            var now = DateTime.UtcNow;

            DateTime start;
            DateTime end = now;

            // Handle custom date range
            if (period == "custom" && !string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                if (DateTime.TryParse(startDate, out DateTime parsedStart) &&
                    DateTime.TryParse(endDate, out DateTime parsedEnd))
                {
                    start = parsedStart.Date;
                    end = parsedEnd.Date.AddDays(1).AddTicks(-1);
                }
                else
                {
                    return Json(new SalesReportViewModel { PeriodText = "Invalid date range" });
                }
            }
            else
            {
                start = period switch
                {
                    "week" => now.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday),
                    "month" => new DateTime(now.Year, now.Month, 1),
                    "year" => new DateTime(now.Year, 1, 1),
                    "alltime" or "all" => DateTime.MinValue,
                    _ => now.AddDays(-7)
                };
            }

            // Fetch sales data within the period
            var filterBuilder = Builders<InventoryProductSales>.Filter;
            var filter = filterBuilder.Gte(i => i.TransactionDate, start) &
                         filterBuilder.Lte(i => i.TransactionDate, end);

            var productSales = await _mongo.ProductSalesInventory.Find(filter).ToListAsync();

            // Extract variant IDs and fetch variant details
            var variantIds = productSales
                .Where(i => !string.IsNullOrEmpty(i.VariantId))
                .Select(i => i.VariantId)
                .Distinct()
                .ToList();

            var productVariants = await _mongo.ProductVariantInventory
                .Find(p => variantIds.Contains(p.Id!))
                .ToListAsync();

            var nameLookup = productVariants
                .Where(p => p.Id != null)
                .ToDictionary(p => p.Id!, p => p.VariantName);
            var categoryLookup = productVariants
                .Where(p => p.Id != null)
                .ToDictionary(p => p.Id!, p => p.Category ?? "N/A");

            // Build the view model
            var viewModel = new SalesReportViewModel
            {
                TotalSales = productSales.Sum(sale =>
                    decimal.TryParse(sale.SalePrice, out decimal price) ? price * sale.Quantity : 0),
                TotalOrders = productSales.Count,
                PeriodText = start == DateTime.MinValue
                    ? "All Time"
                    : $"{start.ToLocalTime():MMM d, yyyy} – {end.ToLocalTime():MMM d, yyyy}"
            };

            // Chart Data (Revenue Trend)
            var dateSpan = (end - start).TotalDays;

            if (period == "alltime" || period == "all" || period == "year" || (period == "custom" && dateSpan > 90))
            {
                // Group by month
                var monthlySales = productSales
                    .GroupBy(i => new { i.TransactionDate.Year, i.TransactionDate.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new
                    {
                        DateLabel = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                        Total = g.Sum(x => decimal.TryParse(x.SalePrice, out decimal p) ? p * x.Quantity : 0)
                    })
                    .ToList();

                viewModel.ChartLabels = monthlySales.Select(m => m.DateLabel).ToList();
                viewModel.ChartValues = monthlySales.Select(m => m.Total).ToList();
            }
            else
            {
                // Group by day
                var dailySales = productSales
                    .GroupBy(i => i.TransactionDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        DateLabel = g.Key.ToString("MMM dd"),
                        Total = g.Sum(x => decimal.TryParse(x.SalePrice, out decimal p) ? p * x.Quantity : 0)
                    })
                    .ToList();

                viewModel.ChartLabels = dailySales.Select(d => d.DateLabel).ToList();
                viewModel.ChartValues = dailySales.Select(d => d.Total).ToList();
            }

            // Top Products (for Doughnut Chart)
            viewModel.TopProducts = productSales
                .GroupBy(i => i.VariantId)
                .Select(g => new TopProductDto
                {
                    ProductName = nameLookup.GetValueOrDefault(g.Key, "(Unknown Product)"),
                    TotalAmount = g.Sum(x => decimal.TryParse(x.SalePrice, out decimal p) ? p * x.Quantity : 0)
                })
                .OrderByDescending(p => p.TotalAmount)
                .Take(5)
                .ToList();

            // Sales Rows (for table)
            viewModel.SalesRows = productSales
                .GroupBy(x => x.VariantId)
                .Select(g =>
                {
                    decimal totalAmount = g.Sum(x => decimal.TryParse(x.SalePrice, out decimal p) ? p * x.Quantity : 0);
                    int totalQuantity = g.Sum(x => x.Quantity);
                    return new ProductSalesRow
                    {
                        ProductId = g.Key ?? "(Unknown)",
                        ProductName = nameLookup.GetValueOrDefault(g.Key, "(Unknown Product)"),
                        Category = categoryLookup.GetValueOrDefault(g.Key, "(Unknown)"),
                        UnitPrice = totalQuantity > 0 ? totalAmount / totalQuantity : 0,
                        Quantity = totalQuantity,
                        TotalAmount = totalAmount
                    };
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            return Json(viewModel);
        }

        public IActionResult Products(int page = 1)
        {
            int pageSize = 5;

            // --- 1. PRE-FETCH ALL PRODUCT DATA ---
            var allProducts = _mongo.ProductInventory.Find(_ => true)
                .ToList()
                .ToDictionary(p => p.Id, p => p);

            // --- 2. FETCH ALL PAID ORDERS (including archived) ---
            var paidOrders = _mongo.TbOrder
                .Find(o => o.PaymentStatus == "Paid")
                .ToList();

            // Flatten order items from all paid orders
            var allOrderItems = paidOrders
                .SelectMany(o => o.Items)
                .Where(i => i.Quantity > 0)
                .ToList();

            // --- CALCULATE TOTAL ORDERS AND SALES ---
            int totalOrders = paidOrders.Count;
            decimal totalSales = paidOrders.Sum(o => o.TotalAmount);

            // --- CALCULATE UNITS SOLD PER VARIANT ---
            var variantSalesCounts = allOrderItems
                .Where(i => i.ProductId != null)
                .GroupBy(i => i.ProductId)
                .ToDictionary(
                    g => g.Key!,
                    g => new { Orders = g.Count(), UnitsSold = g.Sum(x => x.Quantity) }
                );

            // --- 3. FETCH PAGINATED PRODUCT VARIANTS ---
            var totalProducts = (int)_mongo.ProductVariantInventory.CountDocuments(_ => true);
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            var pagedVariants = _mongo.ProductVariantInventory.Find(_ => true)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToList();

            // --- 4. ENRICH VARIANTS ---
            foreach (var variant in pagedVariants)
            {
                if (allProducts.TryGetValue(variant.ProductId, out var product))
                {
                    variant.Category = product.ProductCategory;
                    variant.Description = product.ProductDesc;
                    variant.VariantImg = $"/Sales_Finance/GetProductImage/{product.Id}"; // ✅ ADDED
                }
                else
                {
                    variant.Category = "N/A";
                    variant.Description = "No description available";
                }

                if (variant.Id != null && variantSalesCounts.TryGetValue(variant.Id, out var salesData))
                {
                    variant.OrdersCount = salesData.UnitsSold;
                }
                else
                {
                    variant.OrdersCount = 0;
                }
            }

            // --- 5. PASS DATA TO VIEW BAGS ---
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalSales = $"₱{totalSales:N2}";

            return View(pagedVariants);
        }

        [HttpGet]
        public IActionResult GetProductImage(string id)
        {
                var product = _mongo.ProductInventory
                .Find(p => p.Id == id)
                .FirstOrDefault();

            if (product == null || product.ProductImgRaw == null || product.ProductImgRaw.IsBsonNull)
                return NotFound();

            byte[] imageBytes;

            if (product.ProductImgRaw.IsBsonBinaryData)
                imageBytes = product.ProductImgRaw.AsBsonBinaryData.Bytes;
            else if (product.ProductImgRaw.IsString)
                imageBytes = Convert.FromBase64String(product.ProductImgRaw.AsString);
            else
                return NotFound();

            return File(imageBytes, "image/png");
        }






        //public async Task<IActionResult> Invoices()
        //{
        //    // Fetch all invoices first
        //    var invoices = await 
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

        //            await .UpdateOneAsync(filter, update);
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

        // Replace the existing Invoices method body with this implementation
        public async Task<IActionResult> Invoices(int page = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation($"Loading Invoices page {page} (size {pageSize})");

                var totalOrders = await _mongo.TbOrder.CountDocumentsAsync(_ => true);
                var totalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);
                page = Math.Max(1, Math.Min(page, totalPages == 0 ? 1 : totalPages));

                var orders = await _mongo.TbOrder
                    .Find(_ => true)
                    .SortByDescending(o => o.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                // Lightweight projection for totals to avoid loading full documents
                var totals = await _mongo.TbOrder
                    .Find(_ => true)
                    .Project(o => new { o.PaymentStatus, o.OrderStatus, o.TotalAmount })
                    .ToListAsync();

                var overdueAmount = totals.Where(o => o.PaymentStatus == "Overdue").Sum(o => o.TotalAmount);
                var openAmount = totals.Where(o => o.PaymentStatus == "Unpaid").Sum(o => o.TotalAmount);
                var draftedAmount = totals.Where(o => o.PaymentStatus == "Pending" || o.OrderStatus == "Processing").Sum(o => o.TotalAmount);

                var availableProducts = await _mongo.ProductVariantInventory.Find(v => v.StockQuantity > 0).ToListAsync();
                var customers = await _mongo.TbUserCollection.Find(u => u.Role.ToLower() == "customer").SortBy(u => u.FirstName).ToListAsync();

                // --- Create JSON-safe DTOs to avoid serializer errors in the view ---
                var safeOrders = orders.Select(o => new
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    PaymentStatus = o.PaymentStatus,
                    CreatedAt = o.CreatedAt,
                    ShippingAddress = o.ShippingAddress == null ? null : new
                    {
                        FirstName = o.ShippingAddress.FirstName,
                        LastName = o.ShippingAddress.LastName,
                        Email = o.ShippingAddress.Email,
                        Phone = o.ShippingAddress.Phone,
                        Street = o.ShippingAddress.Street,
                        City = o.ShippingAddress.City,
                        State = o.ShippingAddress.State,
                        Country = o.ShippingAddress.Country,
                        ZipCode = o.ShippingAddress.ZipCode,
                        FullAddress = o.ShippingAddress.FullAddress
                    },
                    Items = (o.Items ?? new List<OrderItem>()).Select(i => new
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        Price = i.Price,
                        Subtotal = i.Subtotal
                    }).ToList(),
                    Subtotal = o.Subtotal,
                    Tax = o.Tax,
                    ShippingFee = o.ShippingFee,
                    TotalAmount = o.TotalAmount
                }).ToList();


                var safeCustomers = customers.Select(c => new
                {
                    Id = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email
                }).ToList();

                var safeProducts = availableProducts.Select(p => new
                {
                    Id = p.Id,
                    VariantName = p.VariantName,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity
                }).ToList();

                // Serialize safe DTOs once on the server
                ViewBag.OrdersJson = JsonSerializer.Serialize(safeOrders);
                ViewBag.CustomersJson = JsonSerializer.Serialize(safeCustomers);
                ViewBag.ProductsJson = JsonSerializer.Serialize(safeProducts);

                var viewModel = new InvoiceListViewModel
                {
                    Orders = orders,
                    OverdueAmount = overdueAmount,
                    OpenAmount = openAmount,
                    DraftedAmount = draftedAmount,
                    AvailableProducts = availableProducts,
                    Customers = customers
                };

                ViewBag.NextInvoiceNumber = await GenerateInvoiceNumber();
                ViewBag.ActiveUsers = await _mongo.TbUserCollection.CountDocumentsAsync(u => u.IsEmailVerified == true);
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalOrders = totalOrders;
                ViewBag.PageSize = pageSize;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Invoices page");

                // Show full exception in browser only during development to debug the 500
                if (_env != null && _env.EnvironmentName == "Development")
                {
                    // return full stack trace in response for quick debugging (remove after fix)
                    return Content(ex.ToString(), "text/plain");
                }

                return View(new InvoiceListViewModel());
            }
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
                subtotal += (decimal)item.Subtotal;

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
            order.TotalAmount = (decimal)(subtotal + order.Tax + order.ShippingFee);

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
            // Use TbOrder instead of Invoices to generate order numbers
            var lastOrder = await _mongo.TbOrder
                .Find(_ => true)
                .SortByDescending(o => o.CreatedAt)
                .Limit(1)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastOrder != null && !string.IsNullOrEmpty(lastOrder.OrderNumber))
            {
                var numericPart = new string(lastOrder.OrderNumber.Where(char.IsDigit).ToArray());
                if (int.TryParse(numericPart, out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"INV-{nextNumber:D5}";
        }

        [HttpGet]
        public IActionResult GetAllProductSalesByPeriod(string period = "week")
        {
            DateTime today = DateTime.Today;
            DateTime start;

            switch (period.ToLower())
            {
                case "week":
                    start = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
                    break;

                case "year":
                    start = new DateTime(today.Year, 1, 1);
                    break;

                default:
                    start = new DateTime(today.Year, today.Month, 1);
                    break;
            }

            // get sales in the selected period
            var sales = _mongo.ProductSalesInventory
                .Find(s => s.TransactionDate >= start)
                .ToList();

            if (!sales.Any())
                return Json(new { success = false, message = "No sales found for selected period" });

            var variants = _mongo.ProductVariantInventory.Find(_ => true).ToList();

            var data = sales
                .Select(s =>
                {
                    var variant = variants.FirstOrDefault(v => v.Id == s.VariantId);

                    return new
                    {
                        VariantName = variant?.VariantName ?? "Unknown",
                        Quantity = s.Quantity,
                        Price = decimal.TryParse(s.SalePrice, out var p) ? p : 0,
                        Total = (decimal.TryParse(s.SalePrice, out var pr) ? pr : 0) * s.Quantity,
                        TransactionDate = s.TransactionDate.ToString("yyyy-MM-dd")
                    };
                })
                .OrderByDescending(x => x.TransactionDate)
                .ToList();

            return Json(new { success = true, data });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProductSalesDataPaginated(int page = 1, int pageSize = 10)
        {
            try
            {
                // 1. Fetch all paid orders
                var allOrders = await _mongo.TbOrder
                    .Find(o => o.PaymentStatus == "Paid" && !o.IsArchive)
                    .SortByDescending(o => o.CreatedAt)
                    .ToListAsync();

                // 2. Flatten order items with order date
                var allItems = allOrders
                    .SelectMany(o => o.Items.Select(i => new
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        Price = i.Price,
                        Total = i.Price * i.Quantity,
                        TransactionDate = o.CreatedAt
                    }))
                    .OrderByDescending(x => x.TransactionDate)
                    .ToList();

                if (!allItems.Any())
                {
                    return Json(new { success = false, message = "No sales data found." });
                }

                // 3. Map sales data
                var salesData = allItems.Select(item => new
                {
                    VariantName = item.ProductName ?? "Unknown Product",
                    Quantity = item.Quantity,
                    Price = item.Price,
                    Total = item.Total,
                    TransactionDate = item.TransactionDate.ToString("MMM dd, yyyy hh:mm tt")
                }).ToList();

                // 4. Calculate pagination
                var totalItems = salesData.Count;
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                // 5. Apply pagination
                var pagedData = salesData
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Json(new
                {
                    success = true,
                    data = pagedData,
                    currentPage = page,
                    totalPages = totalPages,
                    totalItems = totalItems,
                    pageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching paginated product sales data");
                return Json(new { success = false, message = "Error loading sales data." });
            }
        }


        [HttpGet]
        public IActionResult GetSalesSummaryByPeriod(string period = "week", string? startDate = null, string? endDate = null)
        {
            DateTime today = DateTime.Today;
            DateTime start;
            DateTime end = today.AddDays(1).AddTicks(-1);

            // Handle custom date range
            if (period.ToLower() == "custom" && !string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                if (DateTime.TryParse(startDate, out DateTime parsedStart) &&
                    DateTime.TryParse(endDate, out DateTime parsedEnd))
                {
                    start = parsedStart;
                    end = parsedEnd.AddDays(1).AddTicks(-1);
                }
                else
                {
                    return BadRequest(new { success = false, message = "Invalid date format" });
                }
            }
            else
            {
                switch (period.ToLower())
                {
                    case "week":
                        start = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
                        break;

                    case "year":
                        start = new DateTime(today.Year, 1, 1);
                        break;

                    case "alltime":
                        start = DateTime.MinValue;
                        end = DateTime.MaxValue;
                        break;

                    case "month":
                    default:
                        start = new DateTime(today.Year, today.Month, 1);
                        break;
                }
            }

            // Fetch paid orders from TbOrder within the date range
            var orders = _mongo.TbOrder
                   .Find(o => o.PaymentStatus == "Paid"
                       && o.CreatedAt >= start && o.CreatedAt <= end)
                   .ToList();

            // Calculate totals
            int totalOrders = orders.Count;
            decimal totalSales = orders.Sum(o => o.TotalAmount);

            return Json(new
            {
                success = true,
                totalOrders = totalOrders,
                totalSales = Math.Round(totalSales, 2),
                totalSalesFormatted = $"₱{totalSales:N2}"
            });
        }

        // Replace the existing GetProductSales method

        [HttpGet]
        public IActionResult GetProductSales(string productId, string period = "week", string? startDate = null, string? endDate = null)
        {
            if (string.IsNullOrEmpty(productId))
                return Json(new { message = "Missing product ID" });

            // Fetch paid orders that contain this product
            var paidOrders = _mongo.TbOrder
                .Find(o => o.PaymentStatus == "Paid" && !o.IsArchive)
                .ToList();

            // Flatten to order items matching the productId, keeping the order date
            var productSales = paidOrders
                .SelectMany(o => o.Items.Select(i => new { Item = i, o.CreatedAt }))
                .Where(x => x.Item.ProductId == productId)
                .ToList();

            if (!productSales.Any())
                return Json(new { message = "No sales data found" });

            DateTime today = DateTime.Today;
            DateTime start;
            DateTime end = today.AddDays(1);
            string periodLower = (period ?? "week").ToLower();

            if (periodLower == "custom" && !string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                if (DateTime.TryParse(startDate, out DateTime parsedStart) &&
                    DateTime.TryParse(endDate, out DateTime parsedEnd))
                {
                    start = parsedStart.Date;
                    end = parsedEnd.Date.AddDays(1);
                }
                else
                {
                    return Json(new { message = "Invalid date format" });
                }
            }
            else
            {
                switch (periodLower)
                {
                    case "week":
                        start = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
                        break;
                    case "month":
                        start = new DateTime(today.Year, today.Month, 1);
                        break;
                    case "year":
                        start = new DateTime(today.Year, 1, 1);
                        break;
                    case "alltime":
                        start = DateTime.MinValue;
                        end = today.AddDays(1);
                        break;
                    default:
                        start = new DateTime(today.Year, today.Month, 1);
                        break;
                }
            }

            var filtered = (periodLower == "alltime")
                ? productSales
                : productSales.Where(s => s.CreatedAt >= start && s.CreatedAt < end).ToList();

            if (!filtered.Any())
                return Json(Array.Empty<object>());

            IEnumerable<object> grouped;
            double dateSpan = periodLower == "alltime" ? 365 : (end - start).TotalDays;

            if (periodLower == "year" || periodLower == "alltime" || (periodLower == "custom" && dateSpan > 90))
            {
                grouped = filtered
                    .GroupBy(s => new { s.CreatedAt.Year, s.CreatedAt.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new
                    {
                        Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                        Total = g.Sum(x => x.Item.Quantity)
                    });
            }
            else if (periodLower == "month" || (periodLower == "custom" && dateSpan <= 90 && dateSpan > 7))
            {
                grouped = filtered
                    .GroupBy(s => s.CreatedAt.Day)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Label = g.Key.ToString(),
                        Total = g.Sum(x => x.Item.Quantity)
                    });
            }
            else
            {
                grouped = filtered
                    .GroupBy(s => s.CreatedAt.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new
                    {
                        Label = g.Key.ToString("ddd"),
                        Total = g.Sum(x => x.Item.Quantity)
                    });
            }

            return Json(grouped.ToList());
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
            return RedirectToAction("Invoices");
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
            // Use ProductSalesInventory directly instead of invoices
            var productSalesList = _mongo.ProductSalesInventory.Find(_ => true).ToList();

            // Handle empty data
            if (productSalesList == null || productSalesList.Count == 0)
            {
                ViewBag.Revenue = 0;
                ViewBag.Expense = 0;
                ViewBag.TotalTransactions = 0;
                return View();
            }

            // Calculate revenue (parse string prices)
            ViewBag.Revenue = productSalesList.Sum(x =>
            {
                if (decimal.TryParse(x.SalePrice, out var price))
                    return price * x.Quantity;
                return 0;
            });

            // Calculate expenses (parse string values)
            ViewBag.Expense = productSalesList.Sum(x =>
            {
                decimal tax = decimal.TryParse(x.SaleTax, out var t) ? t : 0;
                decimal discount = decimal.TryParse(x.SaleDiscounts, out var d) ? d : 0;
                return tax + discount;
            });

            ViewBag.TotalTransactions = productSalesList.Count;

            return View();
        }
        //Expenses in expense page
        // Note: You must include a using statement for the new Display Model
        // using Sheessential_Sales_Finance.Models; 

        public IActionResult Expenses(
          // Expense filters - all optional with defaults
          string? sortOrder = null,
          string[]? departments = null,
          string[]? status = null,
          decimal? minAmount = null,
          decimal? maxAmount = null,
          DateTime? startDate = null,
          DateTime? endDate = null,
          int expensePage = 1,
          int expensePageSize = 10,

          // Ingredient filters - all optional with defaults
          string[]? ingredientStatus = null,
          DateTime? ingredientStartDate = null,
          DateTime? ingredientEndDate = null,
          int? minQty = null,
          int? maxQty = null,
          string? supplier = null,
          int ingredientPage = 1,
          int ingredientPageSize = 10,

          // Payroll filters - all optional with defaults
          string[]? payrollStatus = null,
          DateTime? startPeriod = null,
          DateTime? endPeriod = null,
          string? payType = null,
          int? minEmployees = null,
          int? maxEmployees = null,
          decimal? minGross = null,
          decimal? maxGross = null,
          decimal? minNet = null,
          decimal? maxNet = null,
          int payrollPage = 1,
          int payrollPageSize = 10
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

                var allExpenses = _mongo.Expenses.Find(expenseFilter).ToList();

                // Sorting Expenses
                allExpenses = sortOrder switch
                {
                    "date_desc" => allExpenses.OrderByDescending(e => e.RequestedAt).ToList(),
                    "date_asc" => allExpenses.OrderBy(e => e.RequestedAt).ToList(),
                    "amount_desc" => allExpenses.OrderByDescending(e => e.Amount).ToList(),
                    "amount_asc" => allExpenses.OrderBy(e => e.Amount).ToList(),
                    _ => allExpenses.OrderByDescending(e => e.RequestedAt).ToList()
                };

                // Paginate Expenses
                var totalExpenses = allExpenses.Count;
                var totalExpensePages = (int)Math.Ceiling(totalExpenses / (double)expensePageSize);
                expensePage = Math.Max(1, Math.Min(expensePage, totalExpensePages == 0 ? 1 : totalExpensePages));

                var expenses = allExpenses
                    .Skip((expensePage - 1) * expensePageSize)
                    .Take(expensePageSize)
                    .ToList();

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

                // Lookup dictionaries for display
                var allIngredients = _mongo.Ingredients.Find(_ => true).ToList().ToDictionary(i => i.Id, i => i);
                var allSuppliers = _mongo.Suppliers.Find(_ => true).ToList().ToDictionary(s => s.Id, s => s);

                var allDisplayRequests = rawRequests.Select(r => new IngredientStockRequestDisplayModel
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

                // Paginate Ingredient Requests
                var totalIngredientRequests = allDisplayRequests.Count;
                var totalIngredientPages = (int)Math.Ceiling(totalIngredientRequests / (double)ingredientPageSize);
                ingredientPage = Math.Max(1, Math.Min(ingredientPage, totalIngredientPages == 0 ? 1 : totalIngredientPages));

                var displayRequests = allDisplayRequests
                    .Skip((ingredientPage - 1) * ingredientPageSize)
                    .Take(ingredientPageSize)
                    .ToList();

                // --------------------
                // 3. FILTER PAYROLL RUNS
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

                var allPayrollRuns = _mongo.ParyrollRuns.Find(payrollFilter).ToList();

                // Paginate Payroll Runs
                var totalPayrollRuns = allPayrollRuns.Count;
                var totalPayrollPages = (int)Math.Ceiling(totalPayrollRuns / (double)payrollPageSize);
                payrollPage = Math.Max(1, Math.Min(payrollPage, totalPayrollPages == 0 ? 1 : totalPayrollPages));

                var payrollRuns = allPayrollRuns
                    .Skip((payrollPage - 1) * payrollPageSize)
                    .Take(payrollPageSize)
                    .ToList();

                // --------------------
                // 4. FETCH BALANCE
                // --------------------
                var balance = _mongo.Balance.Find(_ => true).FirstOrDefault() ?? new Balance();

                // --------------------
                // 5. CALCULATE TOTALS (using all data, not paginated)
                // --------------------
                ViewBag.TotalExpenses = allExpenses.Sum(e => e?.Amount ?? 0);
                ViewBag.PendingTotal = allExpenses.Where(e => e.Status == "Pending").Sum(e => e.Amount);
                ViewBag.ApprovedTotal = allExpenses.Where(e => e.Status == "Approved").Sum(e => e.Amount);
                ViewBag.DeclinedTotal = allExpenses.Where(e => e.Status == "Declined").Sum(e => e.Amount);
                ViewBag.TotalStockRequestCost = allDisplayRequests.Sum(r => r.TotalCost);

                // --------------------
                // 6. PAGINATION VIEWBAGS
                // --------------------
                // Expense Pagination
                ViewBag.ExpenseCurrentPage = expensePage;
                ViewBag.ExpenseTotalPages = totalExpensePages;
                ViewBag.ExpenseTotalItems = totalExpenses;
                ViewBag.ExpensePageSize = expensePageSize;

                // Ingredient Pagination
                ViewBag.IngredientCurrentPage = ingredientPage;
                ViewBag.IngredientTotalPages = totalIngredientPages;
                ViewBag.IngredientTotalItems = totalIngredientRequests;
                ViewBag.IngredientPageSize = ingredientPageSize;

                // Payroll Pagination
                ViewBag.PayrollCurrentPage = payrollPage;
                ViewBag.PayrollTotalPages = totalPayrollPages;
                ViewBag.PayrollTotalItems = totalPayrollRuns;
                ViewBag.PayrollPageSize = payrollPageSize;

                // --------------------
                // 7. BUILD VIEW MODEL
                // --------------------
                var viewModel = new ExpensesWithBalanceViewModel
                {
                    Expenses = expenses,
                    Balance = balance,
                    StockRequests = displayRequests,
                    PayrollRuns = payrollRuns
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Expenses page with filters.");
                return View(new ExpensesWithBalanceViewModel());
            }
        }

        // Replace the existing ExpensesByDepartment method with this one
        [HttpGet]
        public IActionResult ExpensesByDepartment()
        {
            try
            {
                // Fetch all non-ingredient expenses
                var allExpenses = _mongo.Expenses
                    .Find(e => e.isIngredientsRequest == false)
                    .ToList();

                // Fetch all ingredient stock requests + lookups
                var rawRequests = _mongo.IngredientsStockRequests.Find(_ => true).ToList();
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

                // ✅ Fetch payroll snapshots
                var payrollSnapshots = _mongo.PayrollSnapshots
                    .Find(_ => true)
                    .SortByDescending(p => p.ProcessedAt)
                    .ToList();

                _logger.LogInformation("PayrollSnapshots count: {Count}", payrollSnapshots.Count);

                // Get balance
                var balance = _mongo.Balance.Find(_ => true).FirstOrDefault() ?? new Balance();

                var viewModel = new ExpensesWithBalanceViewModel
                {
                    Expenses = allExpenses,
                    Balance = balance,
                    StockRequests = displayRequests,
                    PayrollSnapshots = payrollSnapshots
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ExpensesByDepartment view.");

                // TEMP DEBUG: show full exception in browser during development
                if (_env != null && _env.EnvironmentName == "Development")
                {
                    return Content(ex.ToString(), "text/plain");
                }

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
                    .Set(e => e.Notes, DeclineReason)
                    .Set(e => e.DateApproved, DateTime.UtcNow);

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
                        var balanceUpdate = Builders<Balance>.Update
                            .Set(b => b.CurrentBalance, balance.CurrentBalance)
                            .Set(b => b.LastUpdated, DateTime.UtcNow);

                        _mongo.Balance.UpdateOne(balanceFilter, balanceUpdate);


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
                    Margins = new MarginSettings { Top = 10, Bottom = 10 }
                },
                Objects = {
                    new ObjectSettings {
                        HtmlContent = html,
                        WebSettings = { DefaultEncoding = "utf-8", LoadImages = true }
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

            var viewResult = viewEngine!.FindView(actionContext, viewName, false);
            if (viewResult.View == null)
                throw new Exception($"View '{viewName}' not found.");

            using var sw = new StringWriter();
            var viewContext = new ViewContext(
                actionContext,
                viewResult.View,
                new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model },
                new TempDataDictionary(HttpContext, tempDataProvider!),
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
            var expenses = _mongo.Expenses.Find(e => e.Status == "Approved").ToList();

            // Group by ExpenseType and sum up their total amounts
            var breakdown = expenses
                .GroupBy(e => e.ExpenseType)
                .Select(g => new { Label = g.Key, Total = g.Sum(x => x.Amount) })
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
        public async Task<IActionResult> GetChartData(string period = "year", string? startDate = null, string? endDate = null)
        {
            _logger.LogInformation($"📊 GetChartData called with period: {period}, startDate: {startDate}, endDate: {endDate}");

            DateTime utcNow = DateTime.UtcNow;
            string periodLower = period.ToLower();

            DateTime calculatedStartDate;
            DateTime calculatedEndDate = utcNow;

            if (periodLower == "custom" && !string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
            {
                if (DateTime.TryParse(startDate, out DateTime parsedStart) &&
                    DateTime.TryParse(endDate, out DateTime parsedEnd))
                {
                    calculatedStartDate = parsedStart.Date;
                    calculatedEndDate = parsedEnd.Date.AddDays(1).AddTicks(-1);
                }
                else
                {
                    return BadRequest(new { error = "Invalid date format" });
                }
            }
            else
            {
                switch (periodLower)
                {
                    case "week":
                        int daysToMonday = ((int)utcNow.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                        calculatedStartDate = utcNow.Date.AddDays(-daysToMonday);
                        break;
                    case "month":
                        calculatedStartDate = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                        break;
                    case "year":
                        calculatedStartDate = new DateTime(utcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        break;
                    case "alltime":
                        calculatedStartDate = DateTime.MinValue;
                        break;
                    default:
                        calculatedStartDate = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                        break;
                }
            }

            // Fetch orders and expenses in parallel
            var ordersTask = _mongo.TbOrder.AsQueryable()
                .Where(o => o.PaymentStatus == "Paid" && !o.IsArchive
                    && o.CreatedAt >= calculatedStartDate && o.CreatedAt <= calculatedEndDate)
                .ToListAsync();

            var expensesTask = _mongo.Expenses.AsQueryable()
                .Where(e => e.RequestedAt >= calculatedStartDate && e.RequestedAt <= calculatedEndDate && e.Status == "Approved")
                .ToListAsync();

            await Task.WhenAll(ordersTask, expensesTask);

            var relevantOrders = ordersTask.Result;
            var relevantExpenses = expensesTask.Result;

            // Totals
            decimal totalRevenue = relevantOrders.Sum(o => o.TotalAmount);
            decimal totalExpense = relevantExpenses.Sum(e => e.Amount);
            decimal netProfit = totalRevenue - totalExpense;

            // Expense breakdown (for doughnut)
            var expenseBreakdown = relevantExpenses
                .GroupBy(e => e.ExpenseType)
                .Select(g => new { Label = g.Key, Data = g.Sum(e => e.Amount) })
                .ToList();

            // Build trend data — Revenue, Expense, Profit per time bucket
            List<string> lineChartLabels;
            List<decimal> revenueData;
            List<decimal> expenseData;
            List<decimal> profitData;

            var dateSpan = (calculatedEndDate - calculatedStartDate).TotalDays;

            if (periodLower == "year" || periodLower == "alltime" || (periodLower == "custom" && dateSpan > 90))
            {
                // Group by month (Jan–Dec)
                lineChartLabels = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames.Take(12).ToList();

                revenueData = Enumerable.Range(1, 12)
                    .Select(m => relevantOrders.Where(o => o.CreatedAt.Month == m)
                        .Sum(o => o.TotalAmount))
                    .ToList();

                expenseData = Enumerable.Range(1, 12)
                    .Select(m => relevantExpenses.Where(e => e.RequestedAt.Month == m).Sum(e => e.Amount))
                    .ToList();

                profitData = revenueData.Select((r, i) => r - expenseData[i]).ToList();
            }
            else if (periodLower == "month" || (periodLower == "custom" && dateSpan <= 90 && dateSpan > 7))
            {
                // Group by day of month
                int daysInMonth = DateTime.DaysInMonth(calculatedStartDate.Year, calculatedStartDate.Month);
                lineChartLabels = Enumerable.Range(1, daysInMonth).Select(d => d.ToString()).ToList();

                revenueData = Enumerable.Range(1, daysInMonth)
                    .Select(d => relevantOrders.Where(o => o.CreatedAt.Day == d)
                        .Sum(o => o.TotalAmount))
                    .ToList();

                expenseData = Enumerable.Range(1, daysInMonth)
                    .Select(d => relevantExpenses.Where(e => e.RequestedAt.Day == d).Sum(e => e.Amount))
                    .ToList();

                profitData = revenueData.Select((r, i) => r - expenseData[i]).ToList();
            }
            else
            {
                // Group by day of week (Sun–Sat)
                lineChartLabels = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames.ToList();

                revenueData = Enumerable.Range(0, 7)
                    .Select(d => relevantOrders.Where(o => (int)o.CreatedAt.DayOfWeek == d)
                        .Sum(o => o.TotalAmount))
                    .ToList();

                expenseData = Enumerable.Range(0, 7)
                    .Select(d => relevantExpenses.Where(e => (int)e.RequestedAt.DayOfWeek == d).Sum(e => e.Amount))
                    .ToList();

                profitData = revenueData.Select((r, i) => r - expenseData[i]).ToList();
            }

            return Json(new
            {
                summary = new
                {
                    totalRevenue,
                    totalExpense,
                    netProfit
                },
                dateRange = new
                {
                    start = calculatedStartDate.ToString("yyyy-MM-dd"),
                    end = calculatedEndDate.ToString("yyyy-MM-dd")
                },
                revenueTrend = new
                {
                    labels = lineChartLabels,
                    datasets = new object[]
                    {
                new { label = "Revenue", data = revenueData },
                new { label = "Expense", data = expenseData },
                new { label = "Net Profit", data = profitData }
                    }
                },
                expenseBreakdown = new
                {
                    labels = expenseBreakdown.Select(e => e.Label).ToList(),
                    datasets = new[] { new { data = expenseBreakdown.Select(e => e.Data).ToList() } }
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
            _logger.LogInformation("Generating dynamic sales report...");

            // Fetch paid orders from TbOrder
            var paidOrders = _mongo.TbOrder
                .Find(o => o.PaymentStatus == "Paid" && !o.IsArchive)
                .ToList();

            var variants = _mongo.ProductVariantInventory.Find(_ => true).ToList();

            if (!paidOrders.Any())
                return Json(new { message = "No sales data found." });

            // Flatten order items
            // To this:
            var allItems = paidOrders
                .SelectMany(o => o.Items.Select(i => new { Item = i, o.CreatedAt }))
                .Where(x => x.Item.Quantity > 0)  // ← Filter by valid quantity instead
                .ToList();

            // Summary
            var totalSalesAmount = paidOrders.Sum(o => o.TotalAmount);
            var totalOrders = paidOrders.Count;
            var activeCustomers = paidOrders.Select(o => o.UserId).Distinct().Count();

            // Top performing product
            var topProduct = allItems
                .GroupBy(x => x.Item.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Total = g.Sum(x => x.Item.Price * x.Item.Quantity)
                })
                .OrderByDescending(x => x.Total)
                .FirstOrDefault();

            string topProductName = "Unknown";
            if (topProduct != null)
            {
                var match = variants.FirstOrDefault(v => v.Id == topProduct.ProductId);
                topProductName = match?.VariantName ?? "Unknown Product";
            }

            // Sales Trend (monthly)
            var salesTrend = paidOrders
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new ChartDataPoint
                {
                    Label = $"{new DateTime(g.Key.Year, g.Key.Month, 1):MMM yyyy}",
                    Total = (double)g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => DateTime.Parse(x.Label))
                .ToList();

            // Top Products Chart (percentage)
            var productTotals = allItems
                .GroupBy(x => x.Item.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Total = g.Sum(x => x.Item.Price * x.Item.Quantity)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            var topProductsChart = productTotals.Select(pt =>
            {
                var variant = variants.FirstOrDefault(v => v.Id == pt.ProductId);
                var name = variant?.VariantName ?? "Unknown";

                return new ProductChartPoint
                {
                    Name = name,
                    Percentage = totalSalesAmount > 0
                        ? Math.Round((double)(pt.Total / totalSalesAmount * 100), 2)
                        : 0
                };
            }).ToList();

            // Products Table
            var topProductsTable = productTotals.Select(pt =>
            {
                var variant = variants.FirstOrDefault(v => v.Id == pt.ProductId);

                return new TransactionItem
                {
                    ProductId = pt.ProductId,
                    ProductName = variant?.VariantName ?? "Unknown",
                    Category = variant?.Category ?? "N/A",
                    UnitPrice = variant?.Price ?? 0,
                    Quantity = allItems.Where(x => x.Item.ProductId == pt.ProductId).Sum(x => x.Item.Quantity),
                    TotalAmount = pt.Total
                };
            }).ToList();

            var reportData = new SalesReportDataDto
            {
                Summary = new ReportSummary
                {
                    TotalSales = totalSalesAmount,
                    TotalOrders = totalOrders,
                    ActiveCustomers = activeCustomers,
                    TopPerformingProduct = topProductName
                },
                SalesTrend = salesTrend,
                TopProductsChart = topProductsChart,
                TopProductsTable = topProductsTable
            };

            return Json(reportData);
        }

        [HttpGet]
        public IActionResult ExpenseReportPrint(string status = "all", string sortBy = "amount")
        {
            try
            {
                var allExpenses = _mongo.Expenses
                    .Find(e => e.isIngredientsRequest == false)
                    .ToList();

                var rawRequests = _mongo.IngredientsStockRequests.Find(_ => true).ToList();
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

                var payrollSnapshots = _mongo.PayrollSnapshots
                    .Find(_ => true)
                    .SortByDescending(p => p.ProcessedAt)
                    .ToList();

                var balance = _mongo.Balance.Find(_ => true).FirstOrDefault() ?? new Balance();

                var viewModel = new ExpensesWithBalanceViewModel
                {
                    Expenses = allExpenses,
                    Balance = balance,
                    StockRequests = displayRequests,
                    PayrollSnapshots = payrollSnapshots
                };

                ViewBag.FilterStatus = status;
                ViewBag.SortBy = sortBy;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ExpenseReportPrint view.");
                return Content("Error generating report.");
            }
        }

        public IActionResult ExecutivePayrollApproval()
        {
            var model = new ExpensesWithBalanceViewModel
            {
                Expenses = new List<Expenses>(),
                StockRequests = new List<IngredientStockRequestDisplayModel>(),
                PayrollSnapshots = _mongo.PayrollSnapshots
                    .Find(p => p.Department == "Finance")
                    .ToList(),
                Balance = _mongo.Balance.Find(_ => true).FirstOrDefault()
            };

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> ReleasePayroll(string id, string status)
        {
            try
            {
                _logger.LogInformation("ReleasePayroll triggered for Id: {Id}, Status: {Status}", id, status);

                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(status))
                    return Json(new { success = false, message = "Invalid request." });

                var normalizedStatus = status.Trim();
                if (normalizedStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                    normalizedStatus = "Completed";

                // 1) Find target snapshots by the same cutoff key used by the UI
                var allSnapshots = await _mongo.PayrollSnapshots.Find(_ => true).ToListAsync();
                var targetSnapshots = allSnapshots
                    .Where(s => $"{s.PayPeriodStart:yyyyMMdd}-{s.PayPeriodEnd:yyyyMMdd}" == id)
                    .ToList();

                if (!targetSnapshots.Any())
                    return Json(new { success = false, message = $"No payroll snapshots found for cutoff {id}." });

                var snapshotIds = targetSnapshots
                    .Where(s => !string.IsNullOrWhiteSpace(s.Id))
                    .Select(s => s.Id!)
                    .ToList();

                var grossAmount = targetSnapshots.Sum(x => x.GrossPay);

                // 2) Complete flow = deduct + expense
                if (normalizedStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    var balance = _mongo.Balance.Find(_ => true).FirstOrDefault();
                    if (balance != null)
                    {
                        if (balance.CurrentBalance < grossAmount)
                            return Json(new { success = false, message = "Insufficient balance." });

                        var newBalance = balance.CurrentBalance - grossAmount;

                        await _mongo.Balance.UpdateOneAsync(
                            Builders<Balance>.Filter.Eq(b => b.Id, balance.Id),
                            Builders<Balance>.Update
                                .Set(b => b.CurrentBalance, newBalance)
                                .Set(b => b.LastUpdated, DateTime.UtcNow));
                    }

                    _mongo.Expenses.InsertOne(new Expenses
                    {
                        ExpenseId = $"EXP-{DateTime.UtcNow.Ticks}",
                        Department = "Finance",
                        ExpenseType = "Salary",
                        Description = $"Salary payout for payroll cutoff {id}",
                        Amount = grossAmount,
                        RequestedBy = "System",
                        Status = "Approved",
                        RequestedAt = DateTime.UtcNow,
                        DateApproved = DateTime.UtcNow,
                        Notes = "Automatically generated after payroll release.",
                        AttachmentUrl = "",
                        isIngredientsRequest = false,
                        Version = 0
                    });
                }

                // 3) Update matched snapshots by Id
                await _mongo.PayrollSnapshots.UpdateManyAsync(
                    Builders<PayrollSnapshot>.Filter.In(x => x.Id, snapshotIds),
                    Builders<PayrollSnapshot>.Update
                        .Set(x => x.Status, normalizedStatus)
                        .Set(x => x.ProcessedAt, DateTime.UtcNow));

                return Json(new { success = true, message = $"Payroll marked as {normalizedStatus}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ReleasePayroll failed. Id: {Id}, Status: {Status}", id, status);
                return Json(new { success = false, message = ex.Message });
            }
        }

    }
}
