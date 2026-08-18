namespace EduNexis.Domain.Entities;

/// <summary>
/// A class comment under an assignment.
///
/// A sibling of AnnouncementComment rather than one polymorphic table: both
/// point at a real parent with a real foreign key, so a comment can never
/// outlive the thing it is attached to or dangle against a deleted id. The
/// duplication is a few lines; the alternative is a nullable-FK table that no
/// database constraint can protect.
/// </summary>
public class AssignmentComment : BaseEntity
{
    public Guid AssignmentId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Content { get; private set; } = string.Empty;

    /// <summary>
    /// The comment this one answers, or null for a top-level comment. One level
    /// only — see AnnouncementComment for why.
    /// </summary>
    public Guid? ParentCommentId { get; private set; }

    public Assignment Assignment { get; private set; } = null!;
    public User Author { get; private set; } = null!;

    protected AssignmentComment() { }

    public static AssignmentComment Create(
        Guid assignmentId, Guid authorId, string content, Guid? parentCommentId = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Comment cannot be empty.");

        return new AssignmentComment
        {
            AssignmentId = assignmentId,
            AuthorId = authorId,
            Content = content,
            ParentCommentId = parentCommentId,
        };
    }

    /// <summary>Author-only — a teacher may delete but never rewrite.</summary>
    public void Edit(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Comment cannot be empty.");

        Content = content;
        SetUpdatedAt();
    }
}
