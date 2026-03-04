namespace EduNexis.Domain.Entities;

public class UserEducation : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Institution { get; private set; } = string.Empty;
    public string Degree { get; private set; } = string.Empty;
    public string FieldOfStudy { get; private set; } = string.Empty;
    public int StartYear { get; private set; }
    public int? EndYear { get; private set; }
    public string? Description { get; private set; }

    public User User { get; private set; } = null!;

    protected UserEducation() { }

    public static UserEducation Create(
        Guid userId, string institution, string degree,
        string fieldOfStudy, int startYear, int? endYear, string? description) =>
        new()
        {
            UserId = userId,
            Institution = institution,
            Degree = degree,
            FieldOfStudy = fieldOfStudy,
            StartYear = startYear,
            EndYear = endYear,
            Description = description
        };

    public void Update(
        string institution, string degree,
        string fieldOfStudy, int startYear, int? endYear, string? description)
    {
        Institution = institution;
        Degree = degree;
        FieldOfStudy = fieldOfStudy;
        StartYear = startYear;
        EndYear = endYear;
        Description = description;
        SetUpdatedAt();
    }
}
