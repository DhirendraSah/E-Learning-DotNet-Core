using E_Learning.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Learning.Controllers
{
	public class NotesController : Controller
	{
		private readonly AppDbContext _context;
		private readonly IWebHostEnvironment _hostEnvironment;

		public NotesController(AppDbContext context, IWebHostEnvironment hostEnvironment)
		{
			_context = context;
			_hostEnvironment = hostEnvironment;
		}

		// ✅ Show Create Form
		public IActionResult Index()
		{
			return View(new Note()); // Pass a new Note object
		}

		// ✅ Create Note
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(Note note, IFormFile file)
		{
			if (ModelState.IsValid)
			{
				if (file != null && file.Length > 0)
				{
					// Save file to wwwroot/uploads folder
					string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads");
					Directory.CreateDirectory(uploadsFolder);

					string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
					string filePath = Path.Combine(uploadsFolder, uniqueFileName);

					using (var fileStream = new FileStream(filePath, FileMode.Create))
					{
						await file.CopyToAsync(fileStream);
					}

					note.FilePath = "/uploads/" + uniqueFileName;
				}

				note.CreatedAt = DateTime.Now;

				// Save to database
				_context.Add(note);
				await _context.SaveChangesAsync();

				// ✅ Redirect to Index after saving
				return RedirectToAction(nameof(ManageNotes));
			}

			return View("Index", note);
		}
		public async Task<IActionResult> ManageNotes()
		{
			var notes = await _context.Notes.ToListAsync();
			return View(notes);
		}
		public async Task<IActionResult> Edit(int id)
		{
			var note = await _context.Notes.FindAsync(id);
			if (note == null) return NotFound();

			return View(note);
		}

		// ✅ Handle Edit Form Submission
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, Note note, IFormFile file)
		{
			if (id != note.NoteId) return NotFound();

			if (ModelState.IsValid)
			{
				var existingNote = await _context.Notes.FindAsync(id);
				if (existingNote == null) return NotFound();

				// ✅ Handle file upload
				if (file != null && file.Length > 0)
				{
					// Delete the old file if it exists
					if (!string.IsNullOrEmpty(existingNote.FilePath))
					{
						var oldFilePath = Path.Combine(_hostEnvironment.WebRootPath, existingNote.FilePath.TrimStart('/'));
						if (System.IO.File.Exists(oldFilePath))
						{
							System.IO.File.Delete(oldFilePath);
						}
					}

					// Upload new file
					string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads");
					Directory.CreateDirectory(uploadsFolder);

					string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
					string filePath = Path.Combine(uploadsFolder, uniqueFileName);

					using (var fileStream = new FileStream(filePath, FileMode.Create))
					{
						await file.CopyToAsync(fileStream);
					}

					note.FilePath = "/uploads/" + uniqueFileName;
				}
				else
				{
					note.FilePath = existingNote.FilePath;
				}

				existingNote.Title = note.Title;
				existingNote.Content = note.Content;
				existingNote.FilePath = note.FilePath;

				_context.Update(existingNote);
				await _context.SaveChangesAsync();

				return RedirectToAction(nameof(ManageNotes));
			}

			return View(note);
		}

		// ============================
		// ✅ DELETE FUNCTIONALITY
		// ============================
		public async Task<IActionResult> Delete(int id)
		{
			var note = await _context.Notes.FindAsync(id);
			if (note == null) return NotFound();

			// Delete file from server
			if (!string.IsNullOrEmpty(note.FilePath))
			{
				var filePath = Path.Combine(_hostEnvironment.WebRootPath, note.FilePath.TrimStart('/'));
				if (System.IO.File.Exists(filePath))
				{
					System.IO.File.Delete(filePath);
				}
			}

			_context.Notes.Remove(note);
			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(ManageNotes));
		}
	}
}
