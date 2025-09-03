using System.ComponentModel.DataAnnotations;

namespace ThesisNest.Models
{
    public class PhoneLoginViewModel
    {
        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }
    }
}
