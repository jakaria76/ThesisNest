using System.Collections.Generic;
using ThesisNest.Models;
using ThesisNest.Models.ViewModels;

namespace ThesisNest.ViewModels
{
    public class DashboardViewModel
    {
        public StudentProfile? StudentProfile { get; set; }
        public TeacherProfile? TeacherProfile { get; set; }
        public ThesisCreateVm? ThesisUpload { get; set; }
        public List<DashboardLink> CollaborationLinks { get; set; } = new();
        public List<string> Tasks { get; set; } = new();
    }

    public class DashboardLink
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}