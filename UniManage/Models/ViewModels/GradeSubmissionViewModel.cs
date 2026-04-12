using System.ComponentModel.DataAnnotations;

namespace UniManage.Models.ViewModels
{
    public class GradeSubmissionViewModel
    {
        [Required]
        public int SubmissionId { get; set; }

        [Required]
        [Range(0, 1000, ErrorMessage = "Grade must be between 0 and 1000")]
        [Display(Name = "Grade")]
        public decimal? Grade { get; set; }

        [StringLength(1000)]
        [Display(Name = "Feedback")]
        public string Feedback { get; set; } = string.Empty;

        // Display-only properties
        public string StudentName { get; set; } = string.Empty;
        public string AssignmentTitle { get; set; } = string.Empty;
        public string SubmittedContent { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public decimal MaxPoints { get; set; }
    }
}
