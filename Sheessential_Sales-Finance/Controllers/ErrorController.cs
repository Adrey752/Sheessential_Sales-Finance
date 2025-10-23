using Microsoft.AspNetCore.Mvc;

namespace Sheessential_Sales_Finance.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/Connection")]
        public IActionResult ConnectionError()
        {
            ViewBag.Message = "Unable to connect to the server. Please check your internet connection and try again.";
            return View("Connection");
        }
    }
}
