namespace EduNexis.Domain.Entities;

public class TeacherQuota : BaseEntity
{
    public Guid TeacherId { get; private set; }
    public Guid AssignedById { get; private set; }
    public int TotalQuota { get; private set; }
    public int UsedQuota { get; private set; }
    public int RemainingQuota => TotalQuota - UsedQuota;
    public DateTime AccessStartDate { get; private set; }
    public DateTime AccessEndDate { get; private set; }
    public bool IsAccessActive =>
        DateTime.UtcNow >= AccessStartDate &&
        DateTime.UtcNow <= AccessEndDate;

    // Navigation
    public User Teacher { get; private set; } = null!;
    public User AssignedBy { get; private set; } = null!;

    protected TeacherQuota() { }

    public static TeacherQuota Create(
        Guid teacherId, Guid assignedById,
        int totalQuota, DateTime startDate, DateTime endDate)
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
            AccessEndDate = endDate
        };
    }

    public void ConsumeOne()
    {
        if (!IsAccessActive)
            throw new AccessExpiredException();
        if (RemainingQuota <= 0)
            throw new QuotaExceededException();
        UsedQuota++;
        SetUpdatedAt();
    }

    public void UpdateQuota(int newTotal)
    {
        if (newTotal < UsedQuota)
            throw new DomainException("New quota cannot be less than already used quota.");
        TotalQuota = newTotal;
        SetUpdatedAt();
    }

    public void ExtendAccess(DateTime newEndDate)
    {
        if (newEndDate <= AccessEndDate)
            throw new DomainException("New end date must be later than current end date.");
        AccessEndDate = newEndDate;
        SetUpdatedAt();
    }
}
