using System.ComponentModel.DataAnnotations;

namespace UniManage.Models.ViewModels
{
    public class CreateCourseViewModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Course Code")]
        public string CourseCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Course Title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1, 10, ErrorMessage = "Credits must be between 1 and 10")]
        [Display(Name = "Credits")]
        public int Credits { get; set; }

        [Required]
        [Range(1, 200, ErrorMessage = "Maximum enrollment must be between 1 and 200")]
        [Display(Name = "Maximum Enrollment")]
        public int MaxEnrollment { get; set; } = 50;

        [Required]
        [Display(Name = "Assign Lecturer")]
        public string LecturerId { get; set; } = string.Empty;
    }
}
