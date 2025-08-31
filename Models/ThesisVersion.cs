using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ThesisNest.Models
{
    public class ThesisVersion
    {
        public int Id { get; set; }


        [Required] public int ThesisId { get; set; }
        public Thesis Thesis { get; set; } = null!;


        [Required] public int VersionNo { get; set; }


        [Required, Column(TypeName = "varbinary(max)")]
        public byte[] FileData { get; set; } = Array.Empty<byte>();


        [Required, StringLength(200)] public string FileName { get; set; } = string.Empty;
        [Required, StringLength(100)] public string ContentType { get; set; } = "application/octet-stream";
        public long FileSize { get; set; }


        [StringLength(500)] public string? Comment { get; set; }
        public int? CommentByStudentProfileId { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}