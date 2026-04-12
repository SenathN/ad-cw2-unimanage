using System.ComponentModel.DataAnnotations;

namespace UniManage.Models.ViewModels
{
    public class SubmitAssignmentViewModel
    {
        [Required]
        public int AssignmentId { get; set; }

        [Required]
        [StringLength(5000)]
        [Display(Name = "Assignment Content")]
        public string Content { get; set; } = string.Empty;

        // Display-only properties
        public string AssignmentTitle { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public decimal MaxPoints { get; set; }
        public bool IsLate { get; set; }
    }
}
