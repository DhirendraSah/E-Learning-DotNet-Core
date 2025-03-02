using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Threading.Tasks;
using E_Learning.Models;
using Microsoft.AspNetCore.Http;

namespace E_Learning.Controllers
{
	public class PaymentController : Controller
	{
		private readonly AppDbContext _context;
		private readonly IConfiguration _configuration;

		public PaymentController(AppDbContext context, IConfiguration configuration)
		{
			_context = context;
			_configuration = configuration;
		}

		public async Task<IActionResult> ProcessPayment(int courseId)
		{
			// Fetch course details from database
			var course = await _context.Courses.FindAsync(courseId);
			if (course == null)
			{
				return NotFound();
			}

			var userEmail = HttpContext.Session.GetString("UserEmail");
			if (string.IsNullOrEmpty(userEmail))
			{
				TempData["Error"] = "Please log in to proceed with payment.";
				return RedirectToAction("StudentHomePage", "Student");
			}

			// Base domain URL
			var domain = $"{Request.Scheme}://{Request.Host}";

			var options = new SessionCreateOptions
			{
				PaymentMethodTypes = new List<string> { "card" },
				LineItems = new List<SessionLineItemOptions>
				{
					new SessionLineItemOptions
					{
						PriceData = new SessionLineItemPriceDataOptions
						{
							Currency = "usd",
							ProductData = new SessionLineItemPriceDataProductDataOptions
							{
								Name = course.Title
							},
							UnitAmount = (long)(course.Price * 100) // Convert price to cents
                        },
						Quantity = 1
					}
				},
				Mode = "payment",
				SuccessUrl = $"{domain}/Payment/PaymentSuccess?sessionId={{CHECKOUT_SESSION_ID}}&courseId={courseId}",
				CancelUrl = $"{domain}/Student/ViewCourse/{courseId}"
			};

			// Create Stripe session
			var service = new SessionService();
			Session session = await service.CreateAsync(options);

			// Redirect to Stripe checkout
			return Redirect(session.Url);
		}

		public async Task<IActionResult> PaymentSuccess(string sessionId, int courseId)
		{
			var userEmail = HttpContext.Session.GetString("UserEmail");
			if (string.IsNullOrEmpty(userEmail))
			{
				TempData["Error"] = "Please log in to continue.";
				return RedirectToAction("StudentHomePage", "Student");
			}

			// Verify the payment with Stripe
			var service = new SessionService();
			var session = await service.GetAsync(sessionId);

			if (session.PaymentStatus == "paid")
			{
				// Enroll the student in the course
				var enrollment = new Enrollment
				{
					CourseId = courseId,
					StudentEmail = userEmail
				};

				_context.Enrollments.Add(enrollment);
				await _context.SaveChangesAsync();

				TempData["Success"] = "Payment successful! You are now enrolled in the course.";
				return RedirectToAction("ViewCourse", "Student", new { id = courseId });
			}

			TempData["Error"] = "Payment verification failed.";
			return RedirectToAction("StudentHomePage", "Student");
		}
	}
}
