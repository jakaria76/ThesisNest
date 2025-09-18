using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ThesisNest.Models;

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
        public DbSet<ThesisSubmission> ThesisSubmissions { get; set; } = default!;

        // ========= PLAGIARISM =========
        public DbSet<PlagiarismDocument> PlagiarismDocuments { get; set; } = default!;

        // ========= COMMUNICATION =========
        public DbSet<CommunicationThread> CommunicationThreads { get; set; } = default!;
        public DbSet<Message> Messages { get; set; } = default!;
        public DbSet<CallSession> CallSessions { get; set; } = default!;

        // ========= CHAT =========
        public DbSet<ChatMessage> ChatMessages { get; set; } = default!;

        // ========= FAQ =========
        public DbSet<FAQ> FAQs { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ===== StudentProfile =====
            builder.Entity<StudentProfile>(e =>
            {
                e.Property(p => p.ProfileImage).HasColumnType("bytea");
            });

            // ===== TeacherProfile =====
            builder.Entity<TeacherProfile>(e =>
            {
                e.ToTable("TeacherProfiles");
                e.HasIndex(t => t.UserId).IsUnique();
                e.HasIndex(t => t.Slug).IsUnique();
                e.Property(t => t.ProfileImage).HasColumnType("bytea");
                e.Ignore(t => t.OngoingThesisCount);
                e.Ignore(t => t.CompletedThesisCount);

                // PostgreSQL optimistic concurrency via xmin (shadow property)
                e.Property<uint>("xmin")
                 .IsConcurrencyToken()
                 .ValueGeneratedOnAddOrUpdate();

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
                e.Property(t => t.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
                e.Property(t => t.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
                e.HasOne(t => t.TeacherProfile)
                 .WithMany(p => p.Educations)
                 .HasForeignKey(t => t.TeacherProfileId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== TeacherAchievement =====
            builder.Entity<TeacherAchievement>(e =>
            {
                e.HasIndex(t => new { t.TeacherProfileId, t.Title }).IsUnique();
                e.Property(t => t.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
                e.Property(t => t.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
                e.HasOne(t => t.TeacherProfile)
                 .WithMany(p => p.Achievements)
                 .HasForeignKey(t => t.TeacherProfileId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== TeacherPublication =====
            builder.Entity<TeacherPublication>(e =>
            {
                e.HasIndex(t => new { t.TeacherProfileId, t.Title }).IsUnique();
                e.Property(t => t.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
                e.Property(t => t.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
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
                e.Property(t => t.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");

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
                e.Property(v => v.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
                e.HasOne(v => v.Thesis)
                 .WithMany(t => t.Versions)
                 .HasForeignKey(v => v.ThesisId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== ThesisFeedback =====
            builder.Entity<ThesisFeedback>(e =>
            {
                e.HasIndex(f => new { f.ThesisId, f.CreatedAt });
                e.Property(f => f.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
                e.HasOne(f => f.Thesis)
                 .WithMany(t => t.Feedbacks)
                 .HasForeignKey(f => f.ThesisId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== ThesisSubmission =====
            builder.Entity<ThesisSubmission>(e =>
            {
                e.Property(s => s.SubmissionDate).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
                e.HasOne(s => s.Student)
                 .WithMany()
                 .HasForeignKey(s => s.StudentId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ========= COMMUNICATION =========
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

            builder.Entity<Message>(e =>
            {
                e.ToTable("Messages");
                e.HasIndex(m => new { m.ThreadId, m.SentAt });
                e.Property(m => m.Text).HasMaxLength(4000).IsRequired();
                e.Property(m => m.SentAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");

                e.HasOne(m => m.Thread)
                 .WithMany(t => t.Messages)
                 .HasForeignKey(m => m.ThreadId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<CallSession>(e =>
            {
                e.HasIndex(c => new { c.ThreadId, c.StartedAt });
                e.Property(c => c.StartedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
                e.HasOne(c => c.Thread)
                 .WithMany(t => t.Calls)
                 .HasForeignKey(c => c.ThreadId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== ChatMessage =====
            builder.Entity<ChatMessage>(e =>
            {
                e.Property(c => c.Message)
                    .HasMaxLength(4000)
                    .IsRequired();

                e.Property(c => c.Timestamp)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");

                e.Property(c => c.User)
                    .HasMaxLength(255)
                    .IsRequired();

                e.Property(c => c.FromBot)
                    .HasDefaultValue(false);
            });

            builder.Entity<FAQ>(e =>
            {
                e.Property(f => f.Question).IsRequired().HasMaxLength(500);
                e.Property(f => f.Answer).IsRequired().HasMaxLength(4000);
                e.Property(f => f.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
            });
        }
    }
}
