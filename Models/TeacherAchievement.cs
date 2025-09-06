using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ThesisNest.Models
{
    public class TeacherAchievement
    {
        public int Id { get; set; }

        [Required] public string Title { get; set; } = string.Empty;

        public string? Issuer { get; set; }
        public DateTime? IssuedOn { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }

        [Required] public int TeacherProfileId { get; set; }

        [ValidateNever]
        public TeacherProfile? TeacherProfile { get; set; }  // ✅

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
