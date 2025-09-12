using System;
using System.Collections.Generic;

namespace ThesisNest.Models.ViewModels.Home
{
    public class HomeVm
    {
        public string? UserFullName { get; set; }
        public string Role { get; set; } = "Guest";

        // Common
        public List<NoticeItem> Notices { get; set; } = new();
        public List<DeadlineItem> Deadlines { get; set; } = new();

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
        public List<ActivityItem> TeacherRecent { get; set; } = new();

        // Admin
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int ProposalsThisWeek { get; set; }
        public List<ActivityItem> SiteRecent { get; set; } = new();
    }

    public record NoticeItem(string Title, string Url, DateTime? PinnedUntil);
    public record DeadlineItem(string Title, DateTime DueAt, string? Url);
    public record ActivityItem(string Title, string Detail, DateTime At, string? Url);
}
