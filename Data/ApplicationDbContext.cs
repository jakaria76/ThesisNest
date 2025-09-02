using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ThesisNest.Models;

namespace ThesisNest.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<StudentProfile> StudentProfiles { get; set; } = default!;
        public DbSet<TeacherProfile> TeacherProfiles { get; set; } = default!;
        public DbSet<Department> Departments { get; set; } = default!;
        public DbSet<Thesis> Theses { get; set; } = default!;
        public DbSet<ThesisVersion> ThesisVersions { get; set; } = default!;
        public DbSet<ThesisFeedback> ThesisFeedbacks { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ✅ Student Profile Config
            builder.Entity<StudentProfile>(e =>
            {
                e.Property(p => p.ProfileImage).HasColumnType("varbinary(max)");
            });

            // ✅ Teacher Profile Config
            builder.Entity<TeacherProfile>(e =>
            {
                e.ToTable("TeacherProfiles");
                e.HasIndex(t => t.UserId).IsUnique();
                e.Property(t => t.ProfileImage).HasColumnType("varbinary(max)");

                // Ignore computed properties (not mapped to DB)
                e.Ignore(t => t.OngoingThesisCount);
                e.Ignore(t => t.CompletedThesisCount);

                // ✅ RowVersion for concurrency
                e.Property(t => t.RowVersion)
                 .IsRowVersion()
                 .IsConcurrencyToken();
            });

            // ✅ Thesis Config
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

            // ✅ ThesisVersion Config
            builder.Entity<ThesisVersion>(e =>
            {
                e.HasIndex(v => new { v.ThesisId, v.VersionNo }).IsUnique();

                e.HasOne(v => v.Thesis)
                 .WithMany(t => t.Versions)
                 .HasForeignKey(v => v.ThesisId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ✅ ThesisFeedback Config
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
