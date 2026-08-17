using EduNexis.Application.Features.Notifications.Commands;
using EduNexis.Application.Abstractions;

namespace EduNexis.Application.Features.Announcements.Commands;

public record CommentDto(
    Guid Id,
    Guid AnnouncementId,
    Guid AuthorId,
    string AuthorName,
    string? AuthorPhotoUrl,
    string Content,
    DateTime CreatedAt,
    /// <summary>Whether the caller may delete this comment (own comment, or teacher).</summary>
    bool CanDelete,
    /// <summary>Whether the caller may edit it. Authors only — a teacher may
    /// remove a student's comment but never rewrite it in their name.</summary>
    bool CanEdit,
    /// <summary>Set once edited, so a changed comment says so.</summary>
    DateTime? EditedAt = null
);

/// <summary>
/// A class comment on an announcement.
///
/// The AnnouncementComment entity, its DbSet and its table already existed but
/// nothing ever read or wrote them — the feature was modelled and then never
/// connected. This is the missing half.
/// </summary>
public record AddCommentCommand(
    Guid CourseId,
    Guid AnnouncementId,
    Guid AuthorId,
    string Content
) : ICommand<ApiResponse<CommentDto>>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(1000);
    }
}

public sealed class AddCommentCommandHandler(
    IUnitOfWork uow,
    ISender sender
) : ICommandHandler<AddCommentCommand, ApiResponse<CommentDto>>
{
    public async ValueTask<ApiResponse<CommentDto>> Handle(
        AddCommentCommand cmd, CancellationToken ct)
    {
        var content = cmd.Content?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
            return ApiResponse<CommentDto>.Fail("Comment cannot be empty.");
        if (content.Length > 1000)
            return ApiResponse<CommentDto>.Fail("Comment is too long (1000 characters max).");

        var announcement = await uow.GetRepository<Announcement>()
            .GetByIdAsync(cmd.AnnouncementId, ct);

        if (announcement is null || announcement.IsDeleted)
            return ApiResponse<CommentDto>.Fail("Announcement not found.");

        // The announcement must belong to the course in the route, or a member
        // of course A could comment on course B's announcement by id.
        if (announcement.CourseId != cmd.CourseId)
            return ApiResponse<CommentDto>.Fail("Announcement not found.");

        // Only people in the course may comment.
        var member = await uow.CourseMembers.GetMemberAsync(cmd.CourseId, cmd.AuthorId, ct);
        var course = await uow.GetRepository<Course>().GetByIdAsync(cmd.CourseId, ct);
        var isTeacher = course?.TeacherId == cmd.AuthorId;

        if (member is null && !isTeacher)
            return ApiResponse<CommentDto>.Fail("You are not a member of this course.");

        var comment = AnnouncementComment.Create(cmd.AnnouncementId, cmd.AuthorId, content);
        await uow.GetRepository<AnnouncementComment>().AddAsync(comment, ct);
        await uow.SaveChangesAsync(ct);

        var author = await uow.Users.GetWithProfileAsync(cmd.AuthorId, ct);
        var authorName = author?.Profile?.FullName ?? "Someone";

        // Notify the announcement's author, and the teacher when somebody else
        // replies — a comment nobody is told about is a question nobody answers.
        var notifyIds = new HashSet<Guid> { announcement.AuthorId, course!.TeacherId };
        notifyIds.Remove(cmd.AuthorId); // never notify yourself about your own comment

        foreach (var userId in notifyIds)
        {
            await sender.Send(new SendNotificationCommand(
                UserId: userId,
                Title: $"New comment in {course.Title}",
                Body: $"{authorName}: {Preview(content)}",
                Type: NotificationType.NewComment,
                RedirectUrl: $"/courses/{cmd.CourseId}/stream"
            ), ct);
        }

        return ApiResponse<CommentDto>.Ok(new CommentDto(
            comment.Id,
            comment.AnnouncementId,
            comment.AuthorId,
            authorName,
            author?.Profile?.ProfilePhotoUrl,
            comment.Content,
            comment.CreatedAt,
            CanDelete: true,
            CanEdit: true), "Comment posted.");
    }

    /// <summary>Keeps the notification body to a glance, not a wall of text.</summary>
    private static string Preview(string text) =>
        text.Length <= 90 ? text : text[..87] + "...";
}
