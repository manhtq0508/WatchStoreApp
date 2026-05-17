using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WatchStoreApp.Data;
using WatchStoreApp.Models;
using WatchStoreApp.ViewModel.Product;

namespace WatchStoreApp.Controllers
{
    public class ProductController : Controller
    {
        private readonly MyAppContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(MyAppContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        [Authorize(Roles = "Admin, Employee")]
        public IActionResult Index()
        {
            var products = _context.Products.Include(p => p.Brand).ToList();
            return View(products);
        }

        [Authorize(Roles = "Admin, Employee")]
        public IActionResult CreateMechanical()
        {
            var brands = _context.Brands
                .Where(b => b.Flag == 1)
                .Select(b => new SelectListItem
                {
                    Value = b.BrandId.ToString(),
                    Text = b.BrandName
                }).ToList();

            var genders = new List<SelectListItem>
            {
                new SelectListItem { Value = "Male", Text = "Male" },
                new SelectListItem { Value = "Female", Text = "Female" },
                new SelectListItem { Value = "Other", Text = "Other" }
            };

            var strapMaterials = new List<SelectListItem>
            {
                new SelectListItem { Value = "Leather", Text = "Leather" },
                new SelectListItem { Value = "Metal", Text = "Metal" },
                new SelectListItem { Value = "Silicone", Text = "Silicone" }
            };

            var watchStyles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Classic", Text = "Classic" },
                new SelectListItem { Value = "Modern", Text = "Modern" },
                new SelectListItem { Value = "Sport", Text = "Sport" },
                new SelectListItem { Value = "Luxury", Text = "Luxury" }
            };

            var vm = new CreateMechanicalVM
            {
                BrandList = brands,
                GenderList = genders,
                StrapMaterialList = strapMaterials,
                WatchStyleList = watchStyles
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin, Employee")]
        [HttpPost]
        public async Task<IActionResult> CreateMechanical(CreateMechanicalVM model)
        {
            if (!ModelState.IsValid)
            {
                model.BrandList = _context.Brands
                .Where(b => b.Flag == 1)
                .Select(b => new SelectListItem
                {
                    Value = b.BrandId.ToString(),
                    Text = b.BrandName
                }).ToList();

                model.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Male", Text = "Male" },
                    new SelectListItem { Value = "Female", Text = "Female" },
                    new SelectListItem { Value = "Other", Text = "Other" }
                };

                model.StrapMaterialList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Leather", Text = "Leather" },
                    new SelectListItem { Value = "Metal", Text = "Metal" },
                    new SelectListItem { Value = "Silicone", Text = "Silicone" }
                };

                model.WatchStyleList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Classic", Text = "Classic" },
                    new SelectListItem { Value = "Modern", Text = "Modern" },
                    new SelectListItem { Value = "Sport", Text = "Sport" },
                    new SelectListItem { Value = "Luxury", Text = "Luxury" }
                };
                return View(model);
            }

