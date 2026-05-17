using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WatchStoreApp.Data;
using WatchStoreApp.Models;
using WatchStoreApp.ViewModel.Invoice;

namespace WatchStoreApp.Controllers
{
    [Authorize(Roles = "Admin, Employee")]
    public class InvoiceController : Controller
    {
        private readonly MyAppContext _context;
        public InvoiceController(MyAppContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var invoices = _context.Invoices
                .Include(i => i.Customer)
                .Select(i => new ViewListInvoiceVM
                {
                    InvoiceId = i.InvoiceId,
                    CustomerId = i.CustomerId,
                    CustomerName = i.Customer.Name,
                    Date = i.Date,
                    Total = i.Total,
                    Status = i.Status
                })
                .OrderByDescending(i => i.Date)
                .ToList();
            return View(invoices);
        }

        public IActionResult Details(int id)
        {
            var invoice = _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Coupon)
                .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefault(i => i.InvoiceId == id);
            
            if (invoice == null)
            {
                return NotFound();
            }
            return View(invoice);
        }

        [HttpGet]
        public IActionResult ChangeStatus(int id)
        {
            //var invoice = _context.Invoices
            //    .Include(i => i.Customer)
            //    .FirstOrDefault(i => i.InvoiceId == id);
            var invoice = _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Coupon)
                .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefault(i => i.InvoiceId == id);

            if (invoice == null)
            {
                return NotFound();
            }

            ViewBag.InvoiceId = invoice.InvoiceId;
            ViewBag.CurrentStatus = invoice.Status;
            ViewBag.CustomerName = invoice.Customer.Name;
            ViewBag.Date = invoice.Date;
            ViewBag.Total = invoice.Total;

            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ChangeStatus(int id, string status)
        {
            var invoice = _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefault(i => i.InvoiceId == id);

            if (invoice == null)
            {
                return NotFound();
            }

            // Validate status
            var validStatuses = new[] { "Processing", "Delivered", "Cancelled" };
            if (!validStatuses.Contains(status))
            {
                ModelState.AddModelError("Status", "Invalid status selected.");
                ViewBag.InvoiceId = invoice.InvoiceId;
                ViewBag.CurrentStatus = invoice.Status;
                return View();
            }

            if (status == "Cancelled")
            {
                var details = _context.InvoiceDetails
                    .Include(d => d.Product)
                    .Where(d => d.InvoiceId == invoice.InvoiceId)
                    .ToList();

                foreach (var d in details)
                {
                    d.Product.StockQuantity += d.Quantity;
                }

                _context.SaveChanges();
            }


            invoice.Status = status;
            _context.SaveChanges();

            // 4. SEND EMAIL NOTIFICATION
            try
            {
                string subject = $"Order Update #{invoice.InvoiceId} - {status}";
                string messageBody = GenerateOrderEmailBody(invoice, status);

                // Send to the customer's email
                SendEmail(invoice.Customer.Email, subject, messageBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not send email: " + ex.Message);
            }

            return RedirectToAction("Index");
        }

        public IActionResult Search(string search_invoice)
        {
            if (string.IsNullOrEmpty(search_invoice))
            {
                return RedirectToAction("Index");
            }

            var invoices = _context.Invoices
                .Include(i => i.Customer)
                .Where(i => i.InvoiceId.ToString().Contains(search_invoice) ||
                           i.Customer.Name.ToLower().Contains(search_invoice.ToLower()))
                .Select(i => new ViewListInvoiceVM
                {
                    InvoiceId = i.InvoiceId,
                    CustomerId = i.CustomerId,
                    CustomerName = i.Customer.Name,
                    Date = i.Date,
                    Total = i.Total,
                    Status = i.Status
                })
                .OrderByDescending(i => i.Date)
                .ToList();

            return View("Index", invoices);
        }

        public static void SendEmail(string toEmail, string subject, string body)
        {
            string fromEmail = "ngocchaudl75@gmail.com";
            string fromPassword = "xkzh mbti rmrv cqxt";

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(fromEmail, fromPassword),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);

            try
            {
                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email Error: " + ex.Message);
            }
        }

