using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UniManage.Models;

namespace UniManage.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<CoursePrerequisite> CoursePrerequisites { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Role).HasConversion<string>().IsRequired().HasMaxLength(20);
            });

            // Configure Course
            builder.Entity<Course>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CourseCode).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.HasOne(e => e.Lecturer)
                    .WithMany()
                    .HasForeignKey(e => e.LecturerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Enrollment
            builder.Entity<Enrollment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Student)
                    .WithMany(u => u.Enrollments)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Enrollments)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.FinalGrade).HasPrecision(5, 2);
            });

            // Configure Assignment
            builder.Entity<Assignment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Assignments)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Lecturer)
                    .WithMany(u => u.CreatedAssignments)
                    .HasForeignKey(e => e.LecturerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Submission
            builder.Entity<Submission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).HasMaxLength(5000);
                entity.Property(e => e.Feedback).HasMaxLength(1000);
                entity.HasOne(e => e.Assignment)
                    .WithMany(a => a.Submissions)
                    .HasForeignKey(e => e.AssignmentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Student)
                    .WithMany(u => u.Submissions)
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.Grade).HasPrecision(5, 2);
            });

            // Configure Message
            builder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Subject).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Content).IsRequired().HasMaxLength(5000);
                entity.HasOne(e => e.Sender)
                    .WithMany(u => u.SentMessages)
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Receiver)
                    .WithMany(u => u.ReceivedMessages)
                    .HasForeignKey(e => e.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure CoursePrerequisite (many-to-many relationship)
            builder.Entity<CoursePrerequisite>(entity =>
            {
                entity.HasKey(e => new { e.CourseId, e.PrerequisiteCourseId });
                entity.HasOne(e => e.Course)
                    .WithMany(c => c.Prerequisites)
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.PrerequisiteCourse)
                    .WithMany(c => c.IsPrerequisiteFor)
                    .HasForeignKey(e => e.PrerequisiteCourseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
