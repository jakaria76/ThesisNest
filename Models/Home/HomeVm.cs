using System;
using System.Collections.Generic;

namespace ThesisNest.Models.ViewModels.Home
{
    public class HomeVm
    {
        // Common
        public string? UserFullName { get; set; }
        public string? Role { get; set; }

        // Student
        public int? StudentProfileId { get; set; }
        public int MyTotalSubmissions { get; set; }
        public string? MyProposalStatus { get; set; }
        public int MyFeedbackCount { get; set; }
        public List<ActivityItem> MyRecentActivities { get; set; } = new();

        // Teacher
        public int? TeacherProfileId { get; set; }
        public int PendingReviews { get; set; }
        public int OngoingCount { get; set; }
        public int CompletedCount { get; set; }
        public List<ActivityItem> TeacherRecent { get; set; } = new();   // <-- ADD THIS

        // Admin
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int ProposalsThisWeek { get; set; }
        public List<ActivityItem> SiteRecent { get; set; } = new();      // <-- if not present

        // Notices / Deadlines / FAQs
        public List<NoticeItem> Notices { get; set; } = new();
        public List<DeadlineItem> Deadlines { get; set; } = new();
        public List<FAQ> FAQs { get; set; } = new();
    }

    // If you don't already have this:
    public record ActivityItem(string Title, string? Detail, DateTime At, string? Link);
    public record NoticeItem(string Title, string? Url, DateTime? At);
    public record DeadlineItem(string Title, DateTime DueAt);
}
