using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ThesisNest.Models
{
    [Index(nameof(UserId), IsUnique = true)]
    [Table("TeacherProfiles")]
    public class TeacherProfile
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(64)]
        public string UserId { get; set; } = string.Empty;

        [Required, MaxLength(120)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(80)] public string? Designation { get; set; }
        [MaxLength(80)] public string? Department { get; set; }
        [MaxLength(120)] public string? OfficeLocation { get; set; }

        [EmailAddress, MaxLength(120)] public string? Email { get; set; }
        [Phone, MaxLength(30)] public string? Phone { get; set; }
        public bool IsPublicEmail { get; set; } = true;
        public bool IsPublicPhone { get; set; } = false;

        [MaxLength(1000)] public string? Bio { get; set; }
        [MaxLength(1500)] public string? ResearchSummary { get; set; }

        [Column(TypeName = "varbinary(max)")]
        public byte[]? ProfileImage { get; set; }

        [MaxLength(100)] public string? ProfileImageContentType { get; set; }
        [MaxLength(255)] public string? ProfileImageFileName { get; set; }

        [Required, MaxLength(140)] public string Slug { get; set; } = string.Empty;

        [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Required] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public virtual ICollection<Thesis> Theses { get; set; } = new List<Thesis>();

        [NotMapped]
        public int OngoingThesisCount => Theses?.Count(t => t.Status == ThesisStatus.InProgress) ?? 0;

        [NotMapped]
        public int CompletedThesisCount => Theses?.Count(t => t.Status == ThesisStatus.Completed) ?? 0;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
