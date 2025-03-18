using E_Learning.Models;

public class LectureProgress
{
	public int LectureProgressId { get; set; }
	public int CourseId { get; set; }  // Link to the course
	public int LectureId { get; set; } // Track specific lecture
	public string StudentEmail { get; set; } // Track progress per student
	public bool IsCompleted { get; set; } = false; // Completion status

	public Lecture Lecture { get; set; }
}
