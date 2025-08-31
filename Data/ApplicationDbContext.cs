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

            // Student
            builder.Entity<StudentProfile>()
                   .Property(p => p.ProfileImage)
                   .HasColumnType("varbinary(max)");

            // Teacher
            builder.Entity<TeacherProfile>().ToTable("TeacherProfiles");
            builder.Entity<TeacherProfile>().HasIndex(t => t.UserId).IsUnique();
            builder.Entity<TeacherProfile>().Property(t => t.ProfileImage).HasColumnType("varbinary(max)");
            builder.Entity<TeacherProfile>().Property(t => t.OngoingThesisCount).HasDefaultValue(0);
            builder.Entity<TeacherProfile>().Property(t => t.CompletedThesisCount).HasDefaultValue(0);

            // Thesis
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

                e.HasOne<Department>(t => t.Department)
                 .WithMany()
                 .HasForeignKey(t => t.DepartmentId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ThesisVersion
            builder.Entity<ThesisVersion>(e =>
            {
                e.HasIndex(v => new { v.ThesisId, v.VersionNo }).IsUnique();
                e.HasOne(v => v.Thesis)
                 .WithMany(t => t.Versions)
                 .HasForeignKey(v => v.ThesisId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ThesisFeedback
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
