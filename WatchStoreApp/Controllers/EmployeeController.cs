using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchStoreApp.Data;
using WatchStoreApp.Models;
using WatchStoreApp.ViewModel.Employee;

namespace WatchStoreApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        private readonly MyAppContext _context;
        public EmployeeController(MyAppContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var employees = _context.Employees.ToList();
            return View(employees);
        }

        public IActionResult Search(string search_employee)
        {
            if (string.IsNullOrEmpty(search_employee))
            {
                var allCoupons = _context.Employees.ToList();
                return View("Index", allCoupons);
            }
            var result = _context.Employees
                .Where(c => c.Name.ToLower().Contains(search_employee.ToLower()) || c.PhoneNumber.ToLower().Contains(search_employee.ToLower()))
                .ToList();
            return View("Index", result);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateEmployeeVM model)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model state is not valid");
                return View(model);
            }
            var employee = new Employee
            {
                Name = model.Name,
                CardNumber = model.CardNumber,
                PhoneNumber = model.PhoneNumber,
                Role = model.Role,
                IsAvailable = "Available",
                Email = model.Email,
                Password = model.Password
            };
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            var employee = _context.Employees.Find(id);
            if (employee == null)
            {
                return NotFound();
            }
            return View(employee);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(int id, EditEmployeeVM model)
        {
            if (id != model.EmployeeId)
            {
                Console.WriteLine("ID mismatch");
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model state is not valid");
                return View(model);
            }
            var employee = _context.Employees.Find(id);
            if (employee == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(model.Password))
                employee.Password = model.Password;
            employee.Name = model.Name;
            employee.CardNumber = model.CardNumber;
            employee.PhoneNumber = model.PhoneNumber;
            employee.Role = model.Role;
            employee.IsAvailable = model.IsAvailable;
            employee.Email = model.Email;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var employee = _context.Employees.Find(id);
            if (employee == null)
            {
                return NotFound();
            }

            try
            {
                _context.Employees.Remove(employee);
                _context.SaveChanges();
            }
            catch (Exception)
            {
                var employeeToDisable = _context.Employees.Find(id);

                if (employeeToDisable != null)
                {
                    employeeToDisable.IsAvailable = "Not Available";
                    _context.SaveChanges();
                }
            }

            return RedirectToAction("Index");
        }
    }
}
