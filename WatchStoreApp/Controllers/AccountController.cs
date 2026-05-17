using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchStoreApp.Data;
using WatchStoreApp.Utils;
using WatchStoreApp.ViewModel.Account;

namespace WatchStoreApp.Controllers
{
    [Authorize(Roles = "Admin, Employee")]
    public class AccountController(MyAppContext context) : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Signin");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity.Name;
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            Console.WriteLine($"User ID: {userId}, User Name: {userName}, User Role: {userRole}");

            var employee = context.Employees.Find(int.Parse(userId!));
            if (employee == null)
            {
                Console.WriteLine("Employee not found.");
                return NotFound();
            }
            
            var model = new UpdateAccountVM
            {
                EmployeeId = employee.EmployeeId,
                Name = employee.Name,
                CardNumber = employee.CardNumber,
                PhoneNumber = employee.PhoneNumber,
                Role = employee.Role,
                Email = employee.Email,
                Password = employee.Password
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(UpdateAccountVM model)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model state is invalid.");
                return View(model);
            }

            var account = await context.Employees.FindAsync(model.EmployeeId);
            if (account == null)
            {
                Console.WriteLine("Account not found.");
                return NotFound();
            }

            account.Name = model.Name;
            account.CardNumber = model.CardNumber;
            account.PhoneNumber = model.PhoneNumber;
            account.Role = model.Role;
            account.Email = model.Email;
            if (!string.IsNullOrEmpty(model.Password))
            {
                var hashedPassword = PasswordHelper.IsBcryptHash(model.Password) 
                    ? model.Password 
                    : PasswordHelper.HashPassword(model.Password);
    
                account.Password = hashedPassword;
            }

            context.Employees.Update(account);
            await context.SaveChangesAsync();

            return RedirectToAction("Index", "Account");
        }
    }
}
