namespace EduNexis.Domain.Entities;

public class AnnouncementComment : BaseEntity
{
    public Guid AnnouncementId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Content { get; private set; } = string.Empty;

    // Navigation
    public Announcement Announcement { get; private set; } = null!;
    public User Author { get; private set; } = null!;

    protected AnnouncementComment() { }

    public static AnnouncementComment Create(
        Guid announcementId, Guid authorId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Comment cannot be empty.");

        return new AnnouncementComment
        {
            AnnouncementId = announcementId,
            AuthorId = authorId,
            Content = content
        };
    }
}
