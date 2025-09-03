using System;
using System.ComponentModel.DataAnnotations;

namespace ThesisNest.Models
{
    public class StudentProfile
    {
        [Key]
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? University { get; set; }
        public string? Department { get; set; }
        public string? StudentId { get; set; }
        public string? Semester { get; set; }
        public double? GPA { get; set; }

        // Thesis Info
        public string? ThesisTitle { get; set; }
        public string? Supervisor { get; set; }
        public string? ThesisStatus { get; set; }
        public DateTime? SubmissionDate { get; set; }
        public string? Feedback { get; set; }
        public string? Skills { get; set; }
        public string? Achievements { get; set; }
        public string? LinkedIn { get; set; }
        public string? GitHub { get; set; }
        public string? Portfolio { get; set; }
        public byte[]? ProfileImage { get; set; }
        public string? ProfileImageContentType { get; set; }
        public string? UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
