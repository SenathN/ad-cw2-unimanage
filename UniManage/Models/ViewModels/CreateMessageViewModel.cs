using System.ComponentModel.DataAnnotations;

namespace UniManage.Models.ViewModels
{
    public class CreateMessageViewModel
    {
        [Required]
        [Display(Name = "Recipient")]
        public string ReceiverId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(5000)]
        [Display(Name = "Message")]
        public string Content { get; set; } = string.Empty;
    }
}
