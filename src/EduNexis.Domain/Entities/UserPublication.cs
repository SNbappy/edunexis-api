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

    // PDF storage (Cloudinary)
    public string? PdfUrl { get; private set; }
    public string? PdfPublicId { get; private set; }
    public long? PdfSizeBytes { get; private set; }
    public DateTime? PdfUploadedAt { get; private set; }
    public bool IsPdfPublic { get; private set; } = true;

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
            OrderIndex = orderIndex,
            IsPdfPublic = true
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

    // ── PDF management ────────────────────────────────────────────

    public void SetPdf(string url, string publicId, long sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("PDF URL is required");
        if (string.IsNullOrWhiteSpace(publicId))
            throw new DomainException("PDF public ID is required");

        PdfUrl = url;
        PdfPublicId = publicId;
        PdfSizeBytes = sizeBytes;
        PdfUploadedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void RemovePdf()
    {
        PdfUrl = null;
        PdfPublicId = null;
        PdfSizeBytes = null;
        PdfUploadedAt = null;
        SetUpdatedAt();
    }

    public void SetPdfPublic(bool isPublic)
    {
        IsPdfPublic = isPublic;
        SetUpdatedAt();
    }
}