using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ThesisNest.Models
{
    public class TeacherEducation
    {
        public int Id { get; set; }

        [Required] public string Degree { get; set; } = string.Empty;
        [Required] public string Institution { get; set; } = string.Empty;

        public string? BoardOrUniversity { get; set; }
        public string? FieldOfStudy { get; set; }
        public int? PassingYear { get; set; }
        public string? Result { get; set; }
        public string? Country { get; set; }

        [Required] public int TeacherProfileId { get; set; }

        [ValidateNever]                // ✅ validate করো না
        public TeacherProfile? TeacherProfile { get; set; }  // ✅ nullable

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
