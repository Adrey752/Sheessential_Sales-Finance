using Microsoft.AspNetCore.Mvc;

namespace Sheessential_Sales_Finance.Controllers
{
    public class Sales_FinanceController : Controller
    {
        public IActionResult Index()
        {
            var userName = HttpContext.Session.GetString("UserName");
            var userRole = HttpContext.Session.GetString("UserRole");

            ViewBag.UserName = userName;
            ViewBag.UserRole = userRole;
            return View();
        }
    }
}
