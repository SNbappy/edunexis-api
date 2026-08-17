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
    string Content
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
        var isTeacher = course?.TeacherId == cmd.AuthorId;

        if (member is null && !isTeacher)
            return ApiResponse<CommentDto>.Fail("You are not a member of this course.");

        var comment = AssignmentComment.Create(cmd.AssignmentId, cmd.AuthorId, content);
        await uow.GetRepository<AssignmentComment>().AddAsync(comment, ct);
        await uow.SaveChangesAsync(ct);

        var author = await uow.Users.GetWithProfileAsync(cmd.AuthorId, ct);
        var authorName = author?.Profile?.FullName ?? "Someone";

        // A question under an assignment is usually for the teacher, so they are
        // told; a student is told when the teacher replies on their thread.
        if (!isTeacher && course is not null)
        {
            await sender.Send(new SendNotificationCommand(
                UserId: course.TeacherId,
                Title: $"New comment in {course.Title}",
                Body: $"{authorName} on \"{assignment.Title}\": {Preview(content)}",
                Type: NotificationType.NewComment,
                RedirectUrl: $"/courses/{cmd.CourseId}/assignments/{cmd.AssignmentId}"
            ), ct);
        }

        return ApiResponse<CommentDto>.Ok(new CommentDto(
            comment.Id, comment.AssignmentId, comment.AuthorId,
            authorName, author?.Profile?.ProfilePhotoUrl,
            comment.Content, comment.CreatedAt,
            CanDelete: true, CanEdit: true), "Comment posted.");
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
            CanDelete: true, CanEdit: true, EditedAt: comment.UpdatedAt), "Comment updated.");
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
        var isTeacher = course?.TeacherId == cmd.RequestedById;

        // The teacher moderates their own class; everyone else, own comments only.
        if (comment.AuthorId != cmd.RequestedById && !isTeacher)
            return ApiResponse.Fail("You can only delete your own comments.");

        comment.Delete();
        uow.GetRepository<AssignmentComment>().Update(comment);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Comment deleted.");
    }
}
