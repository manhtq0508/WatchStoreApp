using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchStoreApp.Data;
using WatchStoreApp.Models;
using WatchStoreApp.ViewModel.Coupon;

namespace WatchStoreApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CouponController : Controller
    {
        private readonly MyAppContext _context;
        public CouponController(MyAppContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var coupons = _context.Coupons.ToList();
            return View(coupons);
        }

        public IActionResult Search(string search_coupon)
        {
            if (string.IsNullOrEmpty(search_coupon))
            {
                var allCoupons = _context.Coupons.ToList();
                return View("Index", allCoupons);
            }
            var result = _context.Coupons
                .Where(c => c.CouponCode.ToLower().Contains(search_coupon.ToLower()))
                .ToList();
            return View("Index", result);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateCouponVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.DiscountRate <= 0 || model.DiscountRate > 100)
            {
                ModelState.AddModelError("DiscountRate", "Discount Rate should be between 1 and 100 percent");
                return View(model);
            }
            model.DiscountRate = model.DiscountRate / 100;

            var coupon = new Coupon
            {
                CouponCode = model.CouponCode,
                DiscountRate = model.DiscountRate,
                StartDate = model.StartDate,
                ExpireDate = model.ExpireDate
            };

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            var coupon = _context.Coupons.Find(id);
            if (coupon == null)
            {
                return NotFound();
            }
            coupon.DiscountRate = coupon.DiscountRate * 100;
            if (coupon == null)
            {
                return NotFound();
            }
            return View(coupon);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Coupon model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (model.DiscountRate <= 0 || model.DiscountRate > 100)
            {
                ModelState.AddModelError("DiscountRate", "Discount Rate should be between 1 and 100 percent");
                return View(model);
            }
            var coupon = _context.Coupons.Find(model.CouponId);
            if (coupon == null)
            {
                return NotFound();
            }
            coupon.CouponCode = model.CouponCode;
            coupon.DiscountRate = model.DiscountRate / 100;
            coupon.StartDate = model.StartDate;
            coupon.ExpireDate = model.ExpireDate;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            var coupon = _context.Coupons.Find(id);
            if (coupon == null)
            {
                return NotFound();
            }
            coupon.DiscountRate = coupon.DiscountRate * 100;
            return View(coupon);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var coupon = _context.Coupons.Find(id);
            if (coupon == null)
            {
                return NotFound();
            }
            coupon.Flag = 0;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
