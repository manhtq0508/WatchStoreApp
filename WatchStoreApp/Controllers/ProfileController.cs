using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WatchStoreApp.Data;
using WatchStoreApp.ViewModel.Profile;

namespace WatchStoreApp.Controllers
{
    public class ProfileController : Controller
    {
        private readonly MyAppContext _context;
        public ProfileController(MyAppContext context)
        {
            _context = context;
        }

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

            ViewBag.Gender = new List<SelectListItem>
            {
                new SelectListItem { Value = "Male", Text = "Male" },
                new SelectListItem { Value = "Female", Text = "Female" }
            };

            var customer = _context.Customers.Find(int.Parse(userId));
            if (customer == null)
            {
                Console.WriteLine("Customer not found.");
                return NotFound();
            }

            var model = new UpdateProfileVM
            {
                CustomerId = customer.CustomerId,
                Name = customer.Name,
                DateOfBirth = customer.DateOfBirth,
                Gender = customer.Gender,
                PhoneNumber = customer.PhoneNumber,
                Email = customer.Email,
                Password = customer.Password
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(UpdateProfileVM model)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model state is invalid.");
                return View(model);
            }

            var account = await _context.Customers.FindAsync(model.CustomerId);
            if (account == null)
            {
                Console.WriteLine("Account not found.");
                return NotFound();
            }

            account.Name = model.Name;
            account.DateOfBirth = model.DateOfBirth;
            account.PhoneNumber = model.PhoneNumber;
            account.Gender = model.Gender;
            account.Email = model.Email;
            if (!string.IsNullOrEmpty(model.Password))
            {
                account.Password = model.Password;
            }

            _context.Customers.Update(account);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Profile");
        }
    }
}
