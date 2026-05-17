using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStoreApp.Data;
using WatchStoreApp.Models;

namespace WatchStoreApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MyAppContext _context;

        public HomeController(ILogger<HomeController> logger, MyAppContext context)
        {
            _logger = logger;
            _context = context;
        }


        public IActionResult Index()
        {
            ViewBag.Brands = _context.Brands.ToList();
            ViewBag.Genders = new List<string> { "Male", "Female", "Other" };
            ViewBag.Styles = new List<string> { "Classic", "Modern", "Sport", "Luxury" };

            // Get top-selling products based on actual sales from invoices
            var topSellingProductIds = _context.InvoiceDetails
                .Include(d => d.Invoice)
                .Where(d => d.Invoice.Status != "Cancelled")
                .GroupBy(d => d.ProductId)
                .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(d => d.Quantity) })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(8)
                .Select(x => x.ProductId)
                .ToList();

            var topSellingProductsRaw = _context.Products
                .Where(p => topSellingProductIds.Contains(p.ProductId) && p.Flag == 1)
                .ToList();

            var topSellingProducts = topSellingProductsRaw
                .OrderBy(p => topSellingProductIds.IndexOf(p.ProductId))
                .ToList();

            if (topSellingProducts.Count < 8)
            {
                var additionalProducts = _context.Products
                    .Where(p => !topSellingProductIds.Contains(p.ProductId) && p.Flag == 1)
                    .OrderByDescending(p => p.ProductId)
                    .Take(8 - topSellingProducts.Count)
                    .ToList();
                topSellingProducts.AddRange(additionalProducts);
            }

            ViewBag.TopSellingProducts = topSellingProducts;

            // Get newest products
            ViewBag.NewProducts = _context.Products
                .Where(p => p.Flag == 1) // Only active products
                .OrderByDescending(p => p.ProductId)
                .Take(4)
                .ToList();

            // Get mechanical watches
            ViewBag.MechanicalWatches = _context.Products
                .Where(p => p.WatchType == "Mechanical" && p.Flag == 1)
                .OrderByDescending(p => p.ProductId)
                .Take(8)
                .ToList();

            // Get smartwatches
            ViewBag.Smartwatches = _context.Products
                .Where(p => p.WatchType == "Smartwatch" && p.Flag == 1)
                .OrderByDescending(p => p.ProductId)
                .Take(8)
                .ToList();

            var settings = _context.Settings.FirstOrDefault();
            ViewBag.Settings = settings;

            return View();
        }

        public IActionResult Brands()
        {
            var brands = _context.Brands
                .Where(b => b.Flag == 1)
                .ToList();
            return View(brands);
        }

        public IActionResult Brand(int id)
        {
            ViewBag.Brand = _context.Brands.Find(id);
            var products = _context.Brands
                .Where(b => b.BrandId == id)
                .SelectMany(b => b.Products)
                .Where(p => p.Flag == 1)
                .ToList();
            return View(products);
        }

        public IActionResult Gender(string gender)
        {
            if (string.IsNullOrEmpty(gender))
            {
                return RedirectToAction("Index");
            }

            var products = _context.Products
                .Where(p => p.Gender != null && p.Gender.ToLower() == gender.ToLower())
                .ToList();


            ViewBag.Gender = gender;
            return View(products);
        }

        public IActionResult Style(string style)
        {
            if (string.IsNullOrEmpty(style))
            {
                return RedirectToAction("Index");
            }
            var products = _context.Products
                .Where(p => p.WatchStyle != null && p.WatchStyle.ToLower() == style.ToLower())
                .ToList();
            ViewBag.Type = style;
            return View(products);
        }

        public IActionResult Mechanical(int? id)
        {
            // If id is provided, show individual product detail
            if (id.HasValue)
            {
                var product = _context.Products
                    .Include(p => p.Brand)   
                    .Include(p => p.MechanicalWatch)      
                    .FirstOrDefault(p => p.ProductId == id.Value && p.WatchType == "Mechanical");

                if (product == null)
                {
                    return NotFound();
                }
                return View(product);
            }

            // Otherwise, show list of all mechanical watches
            var products = _context.Products
                .Where(p => p.WatchType == "Mechanical")
                .ToList();
            return View("MechanicalList", products);
        }

        public IActionResult Smartwatch(int? id)
        {
            // If id is provided, show individual product detail
            if (id.HasValue)
            {
                var product = _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.SmartWatch)
                    .FirstOrDefault(p => p.ProductId == id.Value && p.WatchType == "Smartwatch");

                if (product == null)
                {
                    return NotFound();
                }
                return View(product);
            }

            var products = _context.Products
                .Where(p => p.WatchType == "Smartwatch")
                .ToList();
            return View("SmartwatchList", products);
        }

        public IActionResult Searching(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction("Index");
            }

            var products = _context.Products
                .Where(p => p.Flag == 1 &&
                            (p.Name.Contains(query) ||
                             p.Brand.BrandName.Contains(query))) 
                .ToList();

            ViewBag.Query = query;

            return View(products);
        }

        public IActionResult Filter(string price, string gender, string style, int brandId)
        {
            var products = _context.Products
                .Where(p => p.Flag == 1 && p.BrandId == brandId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(gender))
            {
                products = products.Where(p => p.Gender != null && p.Gender.ToLower() == gender.ToLower());
            }

            if (!string.IsNullOrEmpty(style))
            {
                products = products.Where(p => p.WatchStyle != null && p.WatchStyle.ToLower() == style.ToLower());
            }

            if (!string.IsNullOrEmpty(price))
            {
                switch (price)
                {
                    case "low-high":
                        products = products.OrderBy(p => p.SellingPrice);
                        break;
                    case "high-low":
                        products = products.OrderByDescending(p => p.SellingPrice);
                        break;
                    case "under-5m":
                        products = products.Where(p => p.SellingPrice < 5_000_000);
                        break;
                    case "5m-10m":
                        products = products.Where(p => p.SellingPrice >= 5_000_000 && p.SellingPrice <= 10_000_000);
                        break;
                    case "over-10m":
                        products = products.Where(p => p.SellingPrice > 10_000_000);
                        break;
                }
            }

            ViewBag.Brand = _context.Brands.Find(brandId);
            return View("Brand", products.ToList());
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
