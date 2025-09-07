using System;

namespace ThesisNest.Models
{
    public class PlagiarismDocument
    {
        public int Id { get; set; }
        public string FileName { get; set; } = "";
        public string TextContent { get; set; } = "";
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public float CombinedScore { get; set; } = 0f;
    }
}
