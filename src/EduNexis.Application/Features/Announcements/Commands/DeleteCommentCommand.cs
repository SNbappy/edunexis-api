using EduNexis.Application.Abstractions;

namespace EduNexis.Application.Features.Announcements.Commands;

/// <summary>
/// Removes a class comment.
///
/// Allowed for the comment's author and for the course teacher — the author so
/// a misfired comment can be taken back, the teacher so they can moderate their
/// own class. Nobody else, including other students in the course.
/// </summary>
public record DeleteCommentCommand(
    Guid CourseId,
    Guid CommentId,
    Guid RequestedById
) : ICommand<ApiResponse>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public sealed class DeleteCommentCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<DeleteCommentCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        DeleteCommentCommand cmd, CancellationToken ct)
    {
        var comment = await uow.GetRepository<AnnouncementComment>()
            .GetByIdAsync(cmd.CommentId, ct);

        if (comment is null || comment.IsDeleted)
            return ApiResponse.Fail("Comment not found.");

        var announcement = await uow.GetRepository<Announcement>()
            .GetByIdAsync(comment.AnnouncementId, ct);

        // Confirm the comment really lives in the course named in the route.
        if (announcement is null || announcement.CourseId != cmd.CourseId)
            return ApiResponse.Fail("Comment not found.");

        var course = await uow.GetRepository<Course>().GetByIdAsync(cmd.CourseId, ct);
        var isTeacher = course?.TeacherId == cmd.RequestedById;
        var isAuthor  = comment.AuthorId == cmd.RequestedById;

        if (!isAuthor && !isTeacher)
            return ApiResponse.Fail("You can only delete your own comments.");

        comment.Delete();
        uow.GetRepository<AnnouncementComment>().Update(comment);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Comment deleted.");
    }
}
