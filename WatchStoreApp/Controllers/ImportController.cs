using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStoreApp.Data;
using WatchStoreApp.Models;
using WatchStoreApp.ViewModel.Import;

namespace WatchStoreApp.Controllers
{
    [Authorize(Roles = "Admin, Employee")]
    public class ImportController : Controller
    {
        private readonly MyAppContext _context;
        public ImportController(MyAppContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var bills = _context.ImportBills
                .Include(b => b.Employee)
                .Select(b => new ViewListImportBillVM
                {
                    ImportBillId = b.ImportBillId,
                    EmployeeId = b.EmployeeId,
                    EmployeeName = b.Employee.Name,
                    Date = b.Date,
                    Total = b.Total
                })
                .ToList();
            return View(bills);
        }

        public IActionResult Create()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Signin");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity.Name;
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            Console.WriteLine($"User ID: {userId}, User Name: {userName}, User Role: {userRole}");

            var vm = new CreateBillVM
            {
                EmployeeName = userName ?? string.Empty,
                EmployeeId = int.Parse(userId!),
                Date = DateTime.Now,
                Products = _context.Products.ToList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CreateBillVM model)
        {
            if (model.ImportDetails == null || !model.ImportDetails.Any())
            {
                ModelState.AddModelError(string.Empty, "Import details cannot be empty.");
            }

            if (ModelState.IsValid)
            {
                model.Total = model.ImportDetails.Sum(d => d.Price * d.Quantity);
                var importBill = new ImportBill
                {
                    EmployeeId = model.EmployeeId,
                    Date = model.Date,
                    Total = model.Total,
                    ImportDetails = model.ImportDetails.Select(d => new ImportDetail
                    {
                        ProductId = d.ProductId,
                        Quantity = d.Quantity,
                        Price = d.Price
                    }).ToList()
                };
                _context.ImportBills.Add(importBill);

                foreach (var detail in model.ImportDetails)
                {
                    var product = _context.Products.FirstOrDefault(p => p.ProductId == detail.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += detail.Quantity;
                        _context.Products.Update(product);
                    }
                }

                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            model.Products = _context.Products.ToList();
            return View(model);
        }

        public IActionResult Details (int id)
        {
            var importBill = _context.ImportBills
                .Include(b => b.Employee)
                .Include(b => b.ImportDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefault(b => b.ImportBillId == id);
            if (importBill == null)
            {
                return NotFound();
            }
            return View(importBill);
        }

        public IActionResult Index1()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Signin");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity.Name;
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            Console.WriteLine($"User ID: {userId}, User Name: {userName}, User Role: {userRole}");

            return View();
        }
    }
}
