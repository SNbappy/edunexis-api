using EduNexis.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduNexis.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<TeacherQuota> TeacherQuotas => Set<TeacherQuota>();
    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();
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
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<SubmissionAttachment> SubmissionAttachments => Set<SubmissionAttachment>();
    public DbSet<AssignmentComment> AssignmentComments => Set<AssignmentComment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserEducation> UserEducations => Set<UserEducation>();
    public DbSet<UserPublication> UserPublications => Set<UserPublication>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<CourseTeacher> CourseTeachers => Set<CourseTeacher>();
    public DbSet<CourseInvitation> CourseInvitations => Set<CourseInvitation>();

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
        // Stored by name, like every other enum here: an int column would remap
        // every saved preference the moment NotificationType is reordered.
        modelBuilder.Entity<NotificationPreference>().Property(p => p.Type).HasConversion<string>();
        modelBuilder.Entity<SubmissionAttachment>().Property(a => a.Kind).HasConversion<string>();
        modelBuilder.Entity<SubmissionAttachment>()
            .HasIndex(a => a.SubmissionId);
        modelBuilder.Entity<AssignmentComment>()
            .HasIndex(c => c.AssignmentId);

        // Replies. Indexed because every thread render groups on this column,
        // and deliberately left without a database-level foreign key: deleting
        // a comment here is a soft delete, so a cascade would be wrong and a
        // restrict would block a teacher moderating a thread. The handlers
        // validate the parent instead.
        modelBuilder.Entity<AssignmentComment>()
            .HasIndex(c => c.ParentCommentId);
        modelBuilder.Entity<AnnouncementComment>()
            .HasIndex(c => c.ParentCommentId);
        // One row per user per type — the upsert in
        // UpdateNotificationPreferencesCommand assumes it.
        modelBuilder.Entity<NotificationPreference>()
            .HasIndex(p => new { p.UserId, p.Type }).IsUnique();
        modelBuilder.Entity<Course>().Property(c => c.CourseType).HasConversion<string>();
        modelBuilder.Entity<PresentationEvent>().Property(p => p.Status).HasConversion<string>();
        modelBuilder.Entity<PresentationEvent>().Property(p => p.Format).HasConversion<string>();
        modelBuilder.Entity<UserPublication>().Property(p => p.Type).HasConversion<string>();
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(t => t.UserId);
            entity.HasIndex(t => new { t.UsedAt, t.ExpiresAt });
        });

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

        // ── Co-teaching ──────────────────────────────────────────────
        modelBuilder.Entity<CourseTeacher>(entity =>
        {
            entity.HasOne(t => t.Course).WithMany()
                .HasForeignKey(t => t.CourseId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(t => t.User).WithMany()
                .HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            // One row per teacher per course. The invitation-accept path checks
            // for an existing row, but a unique index is what actually stops a
            // double-accept from two tabs creating two.
            entity.HasIndex(t => new { t.CourseId, t.UserId }).IsUnique();
        });

        modelBuilder.Entity<CourseInvitation>(entity =>
        {
            entity.Property(i => i.Status).HasConversion<string>();
            entity.HasOne(i => i.Course).WithMany()
                .HasForeignKey(i => i.CourseId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(i => new { i.CourseId, i.InvitedUserId });
            // Drives the invitee's own list.
            entity.HasIndex(i => new { i.InvitedUserId, i.Status });
        });
    }
}


