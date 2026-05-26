using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Stripe;
using Stripe.Checkout;
using WatchStoreApp.Data;
using WatchStoreApp.Models;
using WatchStoreApp.ViewModel.Cart;
using WatchStoreApp.ViewModel.Payment;
using Customer = WatchStoreApp.Models.Customer;
using Invoice = WatchStoreApp.Models.Invoice;

namespace WatchStoreApp.Controllers
{
    [Authorize(Roles = "Customer")]
    public class PaymentController : Controller
    {
        private readonly MyAppContext _context;
        private readonly IConfiguration _configuration;
        private const string CartSessionKey = "Cart";

        public PaymentController(MyAppContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        private static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return "";
            var atIndex = email.IndexOf('@');
            if (atIndex <= 1) return "***";
            var local = email[..atIndex];
            var domain = email[atIndex..];
            var maskedLocal = local.Length <= 2
                ? local[0] + "***"
                : local[0] + new string('*', Math.Min(6, local.Length - 2)) + local[^1];
            return maskedLocal + domain;
        }

//#warning Remove before deployment
        private CartVM CreateMockCart()
        {
            var cartItems = new List<CartItemVM>
            {
                new CartItemVM
                {
                    ProductId = 1,
                    Name = "Mock Product 1",
                    ImageUrl = "/images/product1.jpg",
                    Price = 1000000,
                    Quantity = 2
                },
                new CartItemVM
                {
                    ProductId = 2,
                    Name = "Mock Product 2",
                    ImageUrl = "/images/product2.jpg",
                    Price = 1500000,
                    Quantity = 1
                }
            };
            return new CartVM()
            {
                Items = cartItems,
            };
        }

        [HttpGet]
        public IActionResult Index(int? productId = null, int quantity = 1)
        {
            var authCustomerId = User.Identity?.IsAuthenticated == true
                ? User.FindFirstValue(ClaimTypes.NameIdentifier)
                : null;
            Log.Information("Checkout page opened. CustomerId={CustomerId} BuyNowProductId={ProductId} Quantity={Quantity}", authCustomerId, productId, quantity);

            var paymentVM = new PaymentVM();
            var setting = _context.Settings.FirstOrDefault();
            var baseShippingFee = setting?.ShippingFee ?? 500000m;
            paymentVM.BaseShippingFee = baseShippingFee;
            var shippingFee = CalculateShippingFee(baseShippingFee, paymentVM.ShippingMethod);

            // If productId is provided (Buy Now), create a cart with single item
            if (productId.HasValue)
            {
                var product = _context.Products.Find(productId.Value);
                if (product == null)
                {
                    Log.Warning("Checkout redirected (product not found). ProductId={ProductId}", productId.Value);
                    return RedirectToAction("Index", "Home");
                }

                paymentVM.ProductId = productId.Value;
                paymentVM.Quantity = quantity;
                paymentVM.Cart.Items.Add(new CartItemVM
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    ImageUrl = product.ImageUrl1,
                    Price = product.SellingPrice,
                    Quantity = quantity
                });
            }
            else
            {
                if (!User.Identity!.IsAuthenticated)
                {
                    Log.Warning("Checkout rejected (not authenticated).");
                    return Json(new { success = false, message = "Please login first" });
                }

                int customerId = int.Parse(
                    User.FindFirst(ClaimTypes.NameIdentifier)!.Value
                );

                var cartItemsInDb = _context.Carts
                    .Include(c => c.Product)
                    .Where(c => c.CustomerId == customerId)
                    .ToList();

                if (!cartItemsInDb.Any())
                {
                    // Cart rỗng → redirect về Cart
                    Log.Information("Checkout redirected (empty cart). CustomerId={CustomerId}", customerId);
                    return RedirectToAction("Index", "Cart");
                }

                paymentVM.Cart = new CartVM();
                foreach (var item in cartItemsInDb)
                {
                    paymentVM.Cart.Items.Add(new CartItemVM
                    {
                        ProductId = item.ProductId,
                        Name = item.Product.Name,
                        ImageUrl = item.Product.ImageUrl1,
                        Price = item.Product.SellingPrice,
                        Quantity = item.Quantity
                    });
                }
            }

