using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using ThesisNest.Models;

namespace ThesisNest.ViewModels
{
    public class StudentProfileViewModel
    {
        // Personal info
        public string? FullName { get; set; }
        public string? RollNumber { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Department { get; set; }
        public string? Program { get; set; }
        public int? Year { get; set; }
        public int? Semester { get; set; }
        public DateTime EnrollmentDate { get; set; }

        // Profile picture
        public IFormFile? ProfilePicture { get; set; }
        public string? ProfilePictureBase64 { get; set; }
        public string? ProfilePictureContentType { get; set; }

        // Thesis submissions
        public List<ThesisSubmission> Submissions { get; set; } = new();

        // New/Resubmit thesis
        public IFormFile? NewThesisFile { get; set; }
        public string? NewThesisTitle { get; set; }
        public string? NewThesisAbstract { get; set; }
        public string? SupervisorName { get; set; }
        public string? Batch { get; set; }
    }
}
