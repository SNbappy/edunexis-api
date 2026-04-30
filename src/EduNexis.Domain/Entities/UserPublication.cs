using EduNexis.Domain.Enums;

namespace EduNexis.Domain.Entities;

public class UserPublication : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Authors { get; private set; } = string.Empty;
    public string? Venue { get; private set; }
    public int Year { get; private set; }
    public string? Url { get; private set; }
    public PublicationType Type { get; private set; }
    public int OrderIndex { get; private set; }

    public User User { get; private set; } = null!;

    protected UserPublication() { }

    public static UserPublication Create(
        Guid userId, string title, string authors,
        string? venue, int year, string? url,
        PublicationType type, int orderIndex)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Publication title is required");
        if (string.IsNullOrWhiteSpace(authors))
            throw new DomainException("Publication authors are required");
        if (year < 1900 || year > DateTime.UtcNow.Year + 1)
            throw new DomainException("Invalid publication year");

        return new UserPublication
        {
            UserId = userId,
            Title = title.Trim(),
            Authors = authors.Trim(),
            Venue = string.IsNullOrWhiteSpace(venue) ? null : venue.Trim(),
            Year = year,
            Url = string.IsNullOrWhiteSpace(url) ? null : url.Trim(),
            Type = type,
            OrderIndex = orderIndex
        };
    }

    public void Update(
        string title, string authors,
        string? venue, int year, string? url,
        PublicationType type)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Publication title is required");
        if (string.IsNullOrWhiteSpace(authors))
            throw new DomainException("Publication authors are required");
        if (year < 1900 || year > DateTime.UtcNow.Year + 1)
            throw new DomainException("Invalid publication year");

        Title = title.Trim();
        Authors = authors.Trim();
        Venue = string.IsNullOrWhiteSpace(venue) ? null : venue.Trim();
        Year = year;
        Url = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        Type = type;
        SetUpdatedAt();
    }

    public void SetOrderIndex(int orderIndex)
    {
        OrderIndex = orderIndex;
        SetUpdatedAt();
    }
}