            var product = new Product
            {
                Name = model.Name,
                BrandId = model.BrandId,
                Gender = model.Gender,
                WarrantyPeriod = model.WarrantyPeriod,
                WatchType = "Mechanical",
                GlassMaterial = model.GlassMaterial,
                CaseDiameter = model.CaseDiameter,
                CaseThickness = model.CaseThickness,
                WaterResistance = model.WaterResistance,
                ProductGenre = model.ProductGenre,
                StrapMaterial = model.StrapMaterial,
                StrapColor = model.StrapColor,
                WatchStyle = model.WatchStyle,
                ImportPrice = model.ImportPrice,
                SellingPrice = model.SellingPrice,
                StockQuantity = model.StockQuantity,
                Flag = 1
            };

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            if (model.ImageUrl1 != null && model.ImageUrl1.Length > 0)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageUrl1.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageUrl1.CopyToAsync(stream);
                }
                product.ImageUrl1 = "/images/products/" + fileName;
            }

            if (model.ImageUrl2 != null && model.ImageUrl2.Length > 0)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageUrl2.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageUrl2.CopyToAsync(stream);
                }
                product.ImageUrl2 = "/images/products/" + fileName;
            }

            if (model.ImageUrl3 != null && model.ImageUrl3.Length > 0)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageUrl3.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageUrl3.CopyToAsync(stream);
                }
                product.ImageUrl3 = "/images/products/" + fileName;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var mechanicalWatch = new MechanicalWatch
            {
                ProductId = product.ProductId,
                CalendarFunction = model.CalendarFunction,
                Functions = model.Functions,
                Movement = model.Movement,
                CaseShape = model.CaseShape
            };

            _context.MechanicalWatches.Add(mechanicalWatch);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");

        }

        [Authorize(Roles = "Admin, Employee")]
        public IActionResult CreateSmartwatch()
        {
            var brands = _context.Brands
                .Where(b => b.Flag == 1)
                .Select(b => new SelectListItem
                {
                    Value = b.BrandId.ToString(),
                    Text = b.BrandName
                }).ToList();

            var genders = new List<SelectListItem>
            {
                new SelectListItem { Value = "Male", Text = "Male" },
                new SelectListItem { Value = "Female", Text = "Female" },
                new SelectListItem { Value = "Other", Text = "Other" }
            };

            var strapMaterials = new List<SelectListItem>
            {
                new SelectListItem { Value = "Leather", Text = "Leather" },
                new SelectListItem { Value = "Metal", Text = "Metal" },
                new SelectListItem { Value = "Silicone", Text = "Silicone" }
            };

            var watchStyles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Classic", Text = "Classic" },
                new SelectListItem { Value = "Modern", Text = "Modern" },
                new SelectListItem { Value = "Sport", Text = "Sport" },
                new SelectListItem { Value = "Luxury", Text = "Luxury" }
            };

            var vm = new CreateSmartwatchVM
            {
                BrandList = brands,
                GenderList = genders,
                StrapMaterialList = strapMaterials,
                WatchStyleList = watchStyles
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin, Employee")]

        [HttpPost]
        public async Task<IActionResult> CreateSmartwatch(CreateSmartwatchVM model)
        {
            if (!ModelState.IsValid)
            {
                model.BrandList = _context.Brands
                .Where(b => b.Flag == 1)
                .Select(b => new SelectListItem
                {
                    Value = b.BrandId.ToString(),
                    Text = b.BrandName
                }).ToList();

                model.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Male", Text = "Male" },
                    new SelectListItem { Value = "Female", Text = "Female" },
                    new SelectListItem { Value = "Other", Text = "Other" }
                };

                model.StrapMaterialList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Leather", Text = "Leather" },
                    new SelectListItem { Value = "Metal", Text = "Metal" },
                    new SelectListItem { Value = "Silicone", Text = "Silicone" }
                };

                model.WatchStyleList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Classic", Text = "Classic" },
                    new SelectListItem { Value = "Modern", Text = "Modern" },
                    new SelectListItem { Value = "Sport", Text = "Sport" },
                    new SelectListItem { Value = "Luxury", Text = "Luxury" }
                };
                return View(model);
            }

            var product = new Product
            {
                Name = model.Name,
                BrandId = model.BrandId,
                Gender = model.Gender,
                WarrantyPeriod = model.WarrantyPeriod,
                WatchType = "Smartwatch",
                GlassMaterial = model.GlassMaterial,
                CaseDiameter = model.CaseDiameter,
                CaseThickness = model.CaseThickness,
                WaterResistance = model.WaterResistance,
                ProductGenre = model.ProductGenre,
                StrapMaterial = model.StrapMaterial,
                StrapColor = model.StrapColor,
                WatchStyle = model.WatchStyle,
                ImportPrice = model.ImportPrice,
                SellingPrice = model.SellingPrice,
                StockQuantity = model.StockQuantity,
                Flag = 1
            };

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            if (model.ImageUrl1 != null && model.ImageUrl1.Length > 0)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageUrl1.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageUrl1.CopyToAsync(stream);
                }
                product.ImageUrl1 = "/images/products/" + fileName;
            }

            if (model.ImageUrl2 != null && model.ImageUrl2.Length > 0)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageUrl2.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageUrl2.CopyToAsync(stream);
                }
                product.ImageUrl2 = "/images/products/" + fileName;
            }

            if (model.ImageUrl3 != null && model.ImageUrl3.Length > 0)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageUrl3.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageUrl3.CopyToAsync(stream);
                }
                product.ImageUrl3 = "/images/products/" + fileName;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var smartwatch = new SmartWatch
            {
                ProductId = product.ProductId,
                BateryLife = model.BateryLife,
                DisplayResolution = model.DisplayResolution,
                DisplayTechnology = model.DisplayTechnology,
                ScreenSize = model.ScreenSize,
                Sensors = model.Sensors
            };

            _context.SmartWatches.Add(smartwatch);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");

        }

        [Authorize(Roles = "Admin, Employee")]
        public IActionResult EditMechanical(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == id && p.WatchType == "Mechanical");
            if (product == null)
                return NotFound();

            var mechanical = _context.MechanicalWatches.FirstOrDefault(s => s.ProductId == id);
            if (mechanical == null)
                return NotFound();

            var brands = _context.Brands
                .Where(b => b.Flag == 1)
                .Select(b => new SelectListItem
                {
                    Value = b.BrandId.ToString(),
                    Text = b.BrandName
                }).ToList();

            var genders = new List<SelectListItem>
            {
                new SelectListItem { Value = "Male", Text = "Male" },
                new SelectListItem { Value = "Female", Text = "Female" },
                new SelectListItem { Value = "Other", Text = "Other" }
            };

            var strapMaterials = new List<SelectListItem>
            {
                new SelectListItem { Value = "Leather", Text = "Leather" },
                new SelectListItem { Value = "Metal", Text = "Metal" },
                new SelectListItem { Value = "Silicone", Text = "Silicone" }
            };

            var watchStyles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Classic", Text = "Classic" },
                new SelectListItem { Value = "Modern", Text = "Modern" },
                new SelectListItem { Value = "Sport", Text = "Sport" },
                new SelectListItem { Value = "Luxury", Text = "Luxury" }
            };

            var vm = new EditMechanicalVM
            {
                ProductId = product.ProductId,
                Name = product.Name,
                BrandId = product.BrandId,
                Gender = product.Gender,
                WarrantyPeriod = product.WarrantyPeriod,
                WatchType = product.WatchType,
                GlassMaterial = product.GlassMaterial,
                CaseDiameter = product.CaseDiameter,
                CaseThickness = product.CaseThickness,
                WaterResistance = product.WaterResistance,
                ProductGenre = product.ProductGenre,
                StrapMaterial = product.StrapMaterial,
                StrapColor = product.StrapColor,
                WatchStyle = product.WatchStyle,
                ImportPrice = product.ImportPrice,
                SellingPrice = product.SellingPrice,
                StockQuantity = product.StockQuantity,
                CalendarFunction = mechanical.CalendarFunction,
                Functions = mechanical.Functions,
                Movement = mechanical.Movement,
                CaseShape = mechanical.CaseShape,

                ImageUrl1Path = product.ImageUrl1,
                ImageUrl2Path = product.ImageUrl2,
                ImageUrl3Path = product.ImageUrl3,

                BrandList = brands,
                GenderList = genders,
                StrapMaterialList = strapMaterials,
                WatchStyleList = watchStyles
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin, Employee")]
        public IActionResult EditSmartwatch(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == id && p.WatchType == "Smartwatch");
            if (product == null)
                return NotFound();

            var smartwatch = _context.SmartWatches.FirstOrDefault(s => s.ProductId == id);
            if (smartwatch == null)
                return NotFound();

            var brands = _context.Brands
                .Where(b => b.Flag == 1)
                .Select(b => new SelectListItem
                {
                    Value = b.BrandId.ToString(),
                    Text = b.BrandName
                }).ToList();

            var genders = new List<SelectListItem>
            {
                new SelectListItem { Value = "Male", Text = "Male" },
                new SelectListItem { Value = "Female", Text = "Female" },
                new SelectListItem { Value = "Other", Text = "Other" }
            };

            var strapMaterials = new List<SelectListItem>
            {
                new SelectListItem { Value = "Leather", Text = "Leather" },
                new SelectListItem { Value = "Metal", Text = "Metal" },
                new SelectListItem { Value = "Silicone", Text = "Silicone" }
            };

            var watchStyles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Classic", Text = "Classic" },
                new SelectListItem { Value = "Modern", Text = "Modern" },
                new SelectListItem { Value = "Sport", Text = "Sport" },
                new SelectListItem { Value = "Luxury", Text = "Luxury" }
            };

            var vm = new EditSmartwatchVM
            {
                ProductId = product.ProductId,
                Name = product.Name,
                BrandId = product.BrandId,
                Gender = product.Gender,
                WarrantyPeriod = product.WarrantyPeriod,
                WatchType = product.WatchType,
                GlassMaterial = product.GlassMaterial,
                CaseDiameter = product.CaseDiameter,
                CaseThickness = product.CaseThickness,
                WaterResistance = product.WaterResistance,
                ProductGenre = product.ProductGenre,
                StrapMaterial = product.StrapMaterial,
                StrapColor = product.StrapColor,
                WatchStyle = product.WatchStyle,
                ImportPrice = product.ImportPrice,
                SellingPrice = product.SellingPrice,
                StockQuantity = product.StockQuantity,
                BateryLife = smartwatch.BateryLife,
                DisplayResolution = smartwatch.DisplayResolution,
                DisplayTechnology = smartwatch.DisplayTechnology,
                ScreenSize = smartwatch.ScreenSize,
                Sensors = smartwatch.Sensors,

                ImageUrl1Path = product.ImageUrl1,
                ImageUrl2Path = product.ImageUrl2,
                ImageUrl3Path = product.ImageUrl3,

                BrandList = brands,
                GenderList = genders,
                StrapMaterialList = strapMaterials,
                WatchStyleList = watchStyles
            };

            return View(vm);
        }

        [Authorize(Roles = "Admin, Employee")]
        [HttpPost]
        public async Task<IActionResult> EditImage1(EditSmartwatchVM model)
        {
            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null)
                return NotFound();
            if (model.ImageUrl1 != null && model.ImageUrl1.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageUrl1.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageUrl1.CopyToAsync(stream);
                }

                if (!string.IsNullOrEmpty(product.ImageUrl1))
                {
                    string existingFilePath = Path.Combine(_webHostEnvironment.WebRootPath,
                        product.ImageUrl1.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(existingFilePath))
                    {
                        System.IO.File.Delete(existingFilePath);
                    }
                }

                product.ImageUrl1 = "/images/products/" + fileName;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                return RedirectToAction("EditSmartwatch", new { id = model.ProductId });
            }
            else
            {
                return RedirectToAction("EditSmartwatch", new { id = model.ProductId });
            }
        }

        [Authorize(Roles = "Admin, Employee")]
        [HttpPost]
        public async Task<IActionResult> EditImage2(EditSmartwatchVM model)
        {
            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null)
                return NotFound();
            if (model.ImageUrl2 != null && model.ImageUrl2.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageUrl2.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageUrl2.CopyToAsync(stream);
                }

                if (!string.IsNullOrEmpty(product.ImageUrl2))
                {
                    string existingFilePath = Path.Combine(_webHostEnvironment.WebRootPath,
                        product.ImageUrl2.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(existingFilePath))
                    {
                        System.IO.File.Delete(existingFilePath);
                    }
                }

                product.ImageUrl2 = "/images/products/" + fileName;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                return RedirectToAction("EditSmartwatch", new { id = model.ProductId });
            }
            else
            {
                return RedirectToAction("EditSmartwatch", new { id = model.ProductId });
            }
        }

        [Authorize(Roles = "Admin, Employee")]
        [HttpPost]
        public async Task<IActionResult> EditImage3(EditSmartwatchVM model)
        {
            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null)
                return NotFound();
            if (model.ImageUrl3 != null && model.ImageUrl3.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageUrl3.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageUrl3.CopyToAsync(stream);
                }

                if (!string.IsNullOrEmpty(product.ImageUrl3))
                {
                    string existingFilePath = Path.Combine(_webHostEnvironment.WebRootPath,
                        product.ImageUrl3.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(existingFilePath))
                    {
                        System.IO.File.Delete(existingFilePath);
                    }
                }

                product.ImageUrl3 = "/images/products/" + fileName;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                return RedirectToAction("EditSmartwatch", new { id = model.ProductId });
            }
            else
            {
                return RedirectToAction("EditSmartwatch", new { id = model.ProductId });
            }
        }

        [Authorize(Roles = "Admin, Employee")]
        [HttpPost]
        public async Task<IActionResult> EditImage1M(EditMechanicalVM model)
        {
            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null)
                return NotFound();
            if (model.ImageUrl1 != null && model.ImageUrl1.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageUrl1.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageUrl1.CopyToAsync(stream);
                }

                if (!string.IsNullOrEmpty(product.ImageUrl1))
                {
                    string existingFilePath = Path.Combine(_webHostEnvironment.WebRootPath,
                        product.ImageUrl1.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(existingFilePath))
                    {
                        System.IO.File.Delete(existingFilePath);
                    }
                }

                product.ImageUrl1 = "/images/products/" + fileName;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                return RedirectToAction("EditMechanical", new { id = model.ProductId });
            }
            else
            {
                return RedirectToAction("EditMechanical", new { id = model.ProductId });
            }
        }

        [Authorize(Roles = "Admin, Employee")]
        [HttpPost]
        public async Task<IActionResult> EditImage2M(EditMechanicalVM model)
        {
            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null)
                return NotFound();
            if (model.ImageUrl2 != null && model.ImageUrl2.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageUrl2.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageUrl2.CopyToAsync(stream);
                }

                if (!string.IsNullOrEmpty(product.ImageUrl2))
                {
                    string existingFilePath = Path.Combine(_webHostEnvironment.WebRootPath,
                        product.ImageUrl2.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(existingFilePath))
                    {
                        System.IO.File.Delete(existingFilePath);
                    }
                }

                product.ImageUrl2 = "/images/products/" + fileName;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                return RedirectToAction("EditMechanical", new { id = model.ProductId });
            }
            else
            {
                return RedirectToAction("EditMechanical", new { id = model.ProductId });
            }
        }

        [Authorize(Roles = "Admin, Employee")]
        [HttpPost]
        public async Task<IActionResult> EditImage3M(EditMechanicalVM model)
        {
            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null)
                return NotFound();
            if (model.ImageUrl3 != null && model.ImageUrl3.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);
                string fileName = Guid.NewGuid() + Path.GetExtension(model.ImageUrl3.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageUrl3.CopyToAsync(stream);
                }

                if (!string.IsNullOrEmpty(product.ImageUrl3))
                {
                    string existingFilePath = Path.Combine(_webHostEnvironment.WebRootPath,
                        product.ImageUrl3.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(existingFilePath))
                    {
                        System.IO.File.Delete(existingFilePath);
                    }
                }

                product.ImageUrl3 = "/images/products/" + fileName;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                return RedirectToAction("EditMechanical", new { id = model.ProductId });
            }
            else
            {
                return RedirectToAction("EditMechanical", new { id = model.ProductId });
            }
        }

        [Authorize(Roles = "Admin, Employee")]
        [HttpPost]
        public async Task<IActionResult> EditSmartwatch(EditSmartwatchVM model)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model is valid");
                model.BrandList = _context.Brands
                    .Select(b => new SelectListItem
                    {
                        Value = b.BrandId.ToString(),
                        Text = b.BrandName
                    }).ToList();

                model.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Male", Text = "Male" },
                    new SelectListItem { Value = "Female", Text = "Female" },
                    new SelectListItem { Value = "Other", Text = "Other" }
                };

                        model.StrapMaterialList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Leather", Text = "Leather" },
                    new SelectListItem { Value = "Metal", Text = "Metal" },
                    new SelectListItem { Value = "Silicone", Text = "Silicone" }
                };

                        model.WatchStyleList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Classic", Text = "Classic" },
                    new SelectListItem { Value = "Modern", Text = "Modern" },
                    new SelectListItem { Value = "Sport", Text = "Sport" },
                    new SelectListItem { Value = "Luxury", Text = "Luxury" }
                };

                return View(model);
            }

            var product = await _context.Products.FindAsync(model.ProductId);
            var smartwatch = await _context.SmartWatches.FirstOrDefaultAsync(s => s.ProductId == model.ProductId);

            if (product == null || smartwatch == null)
                return NotFound();

            product.Name = model.Name;
            product.BrandId = model.BrandId;
            product.Gender = model.Gender;
            product.WarrantyPeriod = model.WarrantyPeriod;
            product.WatchType = "Smartwatch";
            product.GlassMaterial = model.GlassMaterial;
            product.CaseDiameter = model.CaseDiameter;
            product.CaseThickness = model.CaseThickness;
            product.WaterResistance = model.WaterResistance;
            product.ProductGenre = model.ProductGenre;
            product.StrapMaterial = model.StrapMaterial;
            product.StrapColor = model.StrapColor;
            product.WatchStyle = model.WatchStyle;
            product.ImportPrice = model.ImportPrice;
            product.SellingPrice = model.SellingPrice;
            product.StockQuantity = model.StockQuantity;

            smartwatch.BateryLife = model.BateryLife;
            smartwatch.DisplayResolution = model.DisplayResolution;
            smartwatch.DisplayTechnology = model.DisplayTechnology;
            smartwatch.ScreenSize = model.ScreenSize;
            smartwatch.Sensors = model.Sensors;

            _context.Products.Update(product);
            _context.SmartWatches.Update(smartwatch);
            await _context.SaveChangesAsync();

            return RedirectToAction("EditSmartwatch", new { id = model.ProductId });
        }

        [Authorize(Roles = "Admin, Employee")]
        [HttpPost]
        public async Task<IActionResult> EditMechanical(EditMechanicalVM model)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine("Model is valid");
                model.BrandList = _context.Brands
                    .Select(b => new SelectListItem
                    {
                        Value = b.BrandId.ToString(),
                        Text = b.BrandName
                    }).ToList();

                model.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Male", Text = "Male" },
                    new SelectListItem { Value = "Female", Text = "Female" },
                    new SelectListItem { Value = "Other", Text = "Other" }
                };

                model.StrapMaterialList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Leather", Text = "Leather" },
                    new SelectListItem { Value = "Metal", Text = "Metal" },
                    new SelectListItem { Value = "Silicone", Text = "Silicone" }
                };

                model.WatchStyleList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Classic", Text = "Classic" },
                    new SelectListItem { Value = "Modern", Text = "Modern" },
                    new SelectListItem { Value = "Sport", Text = "Sport" },
                    new SelectListItem { Value = "Luxury", Text = "Luxury" }
                };

                return View(model);
            }

            var product = await _context.Products.FindAsync(model.ProductId);
            var mechanical = await _context.MechanicalWatches.FirstOrDefaultAsync(s => s.ProductId == model.ProductId);

            if (product == null || mechanical == null)
                return NotFound();

            product.Name = model.Name;
            product.BrandId = model.BrandId;
            product.Gender = model.Gender;
            product.WarrantyPeriod = model.WarrantyPeriod;
            product.WatchType = "Mechanical";
            product.GlassMaterial = model.GlassMaterial;
            product.CaseDiameter = model.CaseDiameter;
            product.CaseThickness = model.CaseThickness;
            product.WaterResistance = model.WaterResistance;
            product.ProductGenre = model.ProductGenre;
            product.StrapMaterial = model.StrapMaterial;
            product.StrapColor = model.StrapColor;
            product.WatchStyle = model.WatchStyle;
            product.ImportPrice = model.ImportPrice;
            product.SellingPrice = model.SellingPrice;
            product.StockQuantity = model.StockQuantity;

            mechanical.CalendarFunction = model.CalendarFunction;
            mechanical.Functions = model.Functions;
            mechanical.Movement = model.Movement;
            mechanical.CaseShape = model.CaseShape;

            _context.Products.Update(product);
            _context.MechanicalWatches.Update(mechanical);
            await _context.SaveChangesAsync();

            return RedirectToAction("EditMechanical", new { id = model.ProductId });
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == id);
            if (product == null)
                return NotFound();
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();
            product.Flag = 0;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
     }
}
