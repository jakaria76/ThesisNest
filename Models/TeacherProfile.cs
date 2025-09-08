using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;

namespace ThesisNest.Models
{
    [Index(nameof(UserId), IsUnique = true)]
    [Index(nameof(Slug), IsUnique = true)]
    [Table("TeacherProfiles")]
    public class TeacherProfile
    {
        [Key]
        public int Id { get; set; }

        // FK to AspNetUsers.Id (nvarchar(450))
        [Required, MaxLength(450)]
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

        [Required, MaxLength(140)]
        public string Slug { get; set; } = string.Empty;

        [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [Required] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        // ---------- Navigation ----------

        [ValidateNever]
        public virtual ApplicationUser? ApplicationUser { get; set; }

        [ValidateNever] public virtual ICollection<Thesis> Theses { get; set; } = new List<Thesis>();
        [ValidateNever] public virtual ICollection<TeacherEducation> Educations { get; set; } = new List<TeacherEducation>();
        [ValidateNever] public virtual ICollection<TeacherAchievement> Achievements { get; set; } = new List<TeacherAchievement>();
        [ValidateNever] public virtual ICollection<TeacherPublication> Publications { get; set; } = new List<TeacherPublication>();

        // ---------- Computed ----------
        [NotMapped]
        public int OngoingThesisCount => Theses?.Count(t => t.Status == ThesisStatus.Pending) ?? 0;

        [NotMapped]
        public int CompletedThesisCount => Theses?.Count(t => t.Status == ThesisStatus.Accept) ?? 0;

        // ---------- Location ----------
        [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double? Latitude { get; set; }

        [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double? Longitude { get; set; }
    }
}
