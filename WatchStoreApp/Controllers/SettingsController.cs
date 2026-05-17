using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchStoreApp.Data;
using WatchStoreApp.Models;

namespace WatchStoreApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly MyAppContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment; 

        public SettingsController(MyAppContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var setting = _context.Settings.FirstOrDefault();

            if (setting == null)
            {
                setting = new Setting
                {
                    // Default values if database is empty
                    ShippingFee = 50000,
                    Banner1_1 = "",
                    Banner1_2 = "",
                    Banner1_3 = "",
                    Banner2 = "",
                    Banner3 = "",
                    Banner4 = ""
                };
                _context.Settings.Add(setting);
                _context.SaveChanges();
            }

            return View(setting);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            Setting model,
            string section,
            IFormFile? Banner1_1,
            IFormFile? Banner1_2,
            IFormFile? Banner1_3,
            IFormFile? Banner2,
            IFormFile? Banner3,
            IFormFile? Banner4)
        {
            var setting = _context.Settings.FirstOrDefault();
            if (setting == null)
            {
                setting = new Setting();
                _context.Settings.Add(setting);
            }

            async Task<string?> UploadFile(IFormFile file)
            {
                if (file == null || file.Length == 0) return null;

                string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                string folder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "banner");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string path = Path.Combine(folder, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await file.CopyToAsync(stream);

                return "/images/banner/" + fileName;
            }

            switch (section)
            {
                case "shipping":
                    setting.ShippingFee = model.ShippingFee;
                    break;

                case "banner1_1":
                    Console.WriteLine($"Banner1_1 null? {Banner1_1 == null}");
                    Console.WriteLine($"Banner1_1 Length: {Banner1_1?.Length ?? 0}");
                    if (Banner1_1 != null && Banner1_1.Length > 0)
                    {
                        var uploadedPath = await UploadFile(Banner1_1);
                        Console.WriteLine($"Uploaded to: {uploadedPath}");

                        if (uploadedPath != null)
                        {
                            setting.Banner1_1 = uploadedPath;
                            Console.WriteLine($"Set Banner1_1 to: {setting.Banner1_1}");

                        }
                    }
                    break;

                case "banner1_2":
                    if (Banner1_2 != null && Banner1_2.Length > 0)
                    {
                        var uploadedPath = await UploadFile(Banner1_2);
                        if (uploadedPath != null)
                        {
                            setting.Banner1_2 = uploadedPath;
                        }
                    }
                    break;

                case "banner1_3":
                    if (Banner1_3 != null && Banner1_3.Length > 0)
                    {
                        var uploadedPath = await UploadFile(Banner1_3);
                        if (uploadedPath != null)
                        {
                            setting.Banner1_3 = uploadedPath;
                        }
                    }
                    break;

                case "banner2":
                    if (Banner2 != null && Banner2.Length > 0)
                    {
                        var uploadedPath = await UploadFile(Banner2);
                        if (uploadedPath != null)
                        {
                            setting.Banner2 = uploadedPath;
                        }
                    }
                    break;

                case "banner3":
                    if (Banner3 != null && Banner3.Length > 0)
                    {
                        var uploadedPath = await UploadFile(Banner3);
                        if (uploadedPath != null)
                        {
                            setting.Banner3 = uploadedPath;
                        }
                    }
                    break;

                case "banner4":
                    if (Banner4 != null && Banner4.Length > 0)
                    {
                        var uploadedPath = await UploadFile(Banner4);
                        if (uploadedPath != null)
                        {
                            setting.Banner4 = uploadedPath;
                        }
                    }
                    break;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Updated successfully!";
            return RedirectToAction(nameof(Index));
        }

    }
}