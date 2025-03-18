namespace E_Learning.Models
{
	public class QuizQuestion
	{
		public int QuizQuestionId { get; set; }
		public int QuizId { get; set; }
		public string QuestionText { get; set; }
		public string OptionA { get; set; }
		public string OptionB { get; set; }
		public string OptionC { get; set; }
		public string OptionD { get; set; }
		public string CorrectAnswer { get; set; }  // Store like "A", "B", "C", "D"

		public Quiz Quiz { get; set; }
	}

}
