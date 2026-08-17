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

    /// <summary>
    /// Rewrites the body. Only ever called for the comment's own author — a
    /// teacher moderating a thread can delete a student's comment but must not
    /// be able to change words attributed to them.
    /// </summary>
    public void Edit(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Comment cannot be empty.");

        Content = content;
        SetUpdatedAt();
    }
}
