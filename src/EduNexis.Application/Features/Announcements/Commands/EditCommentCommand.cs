using EduNexis.Application.Abstractions;

namespace EduNexis.Application.Features.Announcements.Commands;

/// <summary>
/// Rewrites your own class comment.
///
/// Author-only, deliberately. A teacher can moderate a thread by deleting a
/// student's comment (see DeleteCommentCommand) but must never be able to edit
/// one — words shown under a student's name have to stay that student's words.
/// </summary>
public record EditCommentCommand(
    Guid CourseId,
    Guid CommentId,
    Guid RequestedById,
    string Content
) : ICommand<ApiResponse<CommentDto>>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public sealed class EditCommentCommandValidator : AbstractValidator<EditCommentCommand>
{
    public EditCommentCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(1000);
    }
}

public sealed class EditCommentCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<EditCommentCommand, ApiResponse<CommentDto>>
{
    public async ValueTask<ApiResponse<CommentDto>> Handle(
        EditCommentCommand cmd, CancellationToken ct)
    {
        var content = cmd.Content?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
            return ApiResponse<CommentDto>.Fail("Comment cannot be empty.");
        if (content.Length > 1000)
            return ApiResponse<CommentDto>.Fail("Comment is too long (1000 characters max).");

        var comment = await uow.GetRepository<AnnouncementComment>()
            .GetByIdAsync(cmd.CommentId, ct);

        if (comment is null || comment.IsDeleted)
            return ApiResponse<CommentDto>.Fail("Comment not found.");

        var announcement = await uow.GetRepository<Announcement>()
            .GetByIdAsync(comment.AnnouncementId, ct);

        // The comment must really belong to the course named in the route.
        if (announcement is null || announcement.CourseId != cmd.CourseId)
            return ApiResponse<CommentDto>.Fail("Comment not found.");

        if (comment.AuthorId != cmd.RequestedById)
            return ApiResponse<CommentDto>.Fail("You can only edit your own comments.");

        comment.Edit(content);
        uow.GetRepository<AnnouncementComment>().Update(comment);
        await uow.SaveChangesAsync(ct);

        var author = await uow.Users.GetWithProfileAsync(comment.AuthorId, ct);

        return ApiResponse<CommentDto>.Ok(new CommentDto(
            comment.Id,
            comment.AnnouncementId,
            comment.AuthorId,
            author?.Profile?.FullName ?? "Unknown",
            author?.Profile?.ProfilePhotoUrl,
            comment.Content,
            comment.CreatedAt,
            CanDelete: true,
            CanEdit: true,
            EditedAt: comment.UpdatedAt,
            ParentCommentId: comment.ParentCommentId), "Comment updated.");
    }
}
