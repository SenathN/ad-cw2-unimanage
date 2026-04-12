using System.ComponentModel.DataAnnotations;

namespace UniManage.Models
{
    public class Assignment
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int CourseId { get; set; }

        [Required]
        public string LecturerId { get; set; } = string.Empty;

        [Required]
        public DateTime DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public decimal MaxPoints { get; set; } = 100;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual Course Course { get; set; } = null!;
        public virtual ApplicationUser Lecturer { get; set; } = null!;
        public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    }

    public class Submission
    {
        public int Id { get; set; }

        [Required]
        public int AssignmentId { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [StringLength(5000)]
        public string Content { get; set; } = string.Empty;

        public string? AttachmentPath { get; set; }

        public decimal? Grade { get; set; }

        [StringLength(1000)]
        public string? Feedback { get; set; } = string.Empty;

        public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

        // Navigation properties
        public virtual Assignment Assignment { get; set; } = null!;
        public virtual ApplicationUser Student { get; set; } = null!;
    }

    public enum SubmissionStatus
    {
        NotSubmitted = 0,
        Submitted = 1,
        Graded = 2,
        Late = 3
    }
}
