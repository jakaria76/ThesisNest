using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ThesisNest.Models
{
    public class TeacherPublication
    {
        public int Id { get; set; }

        [Required] public string Title { get; set; } = string.Empty;

        public string? VenueType { get; set; }
        public string? VenueName { get; set; }
        public int? Year { get; set; }
        public string? Volume { get; set; }
        public string? Issue { get; set; }
        public string? Pages { get; set; }
        public string? DOI { get; set; }
        public string? Url { get; set; }
        public string? CoAuthors { get; set; }
        public string? Abstract { get; set; }

        [Required] public int TeacherProfileId { get; set; }

        [ValidateNever]
        public TeacherProfile? TeacherProfile { get; set; }  // ✅

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
