using System.ComponentModel.DataAnnotations;

namespace ThesisNest.Models
{
    public class OtpVerifyViewModel
    {
        [Required]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string? PhoneNumber { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 4, ErrorMessage = "OTP must be 4-6 digits")]
        public string? Otp { get; set; }
    }
}
