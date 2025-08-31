using System.ComponentModel.DataAnnotations;


namespace ThesisNest.Models
{
    public class ThesisFeedback
    {
        public int Id { get; set; }


        [Required] public int ThesisId { get; set; }
        public Thesis Thesis { get; set; } = null!;


        [Required] public int GivenByTeacherProfileId { get; set; }
        public TeacherProfile? GivenBy { get; set; }


        [Required, StringLength(1500)] public string Message { get; set; } = string.Empty;


        public bool IsChangeRequested { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AcknowledgedAt { get; set; }
    }
}