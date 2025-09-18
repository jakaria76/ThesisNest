using System;
using System.Collections.Generic;
using ThesisNest.Models; // for FAQ

namespace ThesisNest.Models.ViewModels.Home
{
    // Simple DTOs for dashboard
    public record NoticeItem(string Title, string? Url, DateTime? When);
    public record DeadlineItem(string Title, DateTime? Due, string? Url = null);

    public class ActivityItem
    {
        public string Title { get; init; }
        public string? Message { get; init; }
        public DateTime WhenUtc { get; init; }
        public string? Url { get; init; }

        // Accepts DateTime (will normalize to UTC)
        public ActivityItem(string title, string? message, DateTime whenUtc, string? url = null)
        {
            Title = title;
            Message = message;
            WhenUtc = whenUtc.Kind == DateTimeKind.Utc
                ? whenUtc
                : DateTime.SpecifyKind(whenUtc, DateTimeKind.Utc);
            Url = url;
        }

        // Also accept DateTimeOffset (quality-of-life ctor)
        public ActivityItem(string title, string? message, DateTimeOffset when, string? url = null)
            : this(title, message, when.UtcDateTime, url) { }
    }

    public class HomeVm
    {
        // Common
        public string? UserFullName { get; set; }
        public string? Role { get; set; }
        public List<FAQ> FAQs { get; set; } = new();
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
}
