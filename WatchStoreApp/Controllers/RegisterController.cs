using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Serilog;
using WatchStoreApp.Data;
using WatchStoreApp.Models;
using WatchStoreApp.Utils;

namespace WatchStoreApp.Controllers
{
    public class RegisterController : Controller
    {
        private readonly MyAppContext _context;
        private readonly RedisContext _redis;
        
        public RegisterController(MyAppContext context, RedisContext redis)
        {
            _context = context;
            _redis = redis;
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

        public IActionResult Index()
        {
            ViewBag.GenderList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Male", Text = "Male" },
                new SelectListItem { Value = "Female", Text = "Female" },
                new SelectListItem { Value = "Other", Text = "Other" }
            };
            return View();
        }

        [HttpPost]
        public IActionResult Index(string name, string gender, string phoneNumber, DateTime dateOfBirth, 
            string email, string password, string confirmPassword)
        {
            Log.Information("Customer registration attempt. Email={Email}", MaskEmail(email));
            // Validation
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(gender) || 
                string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(email) || 
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "All fields are required.";
                Log.Warning("Customer registration validation failed (missing fields). Email={Email}", MaskEmail(email));
                ViewBag.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Male", Text = "Male" },
                    new SelectListItem { Value = "Female", Text = "Female" },
                    new SelectListItem { Value = "Other", Text = "Other" }
                };
                return View();
            }

            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                Log.Warning("Customer registration validation failed (password mismatch). Email={Email}", MaskEmail(email));
                ViewBag.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Male", Text = "Male" },
                    new SelectListItem { Value = "Female", Text = "Female" },
                    new SelectListItem { Value = "Other", Text = "Other" }
                };
                return View();
            }

            // Check if email already exists
            if (_context.Customers.Any(c => c.Email == email))
            {
                ViewBag.Error = "Email already exists. Please use a different email.";
                Log.Warning("Customer registration failed (email exists). Email={Email}", MaskEmail(email));
                ViewBag.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Male", Text = "Male" },
                    new SelectListItem { Value = "Female", Text = "Female" },
                    new SelectListItem { Value = "Other", Text = "Other" }
                };
                return View();
            }

            var hashedPassword = PasswordHelper.HashPassword(password);
            
            // Create new customer
            var customer = new Customer
            {
                Name = name,
                Gender = gender,
                PhoneNumber = phoneNumber,
                DateOfBirth = dateOfBirth,
                Email = email,
                Password = hashedPassword,
                IsAvailable = "Available"
            };

            _context.Customers.Add(customer);
            _context.SaveChanges();

            ViewBag.Success = "Registration successful! You can now sign in.";
            Log.Information("Customer registration succeeded. CustomerId={CustomerId}", customer.CustomerId);
            return RedirectToAction("Index", "Signin");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Please enter your email address.";
                Log.Warning("Forgot password validation failed (empty email).");
                return View();
            }

            Log.Information("Forgot password requested. Email={Email}", MaskEmail(email));

            // Check if customer exists
            var customer = _context.Customers.FirstOrDefault(c => c.Email == email);
            
            if (customer == null)
            {
                // Don't reveal if email exists for security
                ViewBag.Success = "If an account with that email exists, a password reset link has been sent.";
                Log.Warning("Forgot password requested for non-existing email. Email={Email}", MaskEmail(email));
                return View();
            }

            var token = RandomString.GenerateSecureString(32);
            await _redis.SetStringAsync($"password-reset:{token}", email, TimeSpan.FromMinutes(5));
            
            var resetLink = Url.Action("ResetPassword", "Register",
                new { email = email, token = token }, Request.Scheme);

            if (string.IsNullOrWhiteSpace(resetLink))
            {
                Log.Error("Failed to generate password reset link. CustomerId={CustomerId} Email={Email}", customer.CustomerId, MaskEmail(email));
                ViewBag.Error = "Failed to generate password reset link. Please try again later.";
                return View();
            }

            try
            {
                SendEmail(email, resetLink);
                Log.Information("Password reset email sent. CustomerId={CustomerId} Email={Email}", customer.CustomerId, MaskEmail(email));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send password reset email. CustomerId={CustomerId} Email={Email}", customer.CustomerId, MaskEmail(email));
                ViewBag.Error = "Failed to send password reset email. Please try again later.";
                return View();
            }

            ViewBag.Success = "Password reset instructions have been sent to your email. Please check your inbox.";
            
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) ||  string.IsNullOrEmpty(token))
            {
                Log.Warning("Password reset link opened with missing parameters.");
                TempData["Error"] = "Invalid or expired reset link.";
                return RedirectToAction("ForgotPassword");
            }

            Log.Information("Password reset link opened. Email={Email}", MaskEmail(email));

            var resetEmail = await _redis.GetStringAsync($"password-reset:{token}");
            if (string.IsNullOrEmpty(resetEmail) || resetEmail != email)
            {
                TempData["Error"] = "Invalid or expired reset link.";
                Log.Warning("Password reset link invalid or expired. Email={Email}", MaskEmail(email));
                return RedirectToAction("ForgotPassword");
            }
            
            var customer = _context.Customers.FirstOrDefault(c => c.Email == email);
            if (customer == null)
            {
                TempData["Error"] = "Invalid or expired reset link.";
                Log.Warning("Password reset link rejected (customer not found). Email={Email}", MaskEmail(email));
                return RedirectToAction("ForgotPassword");
            }

            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string token, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "All fields are required.";
                ViewBag.Email = email;
                ViewBag.Token = token;
                Log.Warning("Password reset validation failed (missing fields). Email={Email}", MaskEmail(email));
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                ViewBag.Email = email;
                ViewBag.Token = token;
                Log.Warning("Password reset validation failed (password mismatch). Email={Email}", MaskEmail(email));
                return View();
            }

            var resetEmail = await _redis.GetStringAsync($"password-reset:{token}");
            if (string.IsNullOrEmpty(resetEmail) || resetEmail != email)
            {
                TempData["Error"] = "Invalid or expired reset link.";
                Log.Warning("Password reset failed (token invalid or expired). Email={Email}", MaskEmail(email));
                return RedirectToAction("ForgotPassword");
            }

            var customer = _context.Customers.FirstOrDefault(c => c.Email == email);
            if (customer == null)
            {
                TempData["Error"] = "Invalid or expired reset link.";
                Log.Warning("Password reset failed (customer not found). Email={Email}", MaskEmail(email));
                return RedirectToAction("ForgotPassword");
            }

            // Update password
            customer.Password = PasswordHelper.HashPassword(newPassword);
            _context.Customers.Update(customer);
            _context.SaveChanges();

            // Invalidate token (one-time use)
            await _redis.DeleteKeyAsync($"password-reset:{token}");

            TempData["Success"] = "Password has been reset successfully. You can now sign in with your new password.";
            Log.Information("Password reset succeeded. CustomerId={CustomerId} Email={Email}", customer.CustomerId, MaskEmail(email));
            return RedirectToAction("Index", "Signin");
        }

        private void SendEmail(string toEmail, string resetLink)
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
                Subject = "Reset Your Password",
                Body = $"<p>Click the link below to reset your password:</p><a href='{resetLink}'>{resetLink}</a>",
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);

            try
            {
                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Email send failed.");
                throw;
            }
        }
    }
}

