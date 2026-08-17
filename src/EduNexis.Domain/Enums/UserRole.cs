namespace EduNexis.Domain.Enums;

/// <summary>
/// Ordered least-privileged first, so the enum's default value (0) is Student.
///
/// This previously started at SuperAdmin = 0, which meant any User that reached
/// the database without an explicit role would have been a full platform admin.
/// Nothing hit that path in practice, but "forgot to set the field" should fail
/// closed, not hand out the keys.
///
/// Safe to reorder: AppDbContext maps Role with HasConversion&lt;string&gt;(), so
/// rows store "Student" / "Teacher" / … and no numeric value is persisted.
/// </summary>
public enum UserRole
{
    Student = 0,
    Teacher = 1,
    SuperAdmin = 2
}
