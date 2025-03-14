using E_Learning.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Learning.Controllers
{
	public class EducatorController : Controller
	{
		private readonly AppDbContext _context;
		private readonly IWebHostEnvironment _webHostEnvironment;

		public EducatorController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
		{
			_context = context;
			_webHostEnvironment = webHostEnvironment;
		}

		public async Task<IActionResult> HomePage()
		{
			var dashboardViewModel = new EducatorDashboardViewModel
			{
				TotalEnrollments = await _context.Enrollments.CountAsync(),
				TotalCourses = await _context.Courses.CountAsync(),
				RecentEnrollments = await _context.Enrollments
					.Join(_context.Courses, e => e.CourseId, c => c.CourseId, (e, c) => new Enrollment
					{
						StudentEmail = e.StudentEmail,
						CourseId = e.CourseId,
						Course = c // Assuming Course has a Title property
					})
					.OrderByDescending(e => e.StudentEmail) // Use a timestamp field like EnrollmentDate if available
					.Take(5) // Fetch the latest 5 enrollments
					.ToListAsync()
			};

			return View(dashboardViewModel);
		}



		public IActionResult AddCourses()
		{
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> AddCourses(Course course, IFormFile thumbnail, List<IFormFile> lectureVideos, List<string> lectureTitles)
		{
			if (!ModelState.IsValid)
			{
				TempData["Error"] = "Invalid data. Please check your input.";
				return View(course);
			}

			if (course.IsFree)
			{
				course.Price = 0; // Ensure price is set to 0 for free courses
			}

			if (thumbnail != null && thumbnail.Length > 0)
			{
				string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/thumbnails");
				Directory.CreateDirectory(uploadsFolder);
				string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(thumbnail.FileName);
				string filePath = Path.Combine(uploadsFolder, uniqueFileName);

				using var stream = new FileStream(filePath, FileMode.Create);
				await thumbnail.CopyToAsync(stream);

				course.ThumbnailPath = "/uploads/thumbnails/" + uniqueFileName;
			}

			_context.Courses.Add(course);
			await _context.SaveChangesAsync();

			for (int i = 0; i < lectureVideos.Count; i++)
			{
				if (lectureVideos[i] != null && lectureVideos[i].Length > 0)
				{
					string videoFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/lectures");
					Directory.CreateDirectory(videoFolder);
					string uniqueVideoName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(lectureVideos[i].FileName);
					string videoPath = Path.Combine(videoFolder, uniqueVideoName);

					using var stream = new FileStream(videoPath, FileMode.Create);
					await lectureVideos[i].CopyToAsync(stream);

					_context.Lectures.Add(new Lecture
					{
						LectureTitle = lectureTitles[i],
						VideoPath = "/uploads/lectures/" + uniqueVideoName,
						CourseId = course.CourseId
					});
				}
			}

			await _context.SaveChangesAsync();
			TempData["Success"] = "Course added successfully!";
			return RedirectToAction("MyCourses");
		}


		public async Task<IActionResult> MyCourses()
		{
			var courses = await _context.Courses.Include(c => c.Lectures).ToListAsync();
			return View(courses);
		}

		[HttpPost]
		public async Task<IActionResult> DeleteCourse(int id)
		{
			var course = await _context.Courses.Include(c => c.Lectures).FirstOrDefaultAsync(c => c.CourseId == id);
			if (course == null)
			{
				TempData["Error"] = "Course not found!";
				return RedirectToAction("MyCourses", "Educator");
			}

			foreach (var lecture in course.Lectures)
			{
				if (!string.IsNullOrEmpty(lecture.VideoPath))
				{
					string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, lecture.VideoPath.TrimStart('/'));
					if (System.IO.File.Exists(fullPath))
					{
						System.IO.File.Delete(fullPath);
					}
				}
			}

			if (!string.IsNullOrEmpty(course.ThumbnailPath))
			{
				string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, course.ThumbnailPath.TrimStart('/'));
				if (System.IO.File.Exists(fullPath))
				{
					System.IO.File.Delete(fullPath);
				}
			}

			_context.Lectures.RemoveRange(course.Lectures);
			_context.Courses.Remove(course);
			await _context.SaveChangesAsync();

			TempData["Success"] = "Course deleted successfully!";
			return RedirectToAction("MyCourses", "Educator");
		}

		[HttpPost]
		public async Task<IActionResult> AddLecture(int CourseId, string LectureTitle, IFormFile lectureVideo)
		{
			var course = await _context.Courses.Include(c => c.Lectures).FirstOrDefaultAsync(c => c.CourseId == CourseId);
			if (course == null)
			{
				return Json(new { success = false, message = "Course not found!" });
			}

			if (lectureVideo == null || lectureVideo.Length == 0)
			{
				return Json(new { success = false, message = "Please upload a valid video file." });
			}

			string videoFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/lectures");
			Directory.CreateDirectory(videoFolder);
			string uniqueVideoName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(lectureVideo.FileName);
			string videoPath = Path.Combine(videoFolder, uniqueVideoName);

			using var stream = new FileStream(videoPath, FileMode.Create);
			await lectureVideo.CopyToAsync(stream);

			Lecture lecture = new Lecture
			{
				LectureTitle = LectureTitle,
				VideoPath = "/uploads/lectures/" + uniqueVideoName,
				CourseId = CourseId
			};

			_context.Lectures.Add(lecture);
			await _context.SaveChangesAsync();

			int updatedLectureCount = _context.Lectures.Count(l => l.CourseId == CourseId);

			return Json(new { success = true, lectureCount = updatedLectureCount });
		}
		public async Task<IActionResult> StudentEnrolled()
		{
			var enrollments = await _context.Enrollments
				.Join(_context.Courses, e => e.CourseId, c => c.CourseId, (e, c) => new EnrollmentViewModel
				{
					StudentEmail = e.StudentEmail,
					CourseTitle = c.Title
				})
				.ToListAsync();

			return View(enrollments);
		}
		// Show all feedback from all courses
		public async Task<IActionResult> CourseFeedback()
		{
			var feedbacks = await _context.CourseFeedbacks
				.OrderByDescending(f => f.SubmittedAt)
				.ToListAsync();

			return View(feedbacks); // This loads CourseFeedback.cshtml
		}

		//For Quizzes

		// Show page to create quiz
		public IActionResult AddQuiz()
		{
			return View();
		}

		// POST: Save quiz and questions
		[HttpPost]
		public async Task<IActionResult> AddQuiz(Quiz quiz, List<string> QuestionTexts,
												 List<string> OptionAs, List<string> OptionBs,
												 List<string> OptionCs, List<string> OptionDs,
												 List<string> CorrectAnswers)
		{
			if (string.IsNullOrEmpty(quiz.Subject) || string.IsNullOrEmpty(quiz.Level) || string.IsNullOrEmpty(quiz.Title))
			{
				TempData["Error"] = "Please fill all quiz details.";
				return View(quiz);
			}

			if (QuestionTexts == null || QuestionTexts.Count == 0)
			{
				TempData["Error"] = "Please add at least one question.";
				return View(quiz);
			}

			// Save Quiz
			_context.Quizzes.Add(quiz);
			await _context.SaveChangesAsync();

			// Save Questions
			for (int i = 0; i < QuestionTexts.Count; i++)
			{
				if (!string.IsNullOrWhiteSpace(QuestionTexts[i]))
				{
					_context.QuizQuestions.Add(new QuizQuestion
					{
						QuizId = quiz.QuizId,
						QuestionText = QuestionTexts[i],
						OptionA = OptionAs[i],
						OptionB = OptionBs[i],
						OptionC = OptionCs[i],
						OptionD = OptionDs[i],
						CorrectAnswer = CorrectAnswers[i]
					});
				}
			}

			await _context.SaveChangesAsync();

			TempData["Success"] = "Quiz and questions saved successfully!";
			return RedirectToAction("QuizManagement");
		}

		// View all quizzes
		public async Task<IActionResult> QuizList()
		{
			var quizzes = await _context.Quizzes.ToListAsync();
			return View(quizzes);
		}

		// View quiz details and questions
		public async Task<IActionResult> QuizDetails(int id)
		{
			var quiz = await _context.Quizzes
				.Include(q => q.Questions)
				.FirstOrDefaultAsync(q => q.QuizId == id);

			if (quiz == null)
			{
				return NotFound();
			}

			return View(quiz);
		}

		// Educator Quiz Management - Show All Quizzes with Edit & Delete
		public async Task<IActionResult> QuizManagement()
		{
			var quizzes = await _context.Quizzes.ToListAsync();
			return View(quizzes);
		}

		public async Task<IActionResult> EditQuiz(int id)
		{
			var quiz = await _context.Quizzes
				.Include(q => q.Questions)
				.FirstOrDefaultAsync(q => q.QuizId == id);

			if (quiz == null)
			{
				return NotFound();
			}

			return View(quiz);
		}

		[HttpPost]
		public async Task<IActionResult> EditQuiz(Quiz quiz, List<string> QuestionTexts, List<string> OptionAs,
												  List<string> OptionBs, List<string> OptionCs,
												  List<string> OptionDs, List<string> CorrectAnswers)
		{
			if (string.IsNullOrEmpty(quiz.Title) || string.IsNullOrEmpty(quiz.Subject) || string.IsNullOrEmpty(quiz.Level))
			{
				TempData["Error"] = "Please fill all quiz details.";
				return View(quiz);
			}

			var existingQuiz = await _context.Quizzes
				.Include(q => q.Questions)
				.FirstOrDefaultAsync(q => q.QuizId == quiz.QuizId);

			if (existingQuiz == null)
			{
				return NotFound();
			}

			existingQuiz.Title = quiz.Title;
			existingQuiz.Subject = quiz.Subject;
			existingQuiz.Level = quiz.Level;

			// Clear old questions
			_context.QuizQuestions.RemoveRange(existingQuiz.Questions);
			await _context.SaveChangesAsync();

			// Add updated questions
			for (int i = 0; i < QuestionTexts.Count; i++)
			{
				if (!string.IsNullOrWhiteSpace(QuestionTexts[i]))
				{
					_context.QuizQuestions.Add(new QuizQuestion
					{
						QuizId = quiz.QuizId,
						QuestionText = QuestionTexts[i],
						OptionA = OptionAs[i],
						OptionB = OptionBs[i],
						OptionC = OptionCs[i],
						OptionD = OptionDs[i],
						CorrectAnswer = CorrectAnswers[i]
					});
				}
			}

			await _context.SaveChangesAsync();

			TempData["Success"] = "Quiz updated successfully!";
			return RedirectToAction("QuizManagement");
		}

		[HttpGet]
		public async Task<IActionResult> DeleteQuiz(int id)
		{
			var quiz = await _context.Quizzes
				.Include(q => q.Questions)
				.FirstOrDefaultAsync(q => q.QuizId == id);

			if (quiz == null)
			{
				return NotFound();
			}

			_context.QuizQuestions.RemoveRange(quiz.Questions);
			_context.Quizzes.Remove(quiz);

			await _context.SaveChangesAsync();

			TempData["Success"] = "Quiz deleted successfully!";
			return RedirectToAction("QuizManagement");
		}



	}
}
