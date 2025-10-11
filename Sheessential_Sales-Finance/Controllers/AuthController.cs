using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Sheessential_Sales_Finance.helpers;
using Sheessential_Sales_Finance.Models;
using System.Security.Cryptography;
using System.Text;

namespace Sheessential_Sales_Finance.Controllers
{
    public class AuthController : Controller
    {
        private readonly MongoHelper _mongo;
        private readonly ILogger<AuthController> _logger;


        public AuthController(MongoHelper mongo, ILogger<AuthController> logger)
        {
            _mongo = mongo;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            _logger.LogInformation("\n\n\n\nI'm here hashahahsdfhasdf\n\n\n\n\n\n");
            if (HttpContext.Session.GetString("UserId") != null)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            _logger.LogInformation("\n\n\n\nI'm here hashahahsdfhasdf\n\n\n\n\n\n");
            return View();
        }

        [HttpPost("Auth/Login")]
        public async Task<IActionResult> Login(string Email, string Password)
        {
            _logger.LogInformation("\n\n\n\n\n\n\n We're Logging in!\n\n\n\n\n\n\n\n");

            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ViewBag.Error = "Please fill all fields.";
                return View();
            }

            var user = await _mongo.Users.Find(u => u.Email == Email).FirstOrDefaultAsync();

            if (user == null)
            {
                ViewBag.Error = "Invalid e-mail or password.";
                return View();
            }

            var passwordHash = ComputeSha256Hash(Password);

            if (user.Password != passwordHash)
            {
                ViewBag.Error = "Invalid e-mail or password.";
                return View();
            }

            HttpContext.Session.SetString("UserId", user.Id ?? "");
            HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
            HttpContext.Session.SetString("UserRole", user.Role);

            user.LastLogin = DateTime.Now;
            await _mongo.Users.ReplaceOneAsync(u => u.Id == user.Id, user);

            return RedirectToAction("Index", "Sales_Finance");
        }


        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user, string password)
        {
 
            user.Role ??= "User";
            user.Status ??= "Active";
            user.CreatedAt = DateTime.Now;
            user.LastLogin = DateTime.Now;

            if (!ModelState.IsValid)
            {
                return View(user);
            }
            // check if email already exst
            var existingUser = _mongo.Users.Find(User => User.Email == user.Email).FirstOrDefault();
            if (existingUser != null)
            {
                ViewBag.Error = "Email already registered.";
                return View(user);
            }

            user.Password = ComputeSha256Hash(password);

            _mongo.Users.InsertOne(user);

            ViewBag.Success = "Registration successful!";
            return RedirectToAction("Login", "Auth");
        }

        // helper function for password hashing
        private static string ComputeSha256Hash(string rawData)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }
    }
}
