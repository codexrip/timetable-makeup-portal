using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MongoDB.EntityFrameworkCore.Extensions;
using TimetableMakeup.Models;

namespace TimetableMakeup.Data;

/// <summary>
/// Entity Framework Core DbContext configured for MongoDB.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        // Standalone MongoDB servers do not support multi-document transactions.
        Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AcademicProgram> AcademicPrograms => Set<AcademicProgram>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<TeachingAssignment> TeachingAssignments => Set<TeachingAssignment>();
    public DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();
    public DbSet<MakeupRequest> MakeupRequests => Set<MakeupRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Dates here are calendar days, not moments in time. Save them as UTC midnight
        // and read them back as the same calendar day, so the UTC+5 offset can never
        // shift them to the previous day. (CreatedAt/HeldAt etc. are real moments and
        // stay UTC; pages already call ToLocalTime() when showing them.)
        var dateOnly = new ValueConverter<DateTime, DateTime>(
            v => DateTime.SpecifyKind(v.Date, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v.Date, DateTimeKind.Unspecified));

        modelBuilder.Entity<Department>().ToCollection("departments");
        modelBuilder.Entity<AcademicProgram>().ToCollection("academic_programs");

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToCollection("app_users");
            entity.Property(u => u.Role).HasConversion<string>();
        });

        modelBuilder.Entity<Teacher>().ToCollection("teachers");
        modelBuilder.Entity<Course>().ToCollection("courses");
        modelBuilder.Entity<Section>().ToCollection("sections");
        modelBuilder.Entity<Room>().ToCollection("rooms");
        modelBuilder.Entity<TeachingAssignment>().ToCollection("teaching_assignments");

        modelBuilder.Entity<TimetableEntry>(entity =>
        {
            entity.ToCollection("timetable_entries");
            entity.Property(t => t.DayOfWeek).HasConversion<string>();
            entity.Property(t => t.MakeupDate).HasConversion(dateOnly);
        });

        modelBuilder.Entity<MakeupRequest>(entity =>
        {
            entity.ToCollection("makeup_requests");
            entity.Property(m => m.Status).HasConversion<string>();
            entity.Property(m => m.MissedDate).HasConversion(dateOnly);
            entity.Property(m => m.ApprovedDate).HasConversion(dateOnly);
            entity.OwnsMany(m => m.PreferredSlots, b =>
            {
                b.Property(p => p.Date).HasConversion(dateOnly);
            });
        });
    }
}
