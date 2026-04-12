using System.ComponentModel.DataAnnotations;

namespace UniManage.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string CourseCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int Credits { get; set; }

        [Required]
        public int MaxEnrollment { get; set; } = 50;

        public int CurrentEnrollment { get; set; } = 0;

        [Required]
        public string LecturerId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ApplicationUser Lecturer { get; set; } = null!;
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public virtual ICollection<CoursePrerequisite> Prerequisites { get; set; } = new List<CoursePrerequisite>();
        public virtual ICollection<CoursePrerequisite> IsPrerequisiteFor { get; set; } = new List<CoursePrerequisite>();
    }

    public class CoursePrerequisite
    {
        public int CourseId { get; set; }
        public int PrerequisiteCourseId { get; set; }

        public virtual Course Course { get; set; } = null!;
        public virtual Course PrerequisiteCourse { get; set; } = null!;
    }
}