        private string GenerateOrderEmailBody(Invoice invoice, string status)
        {
            // 1. Determine status color & message
            string statusMessage = "";
            string statusColor = "#005844"; // Theme Green

            if (status == "Cancelled")
            {
                statusMessage = "Your order has been cancelled.";
                statusColor = "#dc2626"; // Red
            }
            else if (status == "Delivered")
            {
                statusMessage = "Your order has been delivered successfully!";
            }
            else
            {
                statusMessage = $"Your order status has been updated to: {status}";
            }

            // 2. Generate Product Rows
            string productRows = "";
            foreach (var item in invoice.InvoiceDetails)
            {
                productRows += $@"
        <tr>
            <td style='padding: 12px; border-bottom: 1px solid #e5e7eb;'>
                <div style='font-weight: bold; color: #111827;'>{item.Product?.Name ?? "Product"}</div>
                <div style='font-size: 12px; color: #6b7280;'>Qty: {item.Quantity}</div>
            </td>
            <td style='padding: 12px; border-bottom: 1px solid #e5e7eb; text-align: right; color: #111827;'>
                {item.Price:N0}₫
            </td>
        </tr>";
            }

            // 3. Prepare Financial Rows (Subtotal, Shipping, Discount)
            string discountRow = "";
            if (invoice.Discount > 0)
            {
                string couponText = invoice.Coupon != null ? $"({invoice.Coupon.CouponCode})" : "";
                discountRow = $@"
        <tr>
            <td style='padding: 6px 0; color: #6b7280;'>Discount {couponText}</td>
            <td style='padding: 6px 0; text-align: right; color: #dc2626;'>-{invoice.Discount:N0}₫</td>
        </tr>";
            }

            // 4. Return Full HTML
            return $@"
    <!DOCTYPE html>
    <html>
    <head>
        <style>
            body {{ font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; line-height: 1.6; color: #374151; }}
            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
            .header {{ background-color: #005844; color: white; padding: 24px; text-align: center; border-radius: 8px 8px 0 0; }}
            .content {{ border: 1px solid #e5e7eb; border-top: none; padding: 24px; border-radius: 0 0 8px 8px; background-color: #ffffff; }}
            .section-title {{ color: #005844; font-size: 16px; font-weight: bold; text-transform: uppercase; margin-top: 28px; margin-bottom: 12px; border-bottom: 2px solid #005844; padding-bottom: 5px; letter-spacing: 0.05em; }}
            .info-grid {{ width: 100%; margin-bottom: 20px; }}
            .info-label {{ color: #6b7280; font-size: 13px; text-transform: uppercase; letter-spacing: 0.05em; margin-bottom: 2px; }}
            .info-value {{ color: #111827; font-weight: 600; margin-bottom: 16px; font-size: 15px; }}
        </style>
    </head>
    <body style='background-color: #f3f4f6; padding: 20px;'>
        <div class='container'>
            <div class='header'>
                <h1 style='margin:0; font-size: 24px; letter-spacing: 1px;'>ORDER UPDATE</h1>
                <p style='margin: 5px 0 0 0; opacity: 0.9; font-size: 16px;'>#{invoice.InvoiceId}</p>
            </div>

            <div class='content'>
                <p style='margin-top: 0;'>Dear <strong>{invoice.Customer.Name}</strong>,</p>
                <p style='color: {statusColor}; font-weight: bold; font-size: 16px; margin-bottom: 24px;'>{statusMessage}</p>

                <div class='section-title'>Order Information</div>
                <table class='info-grid' border='0' cellspacing='0' cellpadding='0'>
                    <tr>
                        <td width='50%' valign='top'>
                            <div class='info-label'>Customer Info</div>
                            <div class='info-value'>
                                {invoice.Customer.Name}<br>
                                {invoice.Customer.Email}<br>
                                {invoice.Customer.PhoneNumber ?? "No Phone"}
                            </div>
                        </td>
                        <td width='50%' valign='top'>
                            <div class='info-label'>Shipping To</div>
                            <div class='info-value'>{invoice.Address}</div>
                        </td>
                    </tr>
                    <tr>
                        <td width='50%' valign='top'>
                            <div class='info-label'>Payment Method</div>
                            <div class='info-value'>{invoice.PaymentMethod}</div>
                        </td>
                        <td width='50%' valign='top'>
                            <div class='info-label'>Shipping Method</div>
                            <div class='info-value'>{invoice.ShippingMethod}</div>
                        </td>
                    </tr>
                    <tr>
                        <td colspan='2'>
                            <div class='info-label'>Note</div>
                            <div class='info-value' style='font-style: italic; color: #4b5563;'>
                                ""{(!string.IsNullOrEmpty(invoice.ShippingNote) ? invoice.ShippingNote : "None")}""
                            </div>
                        </td>
                    </tr>
                </table>

                <div class='section-title'>Order Summary</div>
                <table style='width: 100%; border-collapse: collapse;'>
                    <thead>
                        <tr style='background-color: #f9fafb; border-bottom: 2px solid #e5e7eb;'>
                            <th style='padding: 12px; text-align: left; color: #4b5563; font-size: 13px; text-transform: uppercase;'>Item</th>
                            <th style='padding: 12px; text-align: right; color: #4b5563; font-size: 13px; text-transform: uppercase;'>Total</th>
                        </tr>
                    </thead>
                    <tbody>
                        {productRows}
                    </tbody>
                    <tfoot>
                        <tr>
                            <td colspan='2' style='padding-top: 20px; border-top: 2px solid #e5e7eb;'>
                                <table style='width: 100%;'>
                                    <tr>
                                        <td style='padding: 6px 0; color: #6b7280;'>Subtotal</td>
                                        <td style='padding: 6px 0; text-align: right; color: #111827;'>{invoice.Subtotal:N0}₫</td>
                                    </tr>
                                    <tr>
                                        <td style='padding: 6px 0; color: #6b7280;'>Shipping Fee</td>
                                        <td style='padding: 6px 0; text-align: right; color: #111827;'>{invoice.ShippingFee:N0}₫</td>
                                    </tr>
                                    {discountRow}
                                    <tr>
                                        <td style='padding: 12px 0; font-weight: bold; font-size: 18px; color: #111827;'>Total</td>
                                        <td style='padding: 12px 0; text-align: right; font-weight: bold; font-size: 18px; color: #005844;'>
                                            {invoice.Total:N0}₫
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                    </tfoot>
                </table>

                <div style='margin-top: 30px; padding-top: 20px; border-top: 1px dashed #e5e7eb; text-align: center; color: #9ca3af; font-size: 12px;'>
                    <p style='margin-bottom: 4px;'>Thank you for choosing Timeless Elegance.</p>
                    <p style='margin-top: 0;'>{DateTime.Now.Year} Watch Store App. All rights reserved.</p>
                </div>
            </div>
        </div>
    </body>
    </html>";
        }
    }
}

