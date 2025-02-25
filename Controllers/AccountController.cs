using E_Learning.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace E_Learning.Controllers
{
	public class AccountController : Controller
	{
		private readonly AppDbContext _context;

		public AccountController(AppDbContext context)
		{
			_context = context;
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
					
					return RedirectToAction("StudentHomePage","Student");
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

		// GET: Admin Home Page
		public IActionResult AdminHomePage()
		{
			return View();
		}

		
	}
}
