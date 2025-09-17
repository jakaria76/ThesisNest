using System.ComponentModel.DataAnnotations;

namespace ThesisNest.Models
{
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        public string? Email { get; set; }
    }

    public class VerifyOtpViewModel
    {
        [Required, EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string? Otp { get; set; }
    }

    public class ResetPasswordOtpViewModel
    {
        [Required, EmailAddress]
        public string? Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [Required, DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }

        [Required]
        public string Token { get; set; }

    }
}
