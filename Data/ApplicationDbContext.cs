using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ThesisNest.Models;
// ⚠️ Twilio টাইপ কনফ্লিক্ট এড়াতে এটা ব্যবহার করবেন না:
// using Twilio.TwiML.Messaging;

namespace ThesisNest.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ========= PROFILES =========
        public DbSet<StudentProfile> StudentProfiles { get; set; } = default!;
        public DbSet<TeacherProfile> TeacherProfiles { get; set; } = default!;

        // ========= EDUCATION / ACHIEVEMENTS / PUBLICATIONS =========
        public DbSet<TeacherEducation> TeacherEducations { get; set; } = default!;
        public DbSet<TeacherAchievement> TeacherAchievements { get; set; } = default!;
        public DbSet<TeacherPublication> TeacherPublications { get; set; } = default!;

        // ========= OTHER ENTITIES =========
        public DbSet<Department> Departments { get; set; } = default!;
        public DbSet<Thesis> Theses { get; set; } = default!;
        public DbSet<ThesisVersion> ThesisVersions { get; set; } = default!;
        public DbSet<ThesisFeedback> ThesisFeedbacks { get; set; } = default!;

        // ========= PLAGIARISM =========
        public DbSet<PlagiarismDocument> PlagiarismDocuments { get; set; } = default!;

        // ========= COMMUNICATION =========
        public DbSet<CommunicationThread> CommunicationThreads { get; set; } = default!;
        // Fully-qualify to avoid any ambiguity (e.g., Twilio):
        public DbSet<ThesisNest.Models.Message> Messages { get; set; } = default!;
        public DbSet<CallSession> CallSessions { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ===== StudentProfile =====
            builder.Entity<StudentProfile>(e =>
            {
                e.Property(p => p.ProfileImage).HasColumnType("varbinary(max)");
            });

            // ===== TeacherProfile =====
            builder.Entity<TeacherProfile>(e =>
            {
                e.ToTable("TeacherProfiles");
                e.HasIndex(t => t.UserId).IsUnique();
                e.HasIndex(t => t.Slug).IsUnique();
                e.Property(t => t.ProfileImage).HasColumnType("varbinary(max)");
                e.Ignore(t => t.OngoingThesisCount);
                e.Ignore(t => t.CompletedThesisCount);
                e.Property(t => t.RowVersion).IsRowVersion().IsConcurrencyToken();
                e.Property(t => t.UserId).HasMaxLength(450);
                e.HasOne<ApplicationUser>()
                 .WithMany()
                 .HasForeignKey(t => t.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== TeacherEducation =====
            builder.Entity<TeacherEducation>(e =>
            {
                e.HasIndex(t => new { t.TeacherProfileId, t.Degree }).IsUnique();
                e.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.Property(t => t.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.HasOne(t => t.TeacherProfile)
                 .WithMany(p => p.Educations)
                 .HasForeignKey(t => t.TeacherProfileId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== TeacherAchievement =====
            builder.Entity<TeacherAchievement>(e =>
            {
                e.HasIndex(t => new { t.TeacherProfileId, t.Title }).IsUnique();
                e.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.Property(t => t.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.HasOne(t => t.TeacherProfile)
                 .WithMany(p => p.Achievements)
                 .HasForeignKey(t => t.TeacherProfileId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== TeacherPublication =====
            builder.Entity<TeacherPublication>(e =>
            {
                e.HasIndex(t => new { t.TeacherProfileId, t.Title }).IsUnique();
                e.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.Property(t => t.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.HasOne(t => t.TeacherProfile)
                 .WithMany(p => p.Publications)
                 .HasForeignKey(t => t.TeacherProfileId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== Department =====
            builder.Entity<Department>(e =>
            {
                e.HasIndex(d => d.Name).IsUnique();
            });

            // ===== Thesis =====
            builder.Entity<Thesis>(e =>
            {
                e.ToTable("Theses");
                e.HasIndex(t => new { t.TeacherProfileId, t.Status });
                e.HasIndex(t => new { t.DepartmentId, t.ProposalStatus });
                e.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                e.HasOne(t => t.Supervisor)
                 .WithMany(p => p.Theses)
                 .HasForeignKey(t => t.TeacherProfileId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(t => t.Student)
                 .WithMany()
                 .HasForeignKey(t => t.StudentProfileId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(t => t.Department)
                 .WithMany()
                 .HasForeignKey(t => t.DepartmentId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ===== ThesisVersion =====
            builder.Entity<ThesisVersion>(e =>
            {
                e.HasIndex(v => new { v.ThesisId, v.VersionNo }).IsUnique();
                e.Property(v => v.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.HasOne(v => v.Thesis)
                 .WithMany(t => t.Versions)
                 .HasForeignKey(v => v.ThesisId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== ThesisFeedback =====
            builder.Entity<ThesisFeedback>(e =>
            {
                e.HasIndex(f => new { f.ThesisId, f.CreatedAt });   // তালিকা দেখাতে সুবিধা
                e.Property(f => f.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.HasOne(f => f.Thesis)
                 .WithMany(t => t.Feedbacks)
                 .HasForeignKey(f => f.ThesisId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ========= COMMUNICATION =========

            // ----- CommunicationThread -----
            builder.Entity<CommunicationThread>(e =>
            {
                e.HasIndex(t => new { t.TeacherProfileId, t.StudentProfileId }).IsUnique();
                e.Property(t => t.IsEnabled).HasDefaultValue(false);

                e.HasOne(t => t.Teacher)
                 .WithMany()
                 .HasForeignKey(t => t.TeacherProfileId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(t => t.Student)
                 .WithMany()
                 .HasForeignKey(t => t.StudentProfileId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ----- Message (fully-qualified) -----
            builder.Entity<ThesisNest.Models.Message>(e =>
            {
                e.ToTable("Messages");
                e.HasIndex(m => new { m.ThreadId, m.SentAt });
                e.Property(m => m.Text).HasMaxLength(4000).IsRequired();
                e.Property(m => m.SentAt).HasDefaultValueSql("GETUTCDATE()");

                e.HasOne(m => m.Thread)
                 .WithMany(t => t.Messages)
                 .HasForeignKey(m => m.ThreadId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ----- CallSession -----
            builder.Entity<CallSession>(e =>
            {
                e.HasIndex(c => new { c.ThreadId, c.StartedAt });
                e.Property(c => c.StartedAt).HasDefaultValueSql("GETUTCDATE()");
                e.HasOne(c => c.Thread)
                 .WithMany(t => t.Calls)
                 .HasForeignKey(c => c.ThreadId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
