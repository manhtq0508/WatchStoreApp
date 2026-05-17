using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
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
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string email, string password)
        {
            var customer = _context.Customers
                .FirstOrDefault(c => c.Email == email);

            if (customer != null)
            {
                if (!PasswordHelper.VerifyPassword(password, customer.Password))
                {
                    ViewBag.Error = "Invalid email or password.";
                    Console.WriteLine("Failed login attempt for email: " + email);
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
                return RedirectToAction("Index", "Home");
            }
            else 
            {
                var employee = _context.Employees
                    .FirstOrDefault(e => e.Email == email);
                
                if (employee == null || !PasswordHelper.VerifyPassword(password, employee.Password))
                {
                    ViewBag.Error = "Invalid email or password.";
                    Console.WriteLine("Failed login attempt for email: " + email);
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

                return RedirectToAction("Index", "Dashboard");
            }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }

        public IActionResult AccessDenied()
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "Guest";
            ViewBag.UserRole = userRole;
            return View();
        }
    }
}
