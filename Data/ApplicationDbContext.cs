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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ========= STUDENT PROFILE =========
            builder.Entity<StudentProfile>(e =>
            {
                e.Property(p => p.ProfileImage).HasColumnType("varbinary(max)");
            });

            // ========= TEACHER PROFILE =========
            builder.Entity<TeacherProfile>(e =>
            {
                e.ToTable("TeacherProfiles");

                // unique keys
                e.HasIndex(t => t.UserId).IsUnique();
                e.HasIndex(t => t.Slug).IsUnique(); // ensure unique slug

                // columns / computed / concurrency
                e.Property(t => t.ProfileImage).HasColumnType("varbinary(max)");
                e.Ignore(t => t.OngoingThesisCount);
                e.Ignore(t => t.CompletedThesisCount);

                e.Property(t => t.RowVersion)
                 .IsRowVersion()
                 .IsConcurrencyToken();

                // ✅ align with AspNetUsers.Id = nvarchar(450)
                e.Property(t => t.UserId).HasMaxLength(450);

                // ✅ explicit FK to AspNetUsers
                e.HasOne<ApplicationUser>()
                 .WithMany()
                 .HasForeignKey(t => t.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ========= TEACHER EDUCATION =========
            builder.Entity<TeacherEducation>(e =>
            {
                e.HasIndex(t => new { t.TeacherProfileId, t.Degree }).IsUnique();

                // timestamps default (server-side)
                e.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                e.Property(t => t.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // RELATION: many Educations -> one TeacherProfile (Cascade on delete)
                e.HasOne(t => t.TeacherProfile)
                 .WithMany(p => p.Educations)
                 .HasForeignKey(t => t.TeacherProfileId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ========= TEACHER ACHIEVEMENTS =========
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

            // ========= TEACHER PUBLICATIONS =========
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

            // ========= DEPARTMENT =========
            builder.Entity<Department>(e =>
            {
                e.HasIndex(d => d.Name).IsUnique();
            });

            // ========= THESIS =========
            builder.Entity<Thesis>(e =>
            {
                e.ToTable("Theses");

                e.HasIndex(t => new { t.TeacherProfileId, t.Status });
                e.HasIndex(t => new { t.DepartmentId, t.ProposalStatus });

                // Supervisor (TeacherProfile) - Keep Restrict to prevent accidental delete
                e.HasOne(t => t.Supervisor)
                 .WithMany(p => p.Theses)
                 .HasForeignKey(t => t.TeacherProfileId)
                 .OnDelete(DeleteBehavior.Restrict);

                // StudentProfile - Restrict
                e.HasOne(t => t.Student)
                 .WithMany()
                 .HasForeignKey(t => t.StudentProfileId)
                 .OnDelete(DeleteBehavior.Restrict);

                // Department - Restrict
                e.HasOne(t => t.Department)
                 .WithMany()
                 .HasForeignKey(t => t.DepartmentId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ========= THESIS VERSION =========
            builder.Entity<ThesisVersion>(e =>
            {
                e.HasIndex(v => new { v.ThesisId, v.VersionNo }).IsUnique();

                e.HasOne(v => v.Thesis)
                 .WithMany(t => t.Versions)
                 .HasForeignKey(v => v.ThesisId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ========= THESIS FEEDBACK =========
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
