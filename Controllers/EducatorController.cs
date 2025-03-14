using E_Learning.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Facebook;

namespace E_Learning.Controllers
{
	public class AccountController : Controller
	{
		private readonly AppDbContext _context;
		private readonly IConfiguration _configuration;

		public AccountController(AppDbContext context, IConfiguration configuration)
		{
			_context = context;
			_configuration = configuration;
		}


		// GET: Login Page
		[HttpGet]
		public IActionResult Login()
		{
			return View();
		}

		// POST: Login Action
		[HttpPost]
		public IActionResult Login(string email, string password)
		{
			// Hardcoded admin credentials for EducatorHomePage
			if (email == "admin@gmail.com" && password == "Admin")
			{
				HttpContext.Session.SetString("UserEmail", email);
				return RedirectToAction("HomePage", "Educator");
			}

			// Check user in the database
			var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);

			if (user != null)
			{
				HttpContext.Session.SetString("UserEmail", email);

				// Redirect based on role
				if (user.Role == "Admin")
				{
					return RedirectToAction("AdminHomePage");
				}
				else
				{

					return RedirectToAction("StudentHomePage", "Student");
				}
			}

			// If no match, display an error message
			ViewBag.Message = "Invalid Email or Password";
			return View();
		}

		// GET: Register Page
		[HttpGet]
		public IActionResult Register()
		{
			return View();
		}

		// POST: Registration Action
		[HttpPost]
		public IActionResult Register(string username, string email, string password, string confirmPassword)
		{
			// Validate passwords
			if (password != confirmPassword)
			{
				ViewBag.Message = "Passwords do not match";
				return View();
			}

			// Check if email is already registered
			if (_context.Users.Any(u => u.Email == email))
			{
				ViewBag.Message = "Email is already registered";
				return View();
			}

			// Create and save a new user
			var newUser = new User
			{
				Username = username,
				Email = email,
				Password = password,
				Role = "Student" // Default role is Student
			};

			_context.Users.Add(newUser);
			_context.SaveChanges();

			return RedirectToAction("Login");
		}

		// GET: Google Login Redirect
		[HttpGet]
		public IActionResult GoogleLogin()
		{
			var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
			return Challenge(properties, GoogleDefaults.AuthenticationScheme);
		}

		// Callback from Google
		[HttpGet]
		public async Task<IActionResult> GoogleResponse()
		{
			var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

			if (result?.Principal == null)
			{
				return RedirectToAction("Login");
			}

			var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
			var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
			var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

			if (email == null)
			{
				ViewBag.Message = "Error retrieving your Google account information.";
				return View("Login");
			}

			// Check if user exists
			var user = _context.Users.FirstOrDefault(u => u.Email == email);

			if (user == null)
			{
				// Register new user (default role as Student)
				user = new User
				{
					Username = name,
					Email = email,
					Password = null, // No password needed for Google login
					Role = "Student"
				};

				_context.Users.Add(user);
				_context.SaveChanges();
			}

			// Set session and redirect to homepage
			HttpContext.Session.SetString("UserEmail", email);

			if (user.Role == "Admin")
			{
				return RedirectToAction("AdminHomePage");
			}
			else
			{
				return RedirectToAction("StudentHomePage", "Student");
			}
		}

		// GET: Admin Home Page
		public IActionResult AdminHomePage()
		{
			return View();
		}

		[HttpGet]
		public IActionResult ForgotPassword()
		{
			return View();
		}

		[HttpPost]
		public IActionResult ForgotPassword(string email)
		{
			var user = _context.Users.FirstOrDefault(u => u.Email == email);
			if (user == null)
			{
				ViewBag.Message = "No account found with that email.";
				return View();
			}

			// Generate token
			var token = GenerateToken();

			// Save token
			var resetToken = new PasswordResetToken
			{
				Email = email,
				Token = token,
				ExpiryDate = DateTime.UtcNow.AddHours(1)  // Token valid for 1 hour
			};

			_context.PasswordResetTokens.Add(resetToken);
			try
			{
				_context.SaveChanges();
			}
			catch (DbUpdateException ex)
			{
				// Log the error (inner exception usually holds the key detail)
				Console.WriteLine($"DbUpdateException: {ex.Message}");

				if (ex.InnerException != null)
				{
					Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
				}

				throw; // Re-throw to let it bubble up if needed
			}


			// Send email
			SendResetEmail(email, token);

			ViewBag.Message = "Password reset link has been sent to your email.";
			return View();
		}

