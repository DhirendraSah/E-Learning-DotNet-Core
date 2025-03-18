using System.ComponentModel.DataAnnotations;

namespace E_Learning.Models
{
	public class User
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public string Username { get; set; }

		[Required]
		[EmailAddress]
		public string Email { get; set; }

		[Required]
		public string Password { get; set; }

		public string Role { get; set; } 
	}
}
