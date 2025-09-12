using System.ComponentModel.DataAnnotations;

namespace ThesisNest.Models.ViewModels.Admin
{
    public class CreateStudentVm
    {
        [Required, MaxLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(120)]
        public string Email { get; set; } = string.Empty;

        [Phone, MaxLength(30)]
        public string? PhoneNumber { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(100)] public string? University { get; set; }
        [MaxLength(100)] public string? Department { get; set; }    // StudentProfile-এ এসব ফিল্ড আছে। :contentReference[oaicite:3]{index=3}
        [MaxLength(50)] public string? StudentId { get; set; }
        [MaxLength(50)] public string? Semester { get; set; }
    }
}
