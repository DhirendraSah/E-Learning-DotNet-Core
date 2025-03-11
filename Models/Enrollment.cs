namespace E_Learning.Models
{
	public class Enrollment
	{
		public int EnrollmentId { get; set; }
		public int CourseId { get; set; }
		public string StudentEmail { get; set; }

		public Course Course { get; set; }
	}

}
