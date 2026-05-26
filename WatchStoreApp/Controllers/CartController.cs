using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
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
                Log.Warning("Add to cart rejected (not authenticated). ProductId={ProductId} Quantity={Quantity}", productId, quantity);
                return Json(new { success = false, message = "Please login first" });
            }

            var product = _context.Products.Find(productId);
            if (product == null)
            {
                Log.Warning("Add to cart failed (product not found). ProductId={ProductId}", productId);
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
                Log.Information("Cart item quantity increased. CustomerId={CustomerId} ProductId={ProductId} AddedQuantity={Quantity} NewQuantity={NewQuantity}", customerId, productId, quantity, dbCart.Quantity);
            }
            else
            {
                _context.Carts.Add(new Cart
                {
                    ProductId = productId,
                    Quantity = quantity,
                    CustomerId = customerId
                });
                Log.Information("Cart item added. CustomerId={CustomerId} ProductId={ProductId} Quantity={Quantity}", customerId, productId, quantity);
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
                Log.Warning("Update cart quantity rejected (invalid quantity). ProductId={ProductId} Quantity={Quantity}", productId, quantity);
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
                Log.Warning("Update cart quantity failed (item not found). CustomerId={CustomerId} ProductId={ProductId}", customerId, productId);
                return Json(new { success = false });
            }

            cartItem.Quantity = quantity;
            _context.SaveChanges();
            Log.Information("Cart item quantity updated. CustomerId={CustomerId} ProductId={ProductId} Quantity={Quantity}", customerId, productId, quantity);

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
                Log.Information("Cart item removed. CustomerId={CustomerId} ProductId={ProductId}", customerId, productId);
            }
            else
            {
                Log.Warning("Remove from cart ignored (item not found). CustomerId={CustomerId} ProductId={ProductId}", customerId, productId);
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
                Log.Warning("Apply coupon rejected (empty code).");
                return Json(new { success = false, message = "Please enter a coupon code." });
            }

            int customerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            Log.Information("Apply coupon attempt. CustomerId={CustomerId} CouponCode={CouponCode}", customerId, couponCode);

            var coupon = _context.Coupons
                .FirstOrDefault(c => c.CouponCode == couponCode);

            if (coupon == null)
            {
                Log.Warning("Apply coupon failed (invalid code). CustomerId={CustomerId} CouponCode={CouponCode}", customerId, couponCode);
                return Json(new { success = false, message = "Invalid coupon code." });
            }

            var now = DateTime.Now;

            if (now < coupon.StartDate)
            {
                Log.Warning("Apply coupon failed (not active yet). CustomerId={CustomerId} CouponCode={CouponCode}", customerId, couponCode);
                return Json(new { success = false, message = "This coupon is not active yet." });
            }

            if (now > coupon.ExpireDate)
            {
                Log.Warning("Apply coupon failed (expired). CustomerId={CustomerId} CouponCode={CouponCode}", customerId, couponCode);
                return Json(new { success = false, message = "This coupon has expired." });
            }

            var cartItems = _context.Carts
                .Include(c => c.Product)
                .Where(c => c.CustomerId == customerId)
                .ToList();

            decimal subtotal = cartItems.Sum(x => x.Product.SellingPrice * x.Quantity);

            decimal discountAmount = subtotal * coupon.DiscountRate;
            decimal newTotal = subtotal - discountAmount;

            HttpContext.Session.SetString("CouponCode", coupon.CouponCode);
            HttpContext.Session.SetString("DiscountAmount", discountAmount.ToString());

            Log.Information("Coupon applied. CustomerId={CustomerId} CouponId={CouponId} CouponCode={CouponCode} DiscountAmount={DiscountAmount}", customerId, coupon.CouponId, coupon.CouponCode, discountAmount);

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

