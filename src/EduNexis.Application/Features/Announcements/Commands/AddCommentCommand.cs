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
    DateTime? EditedAt = null,
    /// <summary>
    /// The comment this one answers, or null at the top level. Threads are one
    /// level deep, so a client can group by this field without recursing.
    /// </summary>
    Guid? ParentCommentId = null,
    /// <summary>Who is being answered, so a reply reads as a reply.</summary>
    string? ReplyToName = null
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
    string Content,
    /// <summary>Set to answer an existing comment in the same thread.</summary>
    Guid? ParentCommentId = null
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
        var isTeacher = await CourseAccess.IsTeacherAsync(uow, course, cmd.AuthorId, ct);

        if (member is null && !isTeacher)
            return ApiResponse<CommentDto>.Fail("You are not a member of this course.");

        // Resolve the reply target before writing anything.
        //
        // A parent from another announcement would put a reply under a thread it
        // was never written for, and replying to a reply is flattened onto the
        // root rather than rejected — the writer's intent is clear, and the UI
        // only ever draws one level.
        Guid? parentId = null;
        AnnouncementComment? parent = null;

        if (cmd.ParentCommentId is Guid requestedParent)
        {
            parent = await uow.GetRepository<AnnouncementComment>()
                .GetByIdAsync(requestedParent, ct);

            if (parent is null || parent.IsDeleted || parent.AnnouncementId != cmd.AnnouncementId)
                return ApiResponse<CommentDto>.Fail("The comment you replied to no longer exists.");

            parentId = parent.ParentCommentId ?? parent.Id;
        }

        var comment = AnnouncementComment.Create(cmd.AnnouncementId, cmd.AuthorId, content, parentId);
        await uow.GetRepository<AnnouncementComment>().AddAsync(comment, ct);
        await uow.SaveChangesAsync(ct);

        var author = await uow.Users.GetWithProfileAsync(cmd.AuthorId, ct);
        var authorName = author?.Profile?.FullName ?? "Someone";

        // Who hears about this: the announcement's author, the course teacher,
        // and everyone already in the thread. Only notifying the author and the
        // teacher meant a student who asked a question was never told when
        // somebody answered it — the thread was one-way for them.
        var priorAuthors = (await uow.GetRepository<AnnouncementComment>()
                .FindAsync(c => c.AnnouncementId == cmd.AnnouncementId && !c.IsDeleted, ct))
            .Select(c => c.AuthorId);

        var notifyIds = new HashSet<Guid>(priorAuthors) { announcement.AuthorId, course!.TeacherId };
        if (parent is not null) notifyIds.Add(parent.AuthorId);
        notifyIds.Remove(cmd.AuthorId); // never notify yourself about your own comment

        // Deep link straight to the comment rather than the top of the stream.
        // The stream can be dozens of posts long; "go read the whole feed" is
        // not a useful answer to "somebody replied to you".
        var redirect =
            $"/courses/{cmd.CourseId}/stream?announcement={cmd.AnnouncementId}#comment-{comment.Id}";

        foreach (var userId in notifyIds)
        {
            await sender.Send(new SendNotificationCommand(
                UserId: userId,
                Title: $"New comment in {course.Title}",
                Body: parent is not null && userId == parent.AuthorId
                    ? $"{authorName} replied to you: {Preview(content)}"
                    : $"{authorName}: {Preview(content)}",
                Type: NotificationType.NewComment,
                RedirectUrl: redirect
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
            CanEdit: true,
            EditedAt: null,
            ParentCommentId: comment.ParentCommentId,
            ReplyToName: parent is null
                ? null
                : (await uow.Users.GetWithProfileAsync(parent.AuthorId, ct))?.Profile?.FullName),
            "Comment posted.");
    }

    /// <summary>Keeps the notification body to a glance, not a wall of text.</summary>
    private static string Preview(string text) =>
        text.Length <= 90 ? text : text[..87] + "...";
}