            // Calculate totals
            paymentVM.Subtotal = paymentVM.Cart.TotalPrice;
            paymentVM.ShippingFee = shippingFee;
            paymentVM.Discount = 0;
            paymentVM.Total = paymentVM.Subtotal + paymentVM.ShippingFee - paymentVM.Discount;

            return View(paymentVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcessPayment(PaymentVM model)
        {
            var authCustomerId = User.Identity?.IsAuthenticated == true
                ? User.FindFirstValue(ClaimTypes.NameIdentifier)
                : null;
            Log.Information(
                "Checkout submit received. AuthCustomerId={AuthCustomerId} BuyNowProductId={ProductId} PaymentMethod={PaymentMethod} ShippingMethod={ShippingMethod} Email={Email}",
                authCustomerId,
                model.ProductId,
                model.PaymentMethod,
                model.ShippingMethod,
                MaskEmail(model.Email)
            );

            var setting = _context.Settings.FirstOrDefault();
            var baseShippingFee = setting?.ShippingFee ?? 500000m;

            // Lấy cart thật: Buy Now hoặc Cart DB
            CartVM cart = ReconstructCart(model);

            // Validate thông tin khách
            if (string.IsNullOrEmpty(model.CustomerName))
                ModelState.AddModelError("CustomerName", "Customer name is required.");
            if (string.IsNullOrEmpty(model.Email))
                ModelState.AddModelError("Email", "Email is required.");
            if (string.IsNullOrEmpty(model.PhoneNumber))
                ModelState.AddModelError("PhoneNumber", "Phone number is required.");
            if (string.IsNullOrEmpty(model.Address))
                ModelState.AddModelError("Address", "Address is required.");

            // Kiểm tra số lượng sản phẩm
            var productIds = cart.Items.Select(i => i.ProductId).ToList();
            var productsInDb = _context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionary(p => p.ProductId, p => p.StockQuantity);

            foreach (var item in cart.Items)
            {
                if (!productsInDb.ContainsKey(item.ProductId))
                {
                    ModelState.AddModelError("", $"Sản phẩm {item.Name} không tồn tại.");
                    Log.Warning("Checkout validation failed (product not found). ProductId={ProductId}", item.ProductId);
                }
                else if (item.Quantity > productsInDb[item.ProductId])
                {
                    ModelState.AddModelError("", $"Sản phẩm {item.Name} chỉ còn {productsInDb[item.ProductId]} trong kho.");
                    Log.Warning("Checkout validation failed (insufficient stock). ProductId={ProductId} Requested={Requested} Available={Available}", item.ProductId, item.Quantity, productsInDb[item.ProductId]);
                }
            }

            if (!ModelState.IsValid)
            {
                Log.Warning("Checkout submit rejected (invalid model state). Email={Email}", MaskEmail(model.Email));
                model.Cart = cart;
                model.Subtotal = cart.TotalPrice;
                model.BaseShippingFee = baseShippingFee;
                model.ShippingFee = CalculateShippingFee(baseShippingFee, model.ShippingMethod);
                model.Discount = 0;
                model.Total = model.Subtotal + model.ShippingFee - model.Discount;
                return View("Index", model);
            }

            // Tìm hoặc tạo customer
            var customer = _context.Customers.FirstOrDefault(c => c.Email == model.Email);
            if (customer == null)
            {
                customer = new Customer
                {
                    Name = model.CustomerName,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Gender = "",
                    DateOfBirth = DateTime.Now,
                    IsAvailable = "Available",
                    Password = "" // Guest checkout
                };
                _context.Customers.Add(customer);
                _context.SaveChanges();
                Log.Information("Guest customer created during checkout. CustomerId={CustomerId} Email={Email}", customer.CustomerId, MaskEmail(customer.Email));
            }
            else
            {
                Log.Information("Existing customer used for checkout. CustomerId={CustomerId}", customer.CustomerId);
            }

            // Tính tổng với discount
            var shippingFee = CalculateShippingFee(baseShippingFee, model.ShippingMethod);
            decimal discount = 0;
            int? couponId = null;
            var subtotal = cart.TotalPrice;

            if (!string.IsNullOrEmpty(model.DiscountCode))
            {
                var coupon = _context.Coupons
                    .FirstOrDefault(c => c.CouponCode == model.DiscountCode
                                        && c.StartDate <= DateTime.Now
                                        && c.ExpireDate >= DateTime.Now
                                        && c.Flag == 1);
                if (coupon != null)
                {
                    couponId = coupon.CouponId;
                    discount = subtotal * coupon.DiscountRate;
                    Log.Information("Coupon applied during checkout. CustomerId={CustomerId} CouponId={CouponId} CouponCode={CouponCode} Discount={Discount}", customer.CustomerId, couponId, model.DiscountCode, discount);
                }
                else
                {
                    Log.Warning("Coupon rejected during checkout. CustomerId={CustomerId} CouponCode={CouponCode}", customer.CustomerId, model.DiscountCode);
                }
            }

            var total = subtotal + shippingFee - discount;

            // Tạo invoice
            var invoice = new Invoice
            {
                CustomerId = customer.CustomerId,
                Date = DateTime.Now,
                PaymentMethod = model.PaymentMethod,
                ShippingMethod = model.ShippingMethod,
                Address = model.Address,
                ShippingNote = model.ShippingNote,
                DiscountCode = model.DiscountCode,
                CouponId = couponId,
                Subtotal = subtotal,
                Discount = discount,
                ShippingFee = shippingFee,
                Total = total,
                Status = "Pending"
            };

            // Thêm chi tiết invoice & trừ stock
            foreach (var item in cart.Items)
            {
                var product = _context.Products.Find(item.ProductId);
                if (product != null)
                {
                    invoice.InvoiceDetails.Add(new InvoiceDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Price
                    });
                    product.StockQuantity -= item.Quantity;
                    _context.Products.Update(product);
                    Log.Information("Stock decremented for checkout. ProductId={ProductId} Quantity={Quantity} RemainingStock={Remaining}", product.ProductId, item.Quantity, product.StockQuantity);
                }
                else
                {
                    Log.Warning("Invoice detail skipped (product not found). ProductId={ProductId}", item.ProductId);
                }
            }

            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            Log.Information("Invoice created. InvoiceId={InvoiceId} CustomerId={CustomerId} PaymentMethod={PaymentMethod} Total={Total}", invoice.InvoiceId, invoice.CustomerId, invoice.PaymentMethod, invoice.Total);

            // Nếu checkout từ Cart (không phải Buy Now) → xóa cart DB
            if (!model.ProductId.HasValue && User.Identity!.IsAuthenticated)
            {
                int customerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var cartItemsInDb = _context.Carts.Where(c => c.CustomerId == customerId).ToList();
                if (cartItemsInDb.Any())
                {
                    _context.Carts.RemoveRange(cartItemsInDb);
                    _context.SaveChanges();
                    Log.Information("Cart cleared after checkout. CustomerId={CustomerId} ItemsRemoved={Count}", customerId, cartItemsInDb.Count);
                }
            }

            // Chuyển hướng thanh toán
            if (model.PaymentMethod == "Credit Card")
            {
                var stripeSessionUrl = CreateStripeCheckoutSession(invoice, cart);
                Log.Information("Redirecting to Stripe checkout. InvoiceId={InvoiceId}", invoice.InvoiceId);
                return Redirect(stripeSessionUrl);
            }
            else
            {
                Log.Information("Checkout completed without Stripe. InvoiceId={InvoiceId}", invoice.InvoiceId);
                return RedirectToAction("Success", new { invoiceId = invoice.InvoiceId });
            }
        }



