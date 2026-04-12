using System.ComponentModel.DataAnnotations;

namespace UniManage.Models.ViewModels
{
    public class CreateAssignmentViewModel
    {
        [Required]
        public int CourseId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Assignment Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }

        [Required]
        [Range(1, 1000, ErrorMessage = "Max points must be between 1 and 1000")]
        [Display(Name = "Maximum Points")]
        public decimal MaxPoints { get; set; } = 100;
    }
}
