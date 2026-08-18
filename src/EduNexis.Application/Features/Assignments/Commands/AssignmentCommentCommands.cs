using EduNexis.Application.Abstractions;
using EduNexis.Application.Features.Announcements.Commands;
using EduNexis.Application.Features.Notifications.Commands;

namespace EduNexis.Application.Features.Assignments.Commands;

/* Class comments on an assignment.
   Mirrors the announcement comment flow, and deliberately reuses its CommentDto
   so the client can render both threads with one component. */

// ── Add ──────────────────────────────────────────────────────────────

public record AddAssignmentCommentCommand(
    Guid CourseId,
    Guid AssignmentId,
    Guid AuthorId,
    string Content,
    /// <summary>Set to answer an existing comment in the same thread.</summary>
    Guid? ParentCommentId = null
) : ICommand<ApiResponse<CommentDto>>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public sealed class AddAssignmentCommentCommandValidator
    : AbstractValidator<AddAssignmentCommentCommand>
{
    public AddAssignmentCommentCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(1000);
    }
}

public sealed class AddAssignmentCommentCommandHandler(
    IUnitOfWork uow,
    ISender sender
) : ICommandHandler<AddAssignmentCommentCommand, ApiResponse<CommentDto>>
{
    public async ValueTask<ApiResponse<CommentDto>> Handle(
        AddAssignmentCommentCommand cmd, CancellationToken ct)
    {
        var content = cmd.Content?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
            return ApiResponse<CommentDto>.Fail("Comment cannot be empty.");
        if (content.Length > 1000)
            return ApiResponse<CommentDto>.Fail("Comment is too long (1000 characters max).");

        var assignment = await uow.GetRepository<Assignment>()
            .GetByIdAsync(cmd.AssignmentId, ct);

        // The assignment must belong to the course in the route, or a member of
        // course A could comment on course B's assignment by id.
        if (assignment is null || assignment.IsDeleted || assignment.CourseId != cmd.CourseId)
            return ApiResponse<CommentDto>.Fail("Assignment not found.");

        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct);
        var member = await uow.CourseMembers.GetMemberAsync(cmd.CourseId, cmd.AuthorId, ct);
        var isTeacher = await CourseAccess.IsTeacherAsync(uow, course, cmd.AuthorId, ct);

        if (member is null && !isTeacher)
            return ApiResponse<CommentDto>.Fail("You are not a member of this course.");

        // Same rules as the announcement thread: the parent must belong to this
        // assignment, and a reply to a reply is flattened onto the root.
        Guid? parentId = null;
        AssignmentComment? parent = null;

        if (cmd.ParentCommentId is Guid requestedParent)
        {
            parent = await uow.GetRepository<AssignmentComment>()
                .GetByIdAsync(requestedParent, ct);

            if (parent is null || parent.IsDeleted || parent.AssignmentId != cmd.AssignmentId)
                return ApiResponse<CommentDto>.Fail("The comment you replied to no longer exists.");

            parentId = parent.ParentCommentId ?? parent.Id;
        }

        var comment = AssignmentComment.Create(cmd.AssignmentId, cmd.AuthorId, content, parentId);
        await uow.GetRepository<AssignmentComment>().AddAsync(comment, ct);
        await uow.SaveChangesAsync(ct);

        var author = await uow.Users.GetWithProfileAsync(cmd.AuthorId, ct);
        var authorName = author?.Profile?.FullName ?? "Someone";

        // The teacher and everyone already in the thread. The previous version
        // notified the teacher only, and only when a student wrote — so when the
        // teacher finally answered, the student who asked was never told. The
        // comment above claimed otherwise; the code never did it.
        if (course is not null)
        {
            var priorAuthors = (await uow.GetRepository<AssignmentComment>()
                    .FindAsync(c => c.AssignmentId == cmd.AssignmentId && !c.IsDeleted, ct))
                .Select(c => c.AuthorId);

            var notifyIds = new HashSet<Guid>(priorAuthors) { course.TeacherId };
            if (parent is not null) notifyIds.Add(parent.AuthorId);
            notifyIds.Remove(cmd.AuthorId);

            var redirect =
                $"/courses/{cmd.CourseId}/assignments/{cmd.AssignmentId}#comment-{comment.Id}";

            foreach (var userId in notifyIds)
            {
                await sender.Send(new SendNotificationCommand(
                    UserId: userId,
                    Title: $"New comment in {course.Title}",
                    Body: parent is not null && userId == parent.AuthorId
                        ? $"{authorName} replied to you on \"{assignment.Title}\": {Preview(content)}"
                        : $"{authorName} on \"{assignment.Title}\": {Preview(content)}",
                    Type: NotificationType.NewComment,
                    RedirectUrl: redirect
                ), ct);
            }
        }

        return ApiResponse<CommentDto>.Ok(new CommentDto(
            comment.Id, comment.AssignmentId, comment.AuthorId,
            authorName, author?.Profile?.ProfilePhotoUrl,
            comment.Content, comment.CreatedAt,
            CanDelete: true, CanEdit: true,
            EditedAt: null,
            ParentCommentId: comment.ParentCommentId,
            ReplyToName: parent is null
                ? null
                : (await uow.Users.GetWithProfileAsync(parent.AuthorId, ct))?.Profile?.FullName),
            "Comment posted.");
    }

    private static string Preview(string text) =>
        text.Length <= 90 ? text : text[..87] + "...";
}

