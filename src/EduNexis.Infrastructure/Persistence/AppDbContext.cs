using EduNexis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduNexis.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<TeacherQuota> TeacherQuotas => Set<TeacherQuota>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseMember> CourseMembers => Set<CourseMember>();
    public DbSet<JoinRequest> JoinRequests => Set<JoinRequest>();
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
    public DbSet<PlagiarismReport> PlagiarismReports => Set<PlagiarismReport>();
    public DbSet<GradeComplaint> GradeComplaints => Set<GradeComplaint>();
    public DbSet<GradeComplaintMessage> GradeComplaintMessages => Set<GradeComplaintMessage>();
    public DbSet<CTEvent> CTEvents => Set<CTEvent>();
    public DbSet<CTSubmission> CTSubmissions => Set<CTSubmission>();
    public DbSet<PresentationEvent> PresentationEvents => Set<PresentationEvent>();
    public DbSet<PresentationMark> PresentationMarks => Set<PresentationMark>();
    public DbSet<GradingFormula> GradingFormulas => Set<GradingFormula>();
    public DbSet<FormulaComponent> FormulaComponents => Set<FormulaComponent>();
    public DbSet<FinalMark> FinalMarks => Set<FinalMark>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AnnouncementComment> AnnouncementComments => Set<AnnouncementComment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enums stored as strings
        modelBuilder.Entity<User>().Property(u => u.Role).HasConversion<string>();
        modelBuilder.Entity<JoinRequest>().Property(j => j.Status).HasConversion<string>();
        modelBuilder.Entity<Material>().Property(m => m.Type).HasConversion<string>();
        modelBuilder.Entity<AssignmentSubmission>().Property(s => s.SubmissionType).HasConversion<string>();
        modelBuilder.Entity<GradeComplaint>().Property(g => g.Status).HasConversion<string>();
        modelBuilder.Entity<AttendanceRecord>().Property(a => a.Status).HasConversion<string>();
        modelBuilder.Entity<FormulaComponent>().Property(f => f.ComponentType).HasConversion<string>();
        modelBuilder.Entity<Notification>().Property(n => n.Type).HasConversion<string>();
        modelBuilder.Entity<Course>().Property(c => c.CourseType).HasConversion<string>();

        // Unique indexes
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Course>().HasIndex(c => c.JoiningCode).IsUnique();

        // Restrict delete on multi-FK relationships
        modelBuilder.Entity<TeacherQuota>()
            .HasOne(t => t.Teacher).WithMany(u => u.TeacherQuotas)
            .HasForeignKey(t => t.TeacherId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TeacherQuota>()
            .HasOne(t => t.AssignedBy).WithMany()
            .HasForeignKey(t => t.AssignedById).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Course>()
            .HasOne(c => c.Teacher).WithMany()
            .HasForeignKey(c => c.TeacherId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JoinRequest>()
            .HasOne(j => j.ReviewedBy).WithMany()
            .HasForeignKey(j => j.ReviewedById)
            .IsRequired(false).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FinalMark>()
            .HasOne(f => f.Student).WithMany()
            .HasForeignKey(f => f.StudentId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FinalMark>()
            .HasOne(f => f.Course).WithMany()
            .HasForeignKey(f => f.CourseId).OnDelete(DeleteBehavior.Restrict);
    }
}
