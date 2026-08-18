namespace EduNexis.Domain.Entities;

public class AnnouncementComment : BaseEntity
{
    public Guid AnnouncementId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// The comment this one answers, or null for a top-level comment.
    ///
    /// Exactly one level deep, enforced when the reply is created: a reply to a
    /// reply attaches to the same root. A class thread is a question and its
    /// answers, and unbounded nesting turns that into a tree nobody can read on
    /// a phone — while still keeping "who is this aimed at" explicit, which a
    /// flat list cannot.
    /// </summary>
    public Guid? ParentCommentId { get; private set; }

    // Navigation
    public Announcement Announcement { get; private set; } = null!;
    public User Author { get; private set; } = null!;

    protected AnnouncementComment() { }

    public static AnnouncementComment Create(
        Guid announcementId, Guid authorId, string content, Guid? parentCommentId = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Comment cannot be empty.");

        return new AnnouncementComment
        {
            AnnouncementId = announcementId,
            AuthorId = authorId,
            Content = content,
            ParentCommentId = parentCommentId
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
