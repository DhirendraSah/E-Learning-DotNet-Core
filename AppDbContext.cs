using Microsoft.EntityFrameworkCore;

namespace E_Learning.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Lecture> Lectures { get; set; }

        public DbSet<Enrollment> Enrollments { get; set; }

        public DbSet<LectureProgress> LectureProgresses { get; set; }

        public DbSet<EnrollmentViewModel> TotalEnrollments { get; set; }

        public DbSet<EducatorDashboardViewModel> DashBoardData { get; set; }

        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        public DbSet<CourseLike> CourseLikes { get; set; }

        public DbSet<CourseFeedback> CourseFeedbacks { get; set; }

        public DbSet<Quiz> Quizzes { get; set; }

        public DbSet<QuizQuestion> QuizQuestions { get; set; }

        public DbSet<Note> Notes { get; set; }


    }
}