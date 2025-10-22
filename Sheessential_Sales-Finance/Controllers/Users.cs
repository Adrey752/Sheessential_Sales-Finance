using Microsoft.AspNetCore.Mvc;

namespace Sheessential_Sales_Finance.Controllers
{
    public class Users : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Customer()
        {
            return View();
        }
    }
}
