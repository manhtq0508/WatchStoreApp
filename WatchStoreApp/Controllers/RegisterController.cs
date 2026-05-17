using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WatchStoreApp.Data;
using WatchStoreApp.Models;

namespace WatchStoreApp.Controllers
{
    public class RegisterController : Controller
    {
        private readonly MyAppContext _context;
        
        public RegisterController(MyAppContext context)
        {
            _context = context;
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
            // Validation
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(gender) || 
                string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(email) || 
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "All fields are required.";
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
                ViewBag.GenderList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Male", Text = "Male" },
                    new SelectListItem { Value = "Female", Text = "Female" },
                    new SelectListItem { Value = "Other", Text = "Other" }
                };
                return View();
            }

            // Create new customer
            var customer = new Customer
            {
                Name = name,
                Gender = gender,
                PhoneNumber = phoneNumber,
                DateOfBirth = dateOfBirth,
                Email = email,
                Password = password,
                IsAvailable = "Available"
            };

            _context.Customers.Add(customer);
            _context.SaveChanges();

            ViewBag.Success = "Registration successful! You can now sign in.";
            return RedirectToAction("Index", "Signin");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Please enter your email address.";
                return View();
            }

            // Check if customer exists
            var customer = _context.Customers.FirstOrDefault(c => c.Email == email);
            
            if (customer == null)
            {
                // Don't reveal if email exists for security
                ViewBag.Success = "If an account with that email exists, a password reset link has been sent.";
                return View();
            }

            var resetLink = Url.Action("ResetPassword", "Register",
                new { email = email }, Request.Scheme);

            SendEmail(email, resetLink);

            ViewBag.Success = "Password reset instructions have been sent to your email. Please check your inbox.";
            
            //ViewBag.Email = email;
            //ViewBag.ShowResetForm = true;
            
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            var customer = _context.Customers.FirstOrDefault(c => c.Email == email);
            if (customer == null)
            {
                ViewBag.Error = "Invalid reset link.";
                return RedirectToAction("ForgotPassword");
            }

            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string email, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "All fields are required.";
                ViewBag.Email = email;
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                ViewBag.Email = email;
                return View();
            }

            var customer = _context.Customers.FirstOrDefault(c => c.Email == email);
            if (customer == null)
            {
                ViewBag.Error = "Invalid reset link.";
                return RedirectToAction("ForgotPassword");
            }

            // Update password
            customer.Password = newPassword;
            _context.Customers.Update(customer);
            _context.SaveChanges();

            ViewBag.Success = "Password has been reset successfully. You can now sign in with your new password.";
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
                // Log this error so you know if login failed
                Console.WriteLine("Email Send Failed: " + ex.Message);
            }
        }
    }
}

