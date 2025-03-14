﻿using E_Learning.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace E_Learning.Controllers
{
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;

        public StudentController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> StudentHomePage(string searchQuery)
        {
            ViewBag.UserEmail = HttpContext.Session.GetString("UserEmail");

            var courses = await _context.Courses
                .Where(c => string.IsNullOrEmpty(searchQuery) || c.Title.Contains(searchQuery))
                .ToListAsync();

            ViewBag.SearchQuery = searchQuery; // Preserve search input in UI

            return View(courses);
        }

        public async Task<IActionResult> ViewCourse(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Lectures)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            var userEmail = HttpContext.Session.GetString("UserEmail");
            bool isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseId == id && e.StudentEmail == userEmail);

            ViewBag.IsEnrolled = isEnrolled;
            ViewBag.UserEmail = userEmail;

            // Automatically enroll if the course is free
            if (course.Price == 0 && !isEnrolled && !string.IsNullOrEmpty(userEmail))
            {
                var enrollment = new Enrollment
                {
                    CourseId = id,
                    StudentEmail = userEmail
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                ViewBag.IsEnrolled = true; // Update UI
            }

            return View(course);
        }

        public async Task<IActionResult> EnrollInCourse(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                return NotFound();
            }

            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["Error"] = "Please log in to enroll in a course.";
                return RedirectToAction("StudentHomePage");
            }

            bool isAlreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseId == courseId && e.StudentEmail == userEmail);

            if (isAlreadyEnrolled)
            {
                TempData["Message"] = "You are already enrolled in this course.";
                return RedirectToAction("ViewCourse", new { id = courseId });
            }

            if (course.Price == 0)
            {
                // Auto-enroll for free courses
                var enrollment = new Enrollment
                {
                    CourseId = courseId,
                    StudentEmail = userEmail
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                TempData["Success"] = "You have successfully enrolled in this free course!";
                return RedirectToAction("ViewCourse", new { id = courseId });
            }
            else
            {
                // Redirect to Stripe payment
                return RedirectToAction("ProcessPayment", "Payment", new { courseId = courseId });
            }
        }
        public async Task<IActionResult> MyEnrollments()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["Error"] = "Please log in to view your enrollments.";
                return RedirectToAction("StudentHomePage");
            }

            var enrollments = await _context.Enrollments
                .Where(e => e.StudentEmail == userEmail)
                .Include(e => e.Course)
                .ToListAsync();

            return View(enrollments);
        }
        [HttpPost]
        public async Task<IActionResult> MarkLectureCompleted([FromBody] LectureProgressesDto progressDto)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized();
            }

            var progress = await _context.LectureProgresses
                .FirstOrDefaultAsync(lp => lp.CourseId == progressDto.CourseId && lp.LectureId == progressDto.LectureId && lp.StudentEmail == userEmail);

            if (progress == null)
            {
                _context.LectureProgresses.Add(new LectureProgress
                {
                    CourseId = progressDto.CourseId,
                    LectureId = progressDto.LectureId,
                    StudentEmail = userEmail,
                    IsCompleted = true
                });
            }
            else
            {
                progress.IsCompleted = true;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        public class LectureProgressesDto
        {
            public int CourseId { get; set; }
            public int LectureId { get; set; }
        }


        public async Task<IActionResult> MyProgress()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            // 🔥 FIX: Only fetch enrollments of the logged-in student
            var enrollments = await _context.Enrollments
                .Where(e => e.StudentEmail == userEmail) // ✅ Only this student's enrollments
                .Include(e => e.Course)
                .ToListAsync();

            var progressData = new List<object>();

            foreach (var enrollment in enrollments)
            {
                var totalLectures = await _context.Lectures
                    .Where(l => l.CourseId == enrollment.CourseId)
                    .CountAsync();

                var completedLectures = await _context.LectureProgresses
                    .Where(lp => lp.CourseId == enrollment.CourseId && lp.StudentEmail == userEmail && lp.IsCompleted)
                    .CountAsync();

                var progressPercentage = totalLectures > 0 ? (completedLectures * 100 / totalLectures) : 0;
                bool isCompleted = completedLectures == totalLectures;

                progressData.Add(new
                {
                    Course = enrollment.Course,
                    Progress = progressPercentage,
                    IsCompleted = isCompleted
                });
            }

            return View(progressData);
        }


        public async Task<IActionResult> GetCertificate(int courseId)
        {
            QuestPDF.Settings.License = LicenseType.Community; // Required for QuestPDF

            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized();
            }

            var completedLectures = await _context.LectureProgresses
                .Where(lp => lp.CourseId == courseId && lp.StudentEmail == userEmail && lp.IsCompleted)
                .CountAsync();
            var totalLectures = await _context.Lectures.Where(l => l.CourseId == courseId).CountAsync();

            if (completedLectures != totalLectures)
            {
                return BadRequest("Course not completed yet.");
            }

            var student = await _context.Users.FirstOrDefaultAsync(s => s.Email == userEmail);
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId);

            string studentName = student?.Username ?? userEmail.Split('@')[0];
            string completionDate = DateTime.UtcNow.ToString("MMMM dd, yyyy");

            string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png");
            string signaturePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "signature.png");

            if (!System.IO.File.Exists(logoPath) || !System.IO.File.Exists(signaturePath))
            {
                return BadRequest("One or more required images not found.");
            }

            byte[] logoBytes = await System.IO.File.ReadAllBytesAsync(logoPath);
            byte[] signatureBytes = await System.IO.File.ReadAllBytesAsync(signaturePath);

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.PageColor(Colors.White);

                    page.Content().Padding(20).Column(column =>
                    {
                        // ✅ Fixed: Wrap Image in Width & Height
                        column.Item().AlignCenter().Width(150).Height(50).Image(logoBytes);

                        // Title
                        column.Item().PaddingTop(20)
                            .AlignCenter().Text("Certificate of Completion")
                            .FontSize(32).Bold().FontColor(Colors.Blue.Medium);

                        // Description
                        column.Item().PaddingVertical(10).AlignCenter().Text("This is to certify that")
                            .FontSize(16);

                        // Student Name
                        column.Item().AlignCenter().Text(studentName)
                            .FontSize(26).Bold().FontColor(Colors.Black);

                        // Course Name
                        column.Item().AlignCenter().Text("has successfully completed the course")
                            .FontSize(16);
                        column.Item().AlignCenter().Text(course.Title)
                            .FontSize(22).Bold().FontColor(Colors.Black);

                        // Completion Date
                        column.Item().AlignCenter().Text($"on {completionDate}")
                            .FontSize(16);

                        // ✅ Fixed: Signature Image
                        column.Item().PaddingVertical(20).AlignCenter().Width(150).Height(50).Image(signatureBytes);

                        // Admin Label
                        column.Item().AlignCenter().Text("Administrator").FontSize(14);
                    });

                    // Footer
                    page.Footer().AlignCenter().Text("E-Learning Platform | www.yourwebsite.com")
                        .FontSize(12).FontColor(Colors.Grey.Darken2);
                });
            });

            var stream = new MemoryStream();
            pdf.GeneratePdf(stream);
            stream.Position = 0;

            return File(stream, "application/pdf", "Certificate.pdf");
        }

        [HttpPost]
        public async Task<IActionResult> LikeCourse(int courseId)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("You need to log in to like a course.");

            var existingLike = await _context.CourseLikes
                .FirstOrDefaultAsync(l => l.CourseId == courseId && l.StudentEmail == userEmail);

            if (existingLike == null)
            {
                _context.CourseLikes.Add(new CourseLike
                {
                    CourseId = courseId,
                    StudentEmail = userEmail
                });
                await _context.SaveChangesAsync();
            }

            int likeCount = await _context.CourseLikes
                .CountAsync(l => l.CourseId == courseId);

            return Json(new { likes = likeCount });
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(int courseId, string feedbackText)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Please log in to submit feedback.");

            var feedback = new CourseFeedback
            {
                CourseId = courseId,
                StudentEmail = userEmail,
                FeedbackText = feedbackText
            };

            _context.CourseFeedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetCourseLikes(int courseId)
        {
            int likeCount = await _context.CourseLikes
                .CountAsync(l => l.CourseId == courseId);

            return Json(new { likes = likeCount });
        }


        // Use ActionName to ensure there's no duplicate registration error
        public async Task<IActionResult> Quizzes()
        {
            // Just load all quizzes (no CourseId filter)
            var quizzes = await _context.Quizzes.ToListAsync();
            return View(quizzes);  // Pass to the view
        }

        public async Task<IActionResult> StartQuiz(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.QuizId == id);

            if (quiz == null)
            {
                TempData["Error"] = "Quiz not found.";
                return RedirectToAction("Quizzes");
            }

            return View(quiz);  // Pass quiz with questions to the view
        }

        [HttpPost]
        public async Task<IActionResult> SubmitQuiz(int QuizId, List<string> Answers)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.QuizId == QuizId);

            if (quiz == null)
            {
                TempData["Error"] = "Quiz not found.";
                return RedirectToAction("Quizzes");
            }

            int correctCount = 0;

            for (int i = 0; i < quiz.Questions.Count; i++)
            {
                if (i < Answers.Count && quiz.Questions[i].CorrectAnswer == Answers[i])
                {
                    correctCount++;
                }
            }

            int totalQuestions = quiz.Questions.Count;

            // Set data for the result page
            ViewBag.QuizTitle = quiz.Title;
            ViewBag.Score = correctCount;
            ViewBag.TotalQuestions = totalQuestions;

            // Show the result view
            return View("QuizResult");
        }

        public async Task<IActionResult> QuizResult(int quizId, int score, int totalQuestions)
        {
            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz == null)
            {
                TempData["Error"] = "Quiz not found.";
                return RedirectToAction("Quizzes");
            }

            ViewBag.QuizTitle = quiz.Title;
            ViewBag.Score = score;
            ViewBag.TotalQuestions = totalQuestions;

            return View();
        }

        public async Task<IActionResult> Notes()
        {
            var notes = await _context.Notes.ToListAsync(); // Load all notes
            return View(notes);
        }

        // ✅ Download File
        public IActionResult DownloadFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return NotFound();

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath.TrimStart('/'));

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);
            var fileName = Path.GetFileName(fullPath);

            return File(fileBytes, "application/octet-stream", fileName);
        }

    }
}
