using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ThesisNest.Models
{
    public class Thesis
    {
        [Key] public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        // Supervisor (required)
        [Required] public int TeacherProfileId { get; set; }
        [ForeignKey(nameof(TeacherProfileId))] public TeacherProfile Supervisor { get; set; } = null!;

        // Student owner (OPTIONAL at creation time)
        public int? StudentProfileId { get; set; }
        [ForeignKey(nameof(StudentProfileId))] public StudentProfile? Student { get; set; }

        // Department (REQUIRED)
        [Required] public int DepartmentId { get; set; }
        [ForeignKey(nameof(DepartmentId))] public Department Department { get; set; } = null!;

        // lifecycle + proposal
        [Required] public ThesisStatus Status { get; set; } = ThesisStatus.Proposed;
        [Required] public ProposalStatus ProposalStatus { get; set; } = ProposalStatus.Draft;

        // proposal details
        [Required, MaxLength(4000)] public string Abstract { get; set; } = string.Empty;
        [MaxLength(300)] public string? Keywords { get; set; }

        public int CurrentVersionNo { get; set; } = 1;

        [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public ICollection<ThesisVersion> Versions { get; set; } = new List<ThesisVersion>();
        public ICollection<ThesisFeedback> Feedbacks { get; set; } = new List<ThesisFeedback>();
    }
}
