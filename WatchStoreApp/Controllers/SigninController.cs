using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using WatchStoreApp.Data;
using WatchStoreApp.Utils;

namespace WatchStoreApp.Controllers
{
    public class SigninController : Controller
    {
        private readonly MyAppContext _context;
        public SigninController(MyAppContext context)
        {
            _context = context;
        }

        private static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return "";
            var atIndex = email.IndexOf('@');
            if (atIndex <= 1) return "***";
            var local = email[..atIndex];
            var domain = email[atIndex..];
            var maskedLocal = local.Length <= 2
                ? local[0] + "***"
                : local[0] + new string('*', Math.Min(6, local.Length - 2)) + local[^1];
            return maskedLocal + domain;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string email, string password)
        {
            Log.Information("Login attempt. Email={Email}", MaskEmail(email));
            var customer = _context.Customers
                .FirstOrDefault(c => c.Email == email);

            if (customer != null)
            {
                if (!PasswordHelper.VerifyPassword(password, customer.Password))
                {
                    ViewBag.Error = "Invalid email or password.";
                    Log.Warning("Login failed (invalid password). CustomerEmail={Email}", MaskEmail(email));
                    return View();
                }
                
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, customer.Name),
                    new Claim(ClaimTypes.NameIdentifier, customer.CustomerId.ToString()),
                    new Claim(ClaimTypes.Role, "Customer")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                Log.Information("Customer login succeeded. CustomerId={CustomerId}", customer.CustomerId);
                return RedirectToAction("Index", "Home");
            }
            else 
            {
                var employee = _context.Employees
                    .FirstOrDefault(e => e.Email == email);
                
                if (employee == null || !PasswordHelper.VerifyPassword(password, employee.Password))
                {
                    ViewBag.Error = "Invalid email or password.";
                    Log.Warning("Login failed (employee not found or invalid password). Email={Email}", MaskEmail(email));
                    return View();
                }

                var empClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, employee.Name),
                    new Claim(ClaimTypes.NameIdentifier, employee.EmployeeId.ToString()),
                    new Claim(ClaimTypes.Role, employee.Role)
                };

                var empIdentity = new ClaimsIdentity(empClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                var empPrincipal = new ClaimsPrincipal(empIdentity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, empPrincipal);

                Log.Information("Employee login succeeded. EmployeeId={EmployeeId} Role={Role}", employee.EmployeeId, employee.Role);
                return RedirectToAction("Index", "Dashboard");
            }
        }

        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role);
            Log.Information("User logout. UserId={UserId} Role={Role}", userId, role);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }

        public IActionResult AccessDenied()
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Guest";
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Log.Warning("Access denied. UserId={UserId} Role={Role}", userId, userRole);
            ViewBag.UserRole = userRole;
            return View();
        }
    }
}
