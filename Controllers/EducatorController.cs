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
			var enrollments = await _context.Enrollments
				.Join(_context.Courses, e => e.CourseId, c => c.CourseId, (e, c) => new EnrollmentViewModel
				{
					StudentEmail = e.StudentEmail,
					CourseTitle = c.Title
				})
				.OrderByDescending(e => e.StudentEmail) // Assuming you want to get the latest based on email or another timestamp field
				.Take(5) // Fetch the latest 5 enrollments
				.ToListAsync();

			return View(enrollments);
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
				return RedirectToAction("MyCourses","Educator");
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
			return RedirectToAction("MyCourses","Educator");
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

	}
}
