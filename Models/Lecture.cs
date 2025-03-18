using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace E_Learning.Models
{
	public class Lecture
	{
		[Key]
		public int LectureId { get; set; }

		[Required]
		public string LectureTitle { get; set; }

		public string VideoPath { get; set; }

		[ForeignKey("Course")]
		public int CourseId { get; set; }
		public Course Course { get; set; }
	}
}
