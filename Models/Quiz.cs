using System.ComponentModel.DataAnnotations.Schema;

namespace E_Learning.Models
{
	public class Quiz
	{
		public int QuizId { get; set; }
		public string Title { get; set; }
		public string Subject { get; set; }
		public string Level { get; set; }  // Example: Easy, Medium, Hard
		public List<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
		[NotMapped]
		public int? CourseId { get; internal set; }
	}

}
