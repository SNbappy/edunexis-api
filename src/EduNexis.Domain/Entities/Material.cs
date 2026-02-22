namespace EduNexis.Domain.Entities;

public class Material : BaseEntity
{
    public Guid CourseId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public MaterialType Type { get; private set; }
    public string? FileUrl { get; private set; }
    public string? EmbedUrl { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public bool IsPinned { get; private set; } = false;
    public int DownloadCount { get; private set; } = 0;
    public Guid UploadedById { get; private set; }

    // Navigation
    public Course Course { get; private set; } = null!;
    public User UploadedBy { get; private set; } = null!;

    protected Material() { }

    public static Material Create(
        Guid courseId, string title, MaterialType type,
        string? fileUrl, string? embedUrl, string? thumbnailUrl,
        string? description, string? category, Guid uploadedById)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Material title is required.");

        return new Material
        {
            CourseId = courseId,
            Title = title,
            Type = type,
            FileUrl = fileUrl,
            EmbedUrl = embedUrl,
            ThumbnailUrl = thumbnailUrl,
            Description = description,
            Category = category,
            UploadedById = uploadedById
        };
    }

    public void Update(string title, string? description, string? category)
    {
        Title = title;
        Description = description;
        Category = category;
        SetUpdatedAt();
    }

    public void Pin() { IsPinned = true; SetUpdatedAt(); }
    public void Unpin() { IsPinned = false; SetUpdatedAt(); }
    public void IncrementDownload() { DownloadCount++; SetUpdatedAt(); }
}
