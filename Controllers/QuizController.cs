using E_Learning.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Learning.Controllers
{
	public class QuizController : Controller
	{
		private readonly AppDbContext _context;
		private readonly IWebHostEnvironment _webHostEnvironment;

		public QuizController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
		{
			_context = context;
			_webHostEnvironment = webHostEnvironment;
		}

		// Use ActionName to ensure there's no duplicate registration error
		[ActionName("Quizzes")]
		public async Task<IActionResult> QuizzesWithSession()
		{
			int? courseId = HttpContext.Session.GetInt32("SelectedCourseId");
			if (courseId == null)
			{
				return RedirectToAction("StudentHomePage", "Student");
			}

			var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);

			if (course == null)
			{
				return NotFound();
			}

			ViewBag.CourseName = course.Title;

			var quizzes = await _context.Quizzes
				.Where(q => q.CourseId == courseId)
				.ToListAsync();

			return View("Quizzes", quizzes);
		}


		// Start quiz page
		public async Task<IActionResult> StartQuiz(int quizId)
		{
			var quiz = await _context.Quizzes
				.Include(q => q.Questions)
				.FirstOrDefaultAsync(q => q.QuizId == quizId);

			if (quiz == null)
			{
				return NotFound();
			}

			return View(quiz);  // This expects you to create StartQuiz.cshtml
		}

		// Handle quiz submission and calculate score
		[HttpPost]
		public async Task<IActionResult> SubmitQuiz(int quizId, List<string> Answers)
		{
			var quiz = await _context.Quizzes
				.Include(q => q.Questions)
				.FirstOrDefaultAsync(q => q.QuizId == quizId);

			if (quiz == null)
			{
				return NotFound();
			}

			if (Answers == null || Answers.Count != quiz.Questions.Count)
			{
				TempData["Error"] = "Please answer all questions.";
				return RedirectToAction("StartQuiz", new { quizId = quizId });
			}

			int score = 0;

			for (int i = 0; i < quiz.Questions.Count; i++)
			{
				if (quiz.Questions[i].CorrectAnswer.Equals(Answers[i], StringComparison.OrdinalIgnoreCase))
				{
					score++;
				}
			}

			ViewBag.TotalQuestions = quiz.Questions.Count;
			ViewBag.Score = score;
			ViewBag.QuizTitle = quiz.Title;

			return View("QuizResult");
		}
	}
}
