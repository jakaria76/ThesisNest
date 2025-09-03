using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ThesisNest.Models; // <-- Needed to see ApplicationUser

namespace ThesisNest.Models
{
    public class ThesisSubmission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? StudentId { get; set; }

        [ForeignKey("StudentId")]
        public ApplicationUser? Student { get; set; }

        [Required]
        public string? Title { get; set; }
        public string? Abstract { get; set; }
        public string? SupervisorName { get; set; }
        public string? Batch { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string? Version { get; set; }
        public string? Status { get; set; }
        public string? SupervisorComments { get; set; }

        public string? PlagiarismStatus { get; set; }

        public byte[]? FileData { get; set; }
        public string? FileContentType { get; set; }
        public string? FileName { get; set; }
    }
}
