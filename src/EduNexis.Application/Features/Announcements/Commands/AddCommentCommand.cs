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
    /// <summary>Whether the caller may delete this comment.</summary>
    bool CanDelete
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
    IUnitOfWork uow
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

        return ApiResponse<CommentDto>.Ok(new CommentDto(
            comment.Id,
            comment.AnnouncementId,
            comment.AuthorId,
            author?.Profile?.FullName ?? "Unknown",
            author?.Profile?.ProfilePhotoUrl,
            comment.Content,
            comment.CreatedAt,
            CanDelete: true), "Comment posted.");
    }
}
