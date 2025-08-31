using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ThesisNest.Models
{
    [Index(nameof(UserId), IsUnique = true)]
    [Table("TeacherProfiles")]
    public class TeacherProfile
    {
        [Key] public int Id { get; set; }

        // Identity link
        [Required, MaxLength(64)]
        public string UserId { get; set; } = string.Empty;

        // Basic
        [Required, MaxLength(120)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(80)] public string? Designation { get; set; }
        [MaxLength(80)] public string? Department { get; set; }
        [MaxLength(120)] public string? OfficeLocation { get; set; }

        // Contact
        [EmailAddress, MaxLength(120)] public string? Email { get; set; }
        [Phone, MaxLength(30)] public string? Phone { get; set; }
        public bool IsPublicEmail { get; set; } = true;
        public bool IsPublicPhone { get; set; } = false;

        // About
        [MaxLength(1000)] public string? Bio { get; set; }
        [MaxLength(1500)] public string? ResearchSummary { get; set; }

        // Photo in DB
        [Column(TypeName = "varbinary(max)")] public byte[]? ProfileImage { get; set; }
        [MaxLength(100)] public string? ProfileImageContentType { get; set; }
        [MaxLength(255)] public string? ProfileImageFileName { get; set; }

        // Routing/SEO
        [Required, MaxLength(140)]
        public string Slug { get; set; } = string.Empty;

        // Audit
        [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Required] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Optional soft-delete
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // (পুরনো persisted ফিল্ড—থাকলে ক্ষতি নেই; আমরা ভিউতে লাইভ ভ্যালু বসিয়ে দেব)
        public int OngoingThesisCount { get; set; } = 0;
        public int CompletedThesisCount { get; set; } = 0;

        [Timestamp] public byte[]? RowVersion { get; set; }

        // Teacher's theses
        public virtual ICollection<Thesis> Theses { get; set; } = new List<Thesis>();

        // Computed (NotMapped)
        [NotMapped]
        public int OngoingThesisCountComputed =>
            Theses?.Count(t => t.Status == ThesisStatus.Ongoing) ?? 0;

        [NotMapped]
        public int CompletedThesisCountComputed =>
            Theses?.Count(t => t.Status == ThesisStatus.Completed) ?? 0;
    }
}