		[HttpGet]
		public IActionResult ResetPassword(string token)
		{
			var resetToken = _context.PasswordResetTokens.FirstOrDefault(t => t.Token == token && t.ExpiryDate > DateTime.UtcNow);

			if (resetToken == null)
			{
				return View("TokenExpired");
			}

			ViewBag.Email = resetToken.Email;
			ViewBag.Token = token;
			return View();
		}

		[HttpPost]
		public IActionResult ResetPassword(string token, string newPassword, string confirmPassword)
		{
			if (newPassword != confirmPassword)
			{
				ViewBag.Message = "Passwords do not match.";
				ViewBag.Token = token;
				return View();
			}

			var resetToken = _context.PasswordResetTokens.FirstOrDefault(t => t.Token == token && t.ExpiryDate > DateTime.UtcNow);

			if (resetToken == null)
			{
				return View("TokenExpired");
			}

			var user = _context.Users.FirstOrDefault(u => u.Email == resetToken.Email);
			if (user == null)
			{
				ViewBag.Message = "User not found.";
				return View();
			}

			user.Password = newPassword;
			_context.PasswordResetTokens.Remove(resetToken);  // Invalidate the token
			_context.SaveChanges();

			return RedirectToAction("Login");
		}

		private string GenerateToken()
		{
			byte[] tokenBytes = new byte[32]; // 256 bits
			RandomNumberGenerator.Fill(tokenBytes);
			return Convert.ToBase64String(tokenBytes)
				.Replace("+", "")  // Remove URL-unsafe characters
				.Replace("/", "")
				.Replace("=", "");  // Remove padding for URL safety
		}


		private void SendResetEmail(string email, string token)
		{
			string resetLink = Url.Action("ResetPassword", "Account", new { token }, Request.Scheme);

			var fromAddress = new MailAddress(_configuration["EmailSettings:SenderEmail"], "E-Learning Platform");
			var toAddress = new MailAddress(email);
			const string subject = "Password Reset Request";
			string body = $"Click the link below to reset your password:\n\n{resetLink}\n\nThis link will expire in 1 hour.";

			var smtp = new SmtpClient
			{
				Host = _configuration["EmailSettings:SmtpServer"],
				Port = int.Parse(_configuration["EmailSettings:SmtpPort"]),
				EnableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"]),
				Credentials = new NetworkCredential(
					_configuration["EmailSettings:SenderEmail"],
					_configuration["EmailSettings:SenderPassword"]
				)
			};

			using (var message = new MailMessage(fromAddress, toAddress)
			{
				Subject = subject,
				Body = body
			})
			{
				smtp.Send(message);
			}
		}

		[HttpGet]
		public IActionResult FacebookLogin()
		{
			var properties = new AuthenticationProperties { RedirectUri = Url.Action("FacebookResponse") };
			return Challenge(properties, FacebookDefaults.AuthenticationScheme);
		}

		[HttpGet]
		public async Task<IActionResult> FacebookResponse()
		{
			var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

			if (result?.Principal == null)
			{
				return RedirectToAction("Login");
			}

			var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
			var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
			var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

			if (email == null)
			{
				ViewBag.Message = "Error retrieving your Facebook account information.";
				return View("Login");
			}

			var user = _context.Users.FirstOrDefault(u => u.Email == email);

			if (user == null)
			{
				user = new User
				{
					Username = name,
					Email = email,
					Password = null, // No password for external login
					Role = "Student"
				};

				_context.Users.Add(user);
				_context.SaveChanges();
			}

			HttpContext.Session.SetString("UserEmail", email);

			if (user.Role == "Admin")
			{
				return RedirectToAction("AdminHomePage");
			}
			else
			{
				return RedirectToAction("StudentHomePage", "Student");
			}
		}



	}
}