        private string CreateStripeCheckoutSession(Invoice invoice, CartVM cart)
        {
            var domain = $"{Request.Scheme}://{Request.Host}";
            Log.Information("Creating Stripe checkout session. InvoiceId={InvoiceId} LineItemCount={LineItemCount} ShippingFee={ShippingFee} Discount={Discount}", invoice.InvoiceId, cart.Items.Count, invoice.ShippingFee, invoice.Discount);
            
            var lineItems = new List<SessionLineItemOptions>();
            List<SessionDiscountOptions> discounts = null;

            if (invoice.Discount > 0)
            {
                var couponOptions = new CouponCreateOptions()
                {
                    AmountOff = (long)invoice.Discount,
                    Currency = "vnd",
                    Duration = "once"
                };

                var couponService = new CouponService();
                var coupon = couponService.Create(couponOptions);

                if (coupon != null)
                {
                    discounts = new List<SessionDiscountOptions> 
                    {
                        new SessionDiscountOptions {Coupon = coupon.Id }
                    };
                }
            }

            // Add cart items
            foreach (var item in cart.Items)
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "vnd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Name,
                            Images = new List<string> { $"{domain}{item.ImageUrl}" }
                        },
                        UnitAmount = (long)item.Price,
                    },
                    Quantity = item.Quantity,
                });
            }

            // Add shipping fee
            if (invoice.ShippingFee > 0)
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "vnd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Shipping Fee",
                        },
                        UnitAmount = (long)invoice.ShippingFee,
                    },
                    Quantity = 1,
                });
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Discounts = discounts,
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = $"{domain}/Payment/StripeSuccess?session_id={{CHECKOUT_SESSION_ID}}&invoiceId={invoice.InvoiceId}",
                CancelUrl = $"{domain}/Payment/StripeCancel?invoiceId={invoice.InvoiceId}",
                CustomerEmail = invoice.Customer?.Email,
                Metadata = new Dictionary<string, string>
                {
                    { "invoice_id", invoice.InvoiceId.ToString() }
                }
            };

            var service = new SessionService();
            var session = service.Create(options);

            Log.Information("Stripe checkout session created. InvoiceId={InvoiceId} SessionId={SessionId}", invoice.InvoiceId, session.Id);

            return session.Url;
        }

        [HttpGet]
        public IActionResult StripeSuccess(string session_id, int invoiceId)
        {
            Log.Information("Stripe success callback received. InvoiceId={InvoiceId} SessionId={SessionId}", invoiceId, session_id);
            var invoice = _context.Invoices.Find(invoiceId);
            if (invoice != null)
            {
                invoice.Status = "Pending";
                invoice.PaymentMethod = "Stripe";
                _context.SaveChanges();
                Log.Information("Invoice updated after Stripe success. InvoiceId={InvoiceId} Status={Status}", invoiceId, invoice.Status);
            }
            else
            {
                Log.Warning("Stripe success callback ignored (invoice not found). InvoiceId={InvoiceId}", invoiceId);
            }

            return RedirectToAction("Success", new { invoiceId = invoiceId });
        }

        [HttpGet]
        public IActionResult StripeCancel(int invoiceId)
        {
            Log.Warning("Stripe cancel callback received. InvoiceId={InvoiceId}", invoiceId);
            var invoice = _context.Invoices
                .Include(i => i.Customer)
                .FirstOrDefault(i => i.InvoiceId == invoiceId);
            
            if (invoice != null)
            {
                invoice.Status = "Cancelled";
                
                // Restore stock quantities
                var invoiceDetails = _context.InvoiceDetails
                    .Where(id => id.InvoiceId == invoiceId)
                    .ToList();

                foreach (var detail in invoiceDetails)
                {
                    var product = _context.Products.Find(detail.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += detail.Quantity;
                        _context.Products.Update(product);
                        Log.Information("Stock restored after Stripe cancel. ProductId={ProductId} Quantity={Quantity} NewStock={NewStock}", product.ProductId, detail.Quantity, product.StockQuantity);
                    }
                }

                _context.SaveChanges();
                Log.Information("Invoice cancelled after Stripe cancel. InvoiceId={InvoiceId}", invoiceId);
            }

            if (invoice == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View("Cancelled", invoice);
        }

        private CartVM CreateCartForBuyNow(PaymentVM model)
        {
            var cart = new CartVM();

            if (model.ProductId.HasValue)
            {
                var product = _context.Products.Find(model.ProductId.Value);
                if (product == null)
                {
                    return cart;
                }

                cart.Items.Add(new CartItemVM
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    ImageUrl = product.ImageUrl1,
                    Price = product.SellingPrice,
                    Quantity = model.Quantity
                });
            }

            return cart;
        }

        private CartVM GetCartFromSession()
        {
            var cart = new CartVM();
            var cartJson = HttpContext.Session.GetString(CartSessionKey);
            if (!string.IsNullOrEmpty(cartJson))
            {
                try
                {
                    cart = JsonSerializer.Deserialize<CartVM>(cartJson) ?? new CartVM();
                }
                catch
                {
                    cart = new CartVM();
                }
            }
            return cart;
        }

        private CartVM ReconstructCart(PaymentVM model)
        {
            if (model.ProductId.HasValue)
            {
                return CreateCartForBuyNow(model);
            }
            else if (User.Identity!.IsAuthenticated)
            {
                int customerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                var cartItemsInDb = _context.Carts
                    .Include(c => c.Product)
                    .Where(c => c.CustomerId == customerId)
                    .ToList();

                var cart = new CartVM();
                foreach (var item in cartItemsInDb)
                {
                    cart.Items.Add(new CartItemVM
                    {
                        ProductId = item.ProductId,
                        Name = item.Product.Name,
                        ImageUrl = item.Product.ImageUrl1,
                        Price = item.Product.SellingPrice,
                        Quantity = item.Quantity
                    });
                }
                return cart;
            }

            return new CartVM();
        }

        [HttpPost]
        public IActionResult ValidateDiscountCode([FromBody] ValidateDiscountRequest request)
        {
            if (string.IsNullOrEmpty(request.DiscountCode))
            {
                return Json(new { success = false, message = "Please enter a discount code." });
            }

            if (request.Subtotal <= 0)
            {
                return Json(new { success = false, message = "Invalid subtotal." });
            }

            var authCustomerId = User.Identity?.IsAuthenticated == true
                ? User.FindFirstValue(ClaimTypes.NameIdentifier)
                : null;
            Log.Information("Validate discount code request. CustomerId={CustomerId} CouponCode={CouponCode} Subtotal={Subtotal} ShippingMethod={ShippingMethod}", authCustomerId, request.DiscountCode, request.Subtotal, request.ShippingMethod);

            var coupon = _context.Coupons
                .FirstOrDefault(c => c.CouponCode == request.DiscountCode 
                                    && c.StartDate <= DateTime.Now 
                                    && c.ExpireDate >= DateTime.Now 
                                    && c.Flag == 1);

            if (coupon == null)
            {
                Log.Warning("Validate discount code failed (invalid or expired). CustomerId={CustomerId} CouponCode={CouponCode}", authCustomerId, request.DiscountCode);
                return Json(new { success = false, message = "Invalid or expired discount code." });
            }

            var discount = request.Subtotal * coupon.DiscountRate;
            var setting = _context.Settings.FirstOrDefault();
            var baseShippingFee = setting?.ShippingFee ?? 500000m;
            var shippingFee = CalculateShippingFee(baseShippingFee, request.ShippingMethod);
            var total = request.Subtotal + shippingFee - discount;

            return Json(new 
            { 
                success = true, 
                discount = discount,
                discountRate = coupon.DiscountRate,
                total = total,
                shippingFee = shippingFee,
                message = $"Discount code applied! {coupon.DiscountRate * 100}% off."
            });
        }

        private decimal CalculateShippingFee(decimal baseShippingFee, string? shippingMethod)
        {
            if (string.Equals(shippingMethod, "Express", StringComparison.OrdinalIgnoreCase))
            {
                return baseShippingFee * 1.5m;
            }

            return baseShippingFee;
        }

        public IActionResult Success(int invoiceId)
        {
            Log.Information("Checkout success page opened. InvoiceId={InvoiceId}", invoiceId);
            var invoice = _context.Invoices
                .Include(i => i.Customer)
                .FirstOrDefault(i => i.InvoiceId == invoiceId);

            if (invoice == null)
            {
                Log.Warning("Checkout success page redirected (invoice not found). InvoiceId={InvoiceId}", invoiceId);
                return RedirectToAction("Index", "Home");
            }

            return View(invoice);
        }

    }


    public class ValidateDiscountRequest
    {
        public string DiscountCode { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public string ShippingMethod { get; set; } = "Standard";
    }
}

