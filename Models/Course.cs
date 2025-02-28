using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Learning.Models
{
	public class Course
	{
		[Key]
		public int CourseId { get; set; }

		[Required]
		public string Title { get; set; }

		[Required]
		public string Description { get; set; }

		public string ThumbnailPath { get; set; }

		[Required]
		public DateTime Date { get; set; } = DateTime.Now;

		[Column(TypeName = "decimal(18,2)")] // ✅ Fix Precision Issue
		public decimal Price { get; set; }

		public bool IsFree { get; set; }

		public List<Lecture> Lectures { get; set; } = new List<Lecture>();
	}
}
