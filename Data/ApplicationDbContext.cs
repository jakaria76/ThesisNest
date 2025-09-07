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

        // ========= PLAGIARISM =========
        public DbSet<PlagiarismDocument> PlagiarismDocuments { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<StudentProfile>(e =>
            {
                e.Property(p => p.ProfileImage).HasColumnType("varbinary(max)");
            });

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

            builder.Entity<Department>(e =>
            {
                e.HasIndex(d => d.Name).IsUnique();
            });

            builder.Entity<Thesis>(e =>
            {
                e.ToTable("Theses");
                e.HasIndex(t => new { t.TeacherProfileId, t.Status });
                e.HasIndex(t => new { t.DepartmentId, t.ProposalStatus });

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

            builder.Entity<ThesisVersion>(e =>
            {
                e.HasIndex(v => new { v.ThesisId, v.VersionNo }).IsUnique();
                e.HasOne(v => v.Thesis)
                 .WithMany(t => t.Versions)
                 .HasForeignKey(v => v.ThesisId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ThesisFeedback>(e =>
            {
                e.HasOne(f => f.Thesis)
                 .WithMany(t => t.Feedbacks)
                 .HasForeignKey(f => f.ThesisId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
