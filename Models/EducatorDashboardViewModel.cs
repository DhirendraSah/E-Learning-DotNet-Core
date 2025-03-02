using System.ComponentModel.DataAnnotations;

namespace E_Learning.Models
{
	public class EducatorDashboardViewModel
	{
		[Key]
		public int TotalEnrollments { get; set; }
		public int TotalCourses { get; set; }
		public decimal TotalEarnings { get; set; }
		public List<Enrollment> RecentEnrollments { get; set; }
	}
}
