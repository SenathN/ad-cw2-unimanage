using System.ComponentModel.DataAnnotations;

namespace UniManage.Models
{
    public class Enrollment
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        public int CourseId { get; set; }

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;

        public decimal? FinalGrade { get; set; }

        // Navigation properties
        public virtual ApplicationUser Student { get; set; } = null!;
        public virtual Course Course { get; set; } = null!;
    }

    public enum EnrollmentStatus
    {
        Active = 0,
        Completed = 1,
        Dropped = 2,
        Suspended = 3
    }
}
