namespace EduNexis.Domain.Entities;

public class Announcement : BaseEntity
{
    public Guid CourseId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public string? AttachmentUrl { get; private set; }

    // Navigation
    public Course Course { get; private set; } = null!;
    public User Author { get; private set; } = null!;
    public ICollection<AnnouncementComment> Comments { get; private set; } = [];

    protected Announcement() { }

    public static Announcement Create(
        Guid courseId, Guid authorId, string content, string? attachmentUrl)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Announcement content is required.");

        return new Announcement
        {
            CourseId = courseId,
            AuthorId = authorId,
            Content = content,
            AttachmentUrl = attachmentUrl
        };
    }

    public void Update(string content, string? attachmentUrl)
    {
        Content = content;
        AttachmentUrl = attachmentUrl;
        SetUpdatedAt();
    }
}
