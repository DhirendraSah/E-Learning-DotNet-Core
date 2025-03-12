using System.ComponentModel.DataAnnotations;

namespace E_Learning.Models
{
	public class EnrollmentViewModel
	{
		[Key]
		public string StudentEmail { get; set; }

		public string CourseTitle { get; set; }
	}

}
