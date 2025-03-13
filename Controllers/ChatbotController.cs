using Microsoft.AspNetCore.Mvc;

namespace E_Learning.Controllers
{
	[Route("api/chatbot")]
	[ApiController]
	public class ChatbotController : ControllerBase
	{
		[HttpPost("ask")]
		public IActionResult Ask([FromBody] ChatRequest request)
		{
			var response = GenerateChatbotResponse(request.Message);
			return Ok(new { response });
		}

		private string GenerateChatbotResponse(string message)
		{
			if (string.IsNullOrWhiteSpace(message))
				return "Please ask me something!";

			message = message.ToLower();

			if (message.Contains("course"))
				return "You can browse all available courses here. Use the search bar for specific topics.";
			else if (message.Contains("enroll"))
				return "To enroll, click 'View Course' and then 'Enroll'. Let me know if you need help!";
			else if (message.Contains("price") || message.Contains("free"))
				return "Some courses are free while others have a price. You can see it listed with each course.";
			else
				return "I'm here to help with course-related questions. Ask me about enrollment, courses, or pricing!";
		}

		public class ChatRequest
		{
			public string Message { get; set; }
		}
	}
}
