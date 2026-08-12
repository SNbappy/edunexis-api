namespace EduNexis.Domain.Entities;

/// <summary>
/// Single-row table of platform-wide toggles an admin can flip at runtime,
/// without a redeploy. Currently just the course-creation quota switch:
/// while off, every teacher can create unlimited courses (pre-launch state).
/// When turned on, the existing 1-free-course + admin-grant quota system
/// (TeacherQuota) applies to every teacher going forward.
/// </summary>
public class PlatformSetting : BaseEntity
{
    public bool CourseQuotaEnforced { get; private set; } = false;
    public Guid? LastChangedById { get; private set; }

    protected PlatformSetting() { }

    public static PlatformSetting CreateDefault() => new();

    public void SetCourseQuotaEnforced(bool enforced, Guid changedById)
    {
        CourseQuotaEnforced = enforced;
        LastChangedById = changedById;
        SetUpdatedAt();
    }
}