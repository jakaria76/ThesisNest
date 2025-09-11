using System.ComponentModel.DataAnnotations;

namespace ThesisNest.Models.ViewModels.Admin
{
    public class CreateTeacherVm
    {
        [Required, MaxLength(120)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(120)]
        public string Email { get; set; } = string.Empty;

        [Phone, MaxLength(30)]
        public string? Phone { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(80)] public string? Designation { get; set; }
        [MaxLength(80)] public string? Department { get; set; }   // তোমার TeacherProfile-এ Department string আছে। :contentReference[oaicite:2]{index=2}
        [MaxLength(120)] public string? OfficeLocation { get; set; }
    }
}
