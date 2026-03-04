using EduNexis.Domain.Enums;

namespace EduNexis.Domain.Entities;

public class CTEvent : BaseEntity
{
    public Guid CourseId { get; private set; }
    public int CTNumber { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public decimal MaxMarks { get; private set; }
    public DateTime? HeldOn { get; private set; }
    public CTStatus Status { get; private set; } = CTStatus.Draft;
    public string? BestScriptUrl { get; private set; }
    public Guid? BestStudentId { get; private set; }
    public string? WorstScriptUrl { get; private set; }
    public Guid? WorstStudentId { get; private set; }
    public string? AverageScriptUrl { get; private set; }
    public Guid? AverageStudentId { get; private set; }
    public Guid CreatedById { get; private set; }

    public bool KhataUploaded =>
        BestScriptUrl is not null &&
        WorstScriptUrl is not null &&
        AverageScriptUrl is not null;

    // Navigation
    public Course Course { get; private set; } = null!;
    public User CreatedBy { get; private set; } = null!;
    public ICollection<CTSubmission> Submissions { get; private set; } = [];

    protected CTEvent() { }

    public static CTEvent Create(
        Guid courseId, int ctNumber, string title,
        decimal maxMarks, DateTime? heldOn, Guid createdById)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("CT title is required.");
        if (maxMarks <= 0)
            throw new DomainException("Max marks must be greater than zero.");

        return new CTEvent
        {
            CourseId = courseId,
            CTNumber = ctNumber,
            Title = title,
            MaxMarks = maxMarks,
            HeldOn = heldOn,
            CreatedById = createdById,
            Status = CTStatus.Draft
        };
    }

    public void Update(string title, decimal maxMarks, DateTime? heldOn)
    {
        Title = title;
        MaxMarks = maxMarks;
        HeldOn = heldOn;
        SetUpdatedAt();
    }

    public void UploadKhata(
        string bestUrl, Guid? bestStudentId,
        string worstUrl, Guid? worstStudentId,
        string avgUrl, Guid? avgStudentId)
    {
        BestScriptUrl = bestUrl;
        BestStudentId = bestStudentId;
        WorstScriptUrl = worstUrl;
        WorstStudentId = worstStudentId;
        AverageScriptUrl = avgUrl;
        AverageStudentId = avgStudentId;
        SetUpdatedAt();
    }

    public void Unpublish()
    {
        Status = CTStatus.Draft;
        SetUpdatedAt();
    }

    public void Publish()
    {
        if (!KhataUploaded)
            throw new DomainException("Cannot publish CT: all 3 khata must be uploaded first.");
        Status = CTStatus.Published;
        SetUpdatedAt();
    }
}

