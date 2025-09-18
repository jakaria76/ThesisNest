using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ThesisNest.Models
{
    // ThesisId + VersionNo একসাথে ইউনিক
    [Index(nameof(ThesisId), nameof(VersionNo), IsUnique = true)]
    public class ThesisVersion
    {
        public int Id { get; set; }

        [Required] public int ThesisId { get; set; }
        public Thesis Thesis { get; set; } = null!;

        [Required] public int VersionNo { get; set; }

        // ✅ Postgres BLOB
        [Required, Column(TypeName = "bytea")]
        public byte[] FileData { get; set; } = Array.Empty<byte>();

        [Required, StringLength(200)]
        public string FileName { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string ContentType { get; set; } = "application/octet-stream";

        public long FileSize { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }
        public int? CommentByStudentProfileId { get; set; }

        // timestamptz ম্যাপের জন্য DateTimeOffset ব্যবহার করা ভাল
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
