using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStoreApp.Data;
using WatchStoreApp.Models;
using WatchStoreApp.ViewModel.Cart;

namespace WatchStoreApp.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private readonly MyAppContext _context;
        private const string CartSessionKey = "Cart";

        public CartController(MyAppContext context)
        {
            _context = context;
        }

        //public IActionResult Index()
        //{
        //    var cart = GetCart();
        //    return View(cart);
        //}

        public IActionResult Index()
        {
            if (!User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            int customerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var cartItems = _context.Carts
                .Include(c => c.Product)
                .Where(c => c.CustomerId == customerId)
                .ToList();

            var cartVM = new CartVM();
            decimal subtotal = 0;

            foreach (var item in cartItems)
            {
                decimal itemTotal = item.Product.SellingPrice * item.Quantity;
                subtotal += itemTotal;

                cartVM.Items.Add(new CartItemVM
                {
                    ProductId = item.ProductId,
                    Name = item.Product.Name,
                    ImageUrl = item.Product.ImageUrl1,
                    Price = item.Product.SellingPrice,
                    Quantity = item.Quantity
                });
            }

            var appliedCoupon = HttpContext.Session.GetString("CouponCode");
            if (!string.IsNullOrEmpty(appliedCoupon))
            {
                var coupon = _context.Coupons.FirstOrDefault(c => c.CouponCode == appliedCoupon);
                if (coupon != null && DateTime.Now <= coupon.ExpireDate)
                {
                    decimal discount = subtotal * coupon.DiscountRate;
                    ViewBag.CouponCode = coupon.CouponCode;
                    ViewBag.DiscountAmount = discount;
                    ViewBag.FinalTotal = subtotal - discount;
                }
                else
                {
                    HttpContext.Session.Remove("CouponCode");
                }
            }

            return View(cartVM);
        }

        [HttpPost]
        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            if (!User.Identity!.IsAuthenticated)
            {
                return Json(new { success = false, message = "Please login first" });
            }

            var product = _context.Products.Find(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            int customerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var dbCart = _context.Carts.FirstOrDefault(x =>
                x.ProductId == productId &&
                x.CustomerId == customerId
            );

            if (dbCart != null)
            {
                dbCart.Quantity += quantity;
            }
            else
            {
                _context.Carts.Add(new Cart
                {
                    ProductId = productId,
                    Quantity = quantity,
                    CustomerId = customerId
                });
            }

            _context.SaveChanges();

            int totalItems = _context.Carts
                .Where(c => c.CustomerId == customerId)
                .Sum(c => c.Quantity);

            return Json(new { success = true, totalItems });
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                return Json(new { success = false });
            }

            int customerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var cartItem = _context.Carts.FirstOrDefault(x =>
                x.ProductId == productId &&
                x.CustomerId == customerId
            );

            if (cartItem == null)
            {
                return Json(new { success = false });
            }

            cartItem.Quantity = quantity;
            _context.SaveChanges();

            var cartItems = _context.Carts
                .Include(c => c.Product)
                .Where(c => c.CustomerId == customerId)
                .ToList();

            decimal totalPrice = cartItems.Sum(x => x.Product.SellingPrice * x.Quantity);
            int totalItems = cartItems.Sum(x => x.Quantity);

            return Json(new
            {
                success = true,
                totalPrice,
                totalItems
            });
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            int customerId = int.Parse(
                User.FindFirst(ClaimTypes.NameIdentifier)!.Value
            );

            var cartItem = _context.Carts.FirstOrDefault(x =>
                x.ProductId == productId &&
                x.CustomerId == customerId
            );

            if (cartItem != null)
            {
                _context.Carts.Remove(cartItem);
                _context.SaveChanges();
            }

            var cartItems = _context.Carts
                .Include(c => c.Product)
                .Where(c => c.CustomerId == customerId)
                .ToList();

            decimal totalPrice = cartItems.Sum(x => x.Product.SellingPrice * x.Quantity);
            int totalItems = cartItems.Sum(x => x.Quantity);

            return Json(new
            {
                success = true,
                totalPrice,
                totalItems
            });
        }

        [HttpPost]
        public IActionResult ApplyCoupon(string couponCode)
        {
            if (string.IsNullOrEmpty(couponCode))
            {
                return Json(new { success = false, message = "Please enter a coupon code." });
            }

            var coupon = _context.Coupons
                .FirstOrDefault(c => c.CouponCode == couponCode);

            if (coupon == null)
            {
                return Json(new { success = false, message = "Invalid coupon code." });
            }

            var now = DateTime.Now;

            if (now < coupon.StartDate)
            {
                return Json(new { success = false, message = "This coupon is not active yet." });
            }

            if (now > coupon.ExpireDate)
            {
                return Json(new { success = false, message = "This coupon has expired." });
            }

            int customerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var cartItems = _context.Carts
                .Include(c => c.Product)
                .Where(c => c.CustomerId == customerId)
                .ToList();

            decimal subtotal = cartItems.Sum(x => x.Product.SellingPrice * x.Quantity);

            decimal discountAmount = subtotal * coupon.DiscountRate;
            decimal newTotal = subtotal - discountAmount;

            HttpContext.Session.SetString("CouponCode", coupon.CouponCode);
            HttpContext.Session.SetString("DiscountAmount", discountAmount.ToString());

            return Json(new
            {
                success = true,
                message = "Coupon applied!",
                discountAmount = discountAmount,
                newTotal = newTotal
            });
        }
    }
}

