namespace E_Learning.Models
{
	public class CourseFeedback
	{
		public int Id { get; set; }
		public int CourseId { get; set; }
		public string StudentEmail { get; set; }
		public string FeedbackText { get; set; }
		public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
	}
}
