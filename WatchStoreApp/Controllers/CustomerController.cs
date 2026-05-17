using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WatchStoreApp.Data;
using WatchStoreApp.Models;
using WatchStoreApp.Utils;
using WatchStoreApp.ViewModel.Customer;

namespace WatchStoreApp.Controllers
{
    [Authorize(Roles = "Admin, Employee")]
    public class CustomerController : Controller
    {
        private readonly MyAppContext _context;
        public CustomerController(MyAppContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var customers = _context.Customers.ToList();
            return View(customers);
        }

        public IActionResult Search(string search_customer)
        {
            if (string.IsNullOrEmpty(search_customer))
            {
                var allCustomers = _context.Customers.ToList();
                return View("Index", allCustomers);
            }
            var result = _context.Customers
                .Where(c => c.Name.ToLower().Contains(search_customer.ToLower()) || c.PhoneNumber.ToLower().Contains(search_customer.ToLower()))
                .ToList();
            return View("Index", result);
        }

        public IActionResult Create()
        {
            var genders = new List<SelectListItem>
            {
                new SelectListItem { Value = "Male", Text = "Male" },
                new SelectListItem { Value = "Female", Text = "Female" },
                new SelectListItem { Value = "Other", Text = "Other" }
            };

            var isAvailableList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Available", Text = "Available" },
                new SelectListItem { Value = "Not Available", Text = "Not Available" }
            };

            var vm = new CreateCustomerVM
            {
                GenderList = genders,
                IsAvailableList = isAvailableList
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult Create(CreateCustomerVM model)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model state is not valid");

                var genders = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Male", Text = "Male" },
                    new SelectListItem { Value = "Female", Text = "Female" },
                    new SelectListItem { Value = "Other", Text = "Other" }
                };

                var isAvailableList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Available", Text = "Available" },
                    new SelectListItem { Value = "Not Available", Text = "Not Available" }
                };

                var vm = new CreateCustomerVM
                {
                    GenderList = genders,
                    IsAvailableList = isAvailableList
                };

                return View(vm);
            }

            var customer = new Customer
            {
                Name = model.Name,
                DateOfBirth = model.DateOfBirth,
                PhoneNumber = model.PhoneNumber,
                Gender = model.Gender,
                Email = model.Email,
                Password = PasswordHelper.HashPassword(model.Password),
                IsAvailable = model.IsAvailable
            };
            _context.Customers.Add(customer);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null)
            {
                return NotFound();
            }

            var genders = new List<SelectListItem>
            {
                new SelectListItem { Value = "Male", Text = "Male" },
                new SelectListItem { Value = "Female", Text = "Female" },
                new SelectListItem { Value = "Other", Text = "Other" }
            };

            var isAvailableList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Available", Text = "Available" },
                new SelectListItem { Value = "Not Available", Text = "Not Available" }
            };

            var vm = new EditCustomerVM
            {
                CustomerId = customer.CustomerId,
                Name = customer.Name,
                DateOfBirth = customer.DateOfBirth,
                PhoneNumber = customer.PhoneNumber,
                Gender = customer.Gender,
                IsAvailable = customer.IsAvailable,
                Email = customer.Email,
                GenderList = genders,
                IsAvailableList = isAvailableList
            };

            return View(vm);
        }

        [HttpPost]
        public IActionResult Edit(EditCustomerVM model)
        {
            Console.WriteLine("Editing customer with ID: " + model.CustomerId);
            if (!ModelState.IsValid)
            {
                var customer = _context.Customers.Find(model.CustomerId);
                if (customer == null)
                {
                    return NotFound();
                }

                var genders = new List<SelectListItem>
            {
                new SelectListItem { Value = "Male", Text = "Male" },
                new SelectListItem { Value = "Female", Text = "Female" },
                new SelectListItem { Value = "Other", Text = "Other" }
            };

                var isAvailableList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Available", Text = "Available" },
                new SelectListItem { Value = "Not Available", Text = "Not Available" }
            };

                var vm = new EditCustomerVM
                {
                    CustomerId = customer.CustomerId,
                    Name = customer.Name,
                    DateOfBirth = customer.DateOfBirth,
                    PhoneNumber = customer.PhoneNumber,
                    Gender = customer.Gender,
                    IsAvailable = customer.IsAvailable,
                    Email = customer.Email,
                    GenderList = genders,
                    IsAvailableList = isAvailableList
                };

                Console.WriteLine("Model state is not valid");

                return View(vm);
            }

            var existingCustomer = _context.Customers.Find(model.CustomerId);
            if (existingCustomer == null)
            {
                return NotFound();
            }

            existingCustomer.Name = model.Name;
            existingCustomer.DateOfBirth = model.DateOfBirth;
            existingCustomer.PhoneNumber = model.PhoneNumber;
            existingCustomer.Gender = model.Gender;
            existingCustomer.IsAvailable = model.IsAvailable;
            existingCustomer.Email = model.Email;
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var hashedPassword = PasswordHelper.IsBcryptHash(model.Password) 
                    ? model.Password 
                    : PasswordHelper.HashPassword(model.Password);
                
                existingCustomer.Password = hashedPassword;
            }
            _context.SaveChanges();
            return RedirectToAction("Index");

        }

        [HttpGet] 
        public IActionResult Delete(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null)
            {
                return NotFound();
            }

            try
            {
                _context.Customers.Remove(customer);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                var customerToDisable = _context.Customers.Find(id);
                if (customerToDisable != null)
                {
                    customerToDisable.IsAvailable = "Not Available";
                    _context.SaveChanges();
                }

                Console.WriteLine("Could not hard delete customer. Switched to Soft Delete. Error: " + ex.Message);
            }

            return RedirectToAction("Index");
        }
    }
}
