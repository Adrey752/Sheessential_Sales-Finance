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
