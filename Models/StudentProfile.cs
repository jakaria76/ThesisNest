using System;
using System.ComponentModel.DataAnnotations;

namespace ThesisNest.Models
{
    public class StudentProfile
    {
        [Key]
        public int Id { get; set; }

        // Basic Info
        [Required]
        public string FullName { get; set; } = string.Empty;

        // (Legacy/optional) if you still keep URL:
        public string? ProfilePictureUrl { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }

        // Academic Info
        public string? University { get; set; }
        public string? Department { get; set; }
        public string? StudentId { get; set; }
        public string? Semester { get; set; }
        public double? GPA { get; set; }

        // Thesis Info
        public string? ThesisTitle { get; set; }
        public string? Supervisor { get; set; }
        public string? ThesisStatus { get; set; } // Proposal, In Progress, Completed
        public DateTime? SubmissionDate { get; set; }
        public string? Feedback { get; set; }

        // Skills & Achievements
        public string? Skills { get; set; }
        public string? Achievements { get; set; }

        // Social Links
        public string? LinkedIn { get; set; }
        public string? GitHub { get; set; }
        public string? Portfolio { get; set; }

        // === NEW: Store picture in DB ===
        public byte[]? ProfileImage { get; set; }
        public string? ProfileImageContentType { get; set; }

        // Foreign Key for Identity User
        public string? UserId { get; set; }
    }
}
