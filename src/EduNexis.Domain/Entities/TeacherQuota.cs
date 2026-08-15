namespace EduNexis.Domain.Entities;

/// <summary>
/// A single course-creation grant made to a teacher.
///
/// This is a ledger: a teacher may hold several grants at once, each with its
/// own size and expiry, and each is immutable in size once issued. Granting
/// more courses inserts another row rather than editing an existing one.
///
/// It replaced a single mutable row per teacher, which had two problems:
/// granting was destructive (the admin had to supply the new *total*, not the
/// amount to add, so a mistake silently cut someone's allowance), and a
/// short top-up could not be issued at all to a teacher who already held a
/// longer window — extending refused to move the end date backwards, so the
/// whole grant was rejected.
///
/// Grants never affect courses that already exist. Quota is consulted only when
/// a course is created; expiry or revocation stops the next course, and cannot
/// reach back and remove earlier ones.
/// </summary>
public class TeacherQuota : BaseEntity
{
    public Guid TeacherId { get; private set; }

    /// <summary>Admin who issued it, or the teacher themselves for the starter grant.</summary>
    public Guid AssignedById { get; private set; }

    /// <summary>Courses this grant is worth. Fixed once issued.</summary>
    public int TotalQuota { get; private set; }

    public int UsedQuota { get; private set; }
    public int RemainingQuota => TotalQuota - UsedQuota;

    public DateTime AccessStartDate { get; private set; }
    public DateTime AccessEndDate { get; private set; }

    /// <summary>Set when an admin withdraws the grant. Soft, so history survives.</summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>Optional admin note, e.g. "Spring 2027 premium".</summary>
    public string? Note { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    /// <summary>Within its window and not withdrawn.</summary>
    public bool IsAccessActive =>
        !IsRevoked &&
        DateTime.UtcNow >= AccessStartDate &&
        DateTime.UtcNow <= AccessEndDate;

    /// <summary>Active and still has courses left on it.</summary>
    public bool IsSpendable => IsAccessActive && RemainingQuota > 0;

    /// <summary>
    /// The teacher granted it to themselves — the automatic free-tier allowance
    /// provisioned on first course creation, as opposed to an admin grant.
    /// </summary>
    public bool IsStarterGrant => AssignedById == TeacherId;

    // Navigation
    public User Teacher { get; private set; } = null!;
    public User AssignedBy { get; private set; } = null!;

    protected TeacherQuota() { }

    public static TeacherQuota Create(
        Guid teacherId, Guid assignedById,
        int totalQuota, DateTime startDate, DateTime endDate,
        string? note = null)
    {
        if (totalQuota <= 0)
            throw new DomainException("Quota must be greater than zero.");
        if (endDate <= startDate)
            throw new DomainException("End date must be after start date.");

        return new TeacherQuota
        {
            TeacherId = teacherId,
            AssignedById = assignedById,
            TotalQuota = totalQuota,
            UsedQuota = 0,
            AccessStartDate = startDate,
            AccessEndDate = endDate,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };
    }

    /// <summary>
    /// Spend one course from this grant. Callers pick which grant to spend —
    /// see the repository's soonest-expiring-first ordering.
    /// </summary>
    public void ConsumeOne()
    {
        if (IsRevoked)
            throw new QuotaExceededException();
        if (!IsAccessActive)
            throw new AccessExpiredException();
        if (RemainingQuota <= 0)
            throw new QuotaExceededException();

        UsedQuota++;
        SetUpdatedAt();
    }

    /// <summary>
    /// Withdraw the grant. Idempotent. Courses already created with it are
    /// untouched — this only removes unspent allowance going forward.
    /// </summary>
    public void Revoke()
    {
        if (IsRevoked) return;
        RevokedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}
