using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStoreApp.Data;

namespace WatchStoreApp.Controllers
{
    [Authorize(Roles = "Admin, Employee")]
    public class DashboardController : Controller
    {
        private readonly MyAppContext _context;
        public DashboardController(MyAppContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var productCount = _context.Products.Count();
            var customerCount = _context.Customers.Count();
            var employeeCount = _context.Employees.Count();

            var maleCustomerCount = _context.Customers.Count(c => c.Gender == "Male");
            var femaleCustomerCount = _context.Customers.Count(c => c.Gender == "Female");
            var otherCustomerCount = _context.Customers.Count(c => c.Gender == "Other");

            ViewBag.ProductCount = productCount;
            ViewBag.CustomerCount = customerCount;
            ViewBag.EmployeeCount = employeeCount;
            ViewBag.MaleCustomerCount = maleCustomerCount;
            ViewBag.FemaleCustomerCount = femaleCustomerCount;
            ViewBag.OtherCustomerCount = otherCustomerCount;

            // Calculate monthly revenue for the current year
            var currentYear = DateTime.Now.Year;
            var monthlyRevenue = _context.Invoices
                .Where(i => i.Date.Year == currentYear && i.Status != "Cancelled")
                .GroupBy(i => i.Date.Month)
                .Select(g => new { Month = g.Key, Total = g.Sum(i => i.Total) })
                .ToList();

            // Initialize array with 12 months (0 for months with no revenue)
            var revenueData = new decimal[12];
            foreach (var month in monthlyRevenue)
            {
                revenueData[month.Month - 1] = month.Total;
            }

            ViewBag.MonthlyRevenue = revenueData;

            // Get top 5 best-selling products
            var bestSellers = _context.InvoiceDetails
                .Include(d => d.Product)
                .Include(d => d.Invoice)
                .Where(d => d.Invoice.Status != "Cancelled")
                .GroupBy(d => new { d.ProductId, d.Product.Name })
                .Select(g => new
                {
                    productName = g.Key.Name,
                    totalQuantity = g.Sum(d => d.Quantity)
                })
                .OrderByDescending(x => x.totalQuantity)
                .Take(5)
                .ToList();

            ViewBag.BestSellers = bestSellers;

            return View();
        }
    }
}