// ── Edit ─────────────────────────────────────────────────────────────

public record EditAssignmentCommentCommand(
    Guid CourseId,
    Guid CommentId,
    Guid RequestedById,
    string Content
) : ICommand<ApiResponse<CommentDto>>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public sealed class EditAssignmentCommentCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<EditAssignmentCommentCommand, ApiResponse<CommentDto>>
{
    public async ValueTask<ApiResponse<CommentDto>> Handle(
        EditAssignmentCommentCommand cmd, CancellationToken ct)
    {
        var content = cmd.Content?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
            return ApiResponse<CommentDto>.Fail("Comment cannot be empty.");

        var comment = await uow.GetRepository<AssignmentComment>()
            .GetByIdAsync(cmd.CommentId, ct);

        if (comment is null || comment.IsDeleted)
            return ApiResponse<CommentDto>.Fail("Comment not found.");

        var assignment = await uow.GetRepository<Assignment>()
            .GetByIdAsync(comment.AssignmentId, ct);

        if (assignment is null || assignment.CourseId != cmd.CourseId)
            return ApiResponse<CommentDto>.Fail("Comment not found.");

        // Authors only. A teacher moderates by deleting, never by rewriting.
        if (comment.AuthorId != cmd.RequestedById)
            return ApiResponse<CommentDto>.Fail("You can only edit your own comments.");

        comment.Edit(content);
        uow.GetRepository<AssignmentComment>().Update(comment);
        await uow.SaveChangesAsync(ct);

        var author = await uow.Users.GetWithProfileAsync(comment.AuthorId, ct);

        return ApiResponse<CommentDto>.Ok(new CommentDto(
            comment.Id, comment.AssignmentId, comment.AuthorId,
            author?.Profile?.FullName ?? "Unknown", author?.Profile?.ProfilePhotoUrl,
            comment.Content, comment.CreatedAt,
            CanDelete: true, CanEdit: true, EditedAt: comment.UpdatedAt,
            ParentCommentId: comment.ParentCommentId), "Comment updated.");
    }
}

// ── Delete ───────────────────────────────────────────────────────────

public record DeleteAssignmentCommentCommand(
    Guid CourseId,
    Guid CommentId,
    Guid RequestedById
) : ICommand<ApiResponse>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public sealed class DeleteAssignmentCommentCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<DeleteAssignmentCommentCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        DeleteAssignmentCommentCommand cmd, CancellationToken ct)
    {
        var comment = await uow.GetRepository<AssignmentComment>()
            .GetByIdAsync(cmd.CommentId, ct);

        if (comment is null || comment.IsDeleted)
            return ApiResponse.Fail("Comment not found.");

        var assignment = await uow.GetRepository<Assignment>()
            .GetByIdAsync(comment.AssignmentId, ct);

        if (assignment is null || assignment.CourseId != cmd.CourseId)
            return ApiResponse.Fail("Comment not found.");

        var course = await uow.Courses.GetByIdAsync(cmd.CourseId, ct);
        var isTeacher = await CourseAccess.IsTeacherAsync(uow, course, cmd.RequestedById, ct);

        // The teacher moderates their own class; everyone else, own comments only.
        if (comment.AuthorId != cmd.RequestedById && !isTeacher)
            return ApiResponse.Fail("You can only delete your own comments.");

        comment.Delete();
        uow.GetRepository<AssignmentComment>().Update(comment);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Comment deleted.");
    }
}
