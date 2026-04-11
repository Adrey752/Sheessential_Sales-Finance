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

            var employee = await _mongo.HrEmployees
                .Find(Builders<BsonDocument>.Filter.Eq("email", Email.Trim()))
                .FirstOrDefaultAsync();

            if (employee == null)
            {
                ViewBag.Error = "Invalid e-mail or employee ID.";
                return View();
            }

            var department = employee.GetValue("department", "").ToString();
            if (!department.Equals("Finance", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error = "Only Finance department users can log in.";
                return View();
            }

            var employeeId = employee.GetValue("employeeId", "").ToString();
            if (!employeeId.Equals(Password.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Error = "Invalid e-mail or employee ID.";
                return View();
            }

            if (employee.TryGetValue("isActive", out var isActiveValue) && isActiveValue.IsBoolean && !isActiveValue.AsBoolean)
            {
                ViewBag.Error = "Your account is inactive.";
                return View();
            }

            var role = employee.GetValue("role", "").ToString();
            var firstName = employee.GetValue("firstName", "").ToString();
            var lastName = employee.GetValue("lastName", "").ToString();
            var fullName = $"{firstName} {lastName}".Trim();
            var employeeMongoId = employee.GetValue("_id", "").ToString();

            HttpContext.Session.SetString("UserId", employeeMongoId);
            HttpContext.Session.SetString("UserName", string.IsNullOrWhiteSpace(fullName) ? "Finance User" : fullName);
            HttpContext.Session.SetString("UserRole", role);
            HttpContext.Session.SetString("UserDepartment", department);
            HttpContext.Session.SetString("Email", Email.Trim());

            if (role.Equals("Finance manager", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("ExecutivePayrollApproval", "Sales_Finance");
            }

            return RedirectToAction("Dashboard", "Sales_Finance");
        }


        [HttpPost]
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
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _mongo.Users.Find(u => u.Email == email).FirstOrDefaultAsync();

            if (user == null)
            {
                ViewBag.Error = "Email not found.";
                return View();
            }

            // ✅ generate reset token
            user.ResetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            user.ResetTokenExpiry = DateTime.Now.AddMinutes(15);

            await _mongo.Users.ReplaceOneAsync(u => u.Id == user.Id, user);

            string resetLink = Url.Action("ResetPassword", "Auth",
                                           new { token = user.ResetToken },
                                           Request.Scheme);

            // send email
            EmailSender.Send(user.Email,
                "Password Reset Request",
                $"<p>Click to reset password:</p><a href='{resetLink}'>Reset Password</a>"
            );

            ViewBag.Success = "Password reset link sent to your email.";
            return View();
        }
        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
                return BadRequest("Invalid token.");

            return View(model: token);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string token, string newPassword)
        {
            var user = await _mongo.Users
                .Find(u => u.ResetToken == token && u.ResetTokenExpiry >= DateTime.Now)
                .FirstOrDefaultAsync();

            if (user == null)
                return BadRequest("Invalid or expired token.");

            user.Password = ComputeSha256Hash(newPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;

            await _mongo.Users.ReplaceOneAsync(u => u.Id == user.Id, user);

            TempData["Success"] = "Password reset successful!";
            return RedirectToAction("Login");
        }

    }
}
