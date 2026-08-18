using EduNexis.Application.Abstractions;
﻿namespace EduNexis.Application.Features.CT.Commands;

public record UnpublishCTCommand(
    Guid CTEventId,
    Guid TeacherId
) : ICommand<ApiResponse>, ICourseScopedWrite
{
    public async ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
    {
        var e = await uow.GetRepository<CTEvent>().GetByIdAsync(CTEventId, ct);
        return e?.CourseId;
    }
}

public sealed class UnpublishCTCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<UnpublishCTCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        UnpublishCTCommand command, CancellationToken ct)
    {
        var ctEvent = await uow.GetRepository<CTEvent>().GetByIdAsync(command.CTEventId, ct)
            ?? throw new NotFoundException("CTEvent", command.CTEventId);

        var course = await uow.Courses.GetByIdAsync(ctEvent.CourseId, ct)
            ?? throw new NotFoundException("Course", ctEvent.CourseId);

        if (!await CourseAccess.IsTeacherAsync(uow, course, command.TeacherId, ct))
            return ApiResponse.Fail("Only the teacher can unpublish CT events.");

        if (ctEvent.Status != Domain.Enums.CTStatus.Published)
            return ApiResponse.Fail("CT is not published.");

        ctEvent.Unpublish();
        uow.GetRepository<CTEvent>().Update(ctEvent);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("CT unpublished successfully.");
    }
}
