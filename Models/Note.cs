using System.ComponentModel.DataAnnotations;

namespace E_Learning.Models
{
	public class Note
	{
		public int NoteId { get; set; }

		
		public string Title { get; set; }

		public string Content { get; set; }

		
		public string FilePath { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.Now;
	}
}
