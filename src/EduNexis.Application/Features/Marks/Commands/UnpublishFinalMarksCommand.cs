using EduNexis.Application.Abstractions;

namespace EduNexis.Application.Features.Marks.Commands;

public record UnpublishFinalMarksCommand(
    Guid CourseId,
    Guid TeacherId
) : ICommand<ApiResponse>, ICourseScopedWrite
{
    public ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
        => ValueTask.FromResult<Guid?>(CourseId);
}

public sealed class UnpublishFinalMarksCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<UnpublishFinalMarksCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        UnpublishFinalMarksCommand command, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(command.CourseId, ct)
            ?? throw new NotFoundException("Course", command.CourseId);

        if (!await CourseAccess.IsTeacherAsync(uow, course, command.TeacherId, ct))
            throw new UnauthorizedException("Only the teacher can unpublish marks.");

        var finalMarks = await uow.GetRepository<FinalMark>()
            .FindAsync(fm => fm.CourseId == command.CourseId, ct);

        var markList = finalMarks.ToList();
        if (markList.Count == 0)
            return ApiResponse.Fail("No final marks found to unpublish.");

        foreach (var mark in markList)
            mark.Unpublish();

        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Final marks unpublished successfully.");
    }
}