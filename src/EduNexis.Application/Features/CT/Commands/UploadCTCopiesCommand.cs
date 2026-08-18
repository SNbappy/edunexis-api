using EduNexis.Application.Abstractions;
namespace EduNexis.Application.Features.CT.Commands;

public record UploadCTCopiesCommand(
    Guid CTEventId,
    Guid TeacherId,
    Stream? BestCopyStream,
    string? BestCopyFileName,
    Guid? BestStudentId,
    Stream? WorstCopyStream,
    string? WorstCopyFileName,
    Guid? WorstStudentId,
    Stream? AvgCopyStream,
    string? AvgCopyFileName,
    Guid? AvgStudentId
) : ICommand<ApiResponse>, ICourseScopedWrite
{
    public async ValueTask<Guid?> ResolveCourseIdAsync(IUnitOfWork uow, CancellationToken ct)
    {
        var e = await uow.GetRepository<CTEvent>().GetByIdAsync(CTEventId, ct);
        return e?.CourseId;
    }
}

public sealed class UploadCTCopiesCommandHandler(
    IUnitOfWork uow,
    IFileStorageService storage
) : ICommandHandler<UploadCTCopiesCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        UploadCTCopiesCommand command, CancellationToken ct)
    {
        var ctEvent = await uow.GetRepository<CTEvent>().GetByIdAsync(command.CTEventId, ct)
            ?? throw new NotFoundException("CTEvent", command.CTEventId);

        var course = await uow.Courses.GetByIdAsync(ctEvent.CourseId, ct)
            ?? throw new NotFoundException("Course", ctEvent.CourseId);

        if (!await CourseAccess.IsTeacherAsync(uow, course, command.TeacherId, ct))
            return ApiResponse.Fail("Only the teacher can upload khata.");

        var folder = $"ct/{command.CTEventId}/khata";

        var bestTask = command.BestCopyStream != null
            ? storage.UploadAsync(command.BestCopyStream, command.BestCopyFileName!, folder, ct)
            : Task.FromResult(ctEvent.BestScriptUrl);

        var worstTask = command.WorstCopyStream != null
            ? storage.UploadAsync(command.WorstCopyStream, command.WorstCopyFileName!, folder, ct)
            : Task.FromResult(ctEvent.WorstScriptUrl);

        var avgTask = command.AvgCopyStream != null
            ? storage.UploadAsync(command.AvgCopyStream, command.AvgCopyFileName!, folder, ct)
            : Task.FromResult(ctEvent.AverageScriptUrl);

        await Task.WhenAll(bestTask, worstTask, avgTask);

        var bestUrl = await bestTask;
        var worstUrl = await worstTask;
        var avgUrl = await avgTask;

        ctEvent.UploadKhata(
            bestUrl ?? string.Empty,  command.BestCopyStream  != null ? command.BestStudentId  : ctEvent.BestStudentId,
            worstUrl ?? string.Empty, command.WorstCopyStream != null ? command.WorstStudentId : ctEvent.WorstStudentId,
            avgUrl ?? string.Empty,   command.AvgCopyStream   != null ? command.AvgStudentId   : ctEvent.AverageStudentId);

        uow.GetRepository<CTEvent>().Update(ctEvent);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("CT khata uploaded successfully.");
    }
}



