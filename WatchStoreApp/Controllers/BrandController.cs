using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using WatchStoreApp.Data;
using WatchStoreApp.Models;
using WatchStoreApp.ViewModel.Brand;

namespace WatchStoreApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BrandController(MyAppContext context, IWebHostEnvironment webHostEnvironment)
        : Controller
    {
        public IActionResult Index()
        {
            var brands = context.Brands.ToList();
            return View(brands);
        }

        [HttpPost]
        public IActionResult Search(string searchBrand)
        {
            if (string.IsNullOrEmpty(searchBrand))
            {
                var allBrands = context.Brands.ToList();
                return View("Index", allBrands);
            }

            var result = context.Brands
                .Where(b => b.BrandName.ToLower().Contains(searchBrand.ToLower()))
                .ToList();
            return View("Index", result);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateBrandVM model)
        {
            if (ModelState.IsValid)
            {
                var brand = new Brand
                {
                    BrandName = model.BrandName,
                    Origin = model.Origin,
                    ImageUrl = ""
                };
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "images", "brands");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    string fileExtension = Path.GetExtension(model.ImageFile.FileName);
                    string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;

                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }

                    brand.ImageUrl = "/images/brands/" + uniqueFileName;
                }
                context.Brands.Add(brand);
                await context.SaveChangesAsync();
                Log.Information("Brand created. BrandId={BrandId} BrandName={BrandName}", brand.BrandId, brand.BrandName);
                return RedirectToAction("Index");

            }
            Log.Warning("Brand create rejected (invalid model).");
            return View(model);
        }

        public IActionResult Edit(int id)
        {
            var brand = context.Brands.Find(id);
            if (brand == null)
            {
                return NotFound();
            }
            var editBrandVm = new EditBrandVM
            {
                BrandId = brand.BrandId,
                BrandName = brand.BrandName,
                Origin = brand.Origin,
                ImageUrl = brand.ImageUrl
            };
            return View(editBrandVm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditBrandVM model)
        {
            var brand = context.Brands.Find(model.BrandId);
            if (brand == null)
            {
                Log.Warning("Brand edit failed (not found). BrandId={BrandId}", model.BrandId);
                return NotFound();
            }

            brand.BrandName = model.BrandName;
            brand.Origin = model.Origin;

            context.Brands.Update(brand);
            await context.SaveChangesAsync();

            Log.Information("Brand updated. BrandId={BrandId}", brand.BrandId);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> EditImage(EditBrandVM model)
        {
            var brand = await context.Brands.FindAsync(model.BrandId);
            if (brand == null)
                return NotFound();

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(webHostEnvironment.WebRootPath, "images", "brands");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileExtension = Path.GetExtension(model.ImageFile.FileName);
                string uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(fileStream);
                }

                if (!string.IsNullOrEmpty(brand.ImageUrl))
                {
                    string existingFilePath = Path.Combine(webHostEnvironment.WebRootPath, brand.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(existingFilePath))
                    {
                        System.IO.File.Delete(existingFilePath);
                    }
                }

                brand.ImageUrl = "/images/brands/" + uniqueFileName;
                context.Brands.Update(brand);
                await context.SaveChangesAsync();
                Log.Information("Brand image updated. BrandId={BrandId}", brand.BrandId);
            }
            return RedirectToAction("Edit", new { id = model.BrandId });
        }

        public IActionResult Delete(int id)
        {
            var brand = context.Brands.Find(id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var brand = context.Brands.Find(id);
            if (brand == null)
            {
                Log.Warning("Brand delete failed (not found). BrandId={BrandId}", id);
                return NotFound();
            }
            brand.Flag = 0;
            context.Brands.Update(brand);
            await context.SaveChangesAsync();
            Log.Information("Brand deleted (flagged). BrandId={BrandId}", id);
            return RedirectToAction("Index");
        }
    }
}
