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
) : ICommand<ApiResponse>;

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

        if (course.TeacherId != command.TeacherId)
            return ApiResponse.Fail("Only the teacher can upload khata.");

        var folder = $"ct/{command.CTEventId}/khata";

        var bestUrl  = command.BestCopyStream  != null ? await storage.UploadAsync(command.BestCopyStream,  command.BestCopyFileName!,  folder, ct) : ctEvent.BestScriptUrl;
        var worstUrl = command.WorstCopyStream != null ? await storage.UploadAsync(command.WorstCopyStream, command.WorstCopyFileName!, folder, ct) : ctEvent.WorstScriptUrl;
        var avgUrl   = command.AvgCopyStream   != null ? await storage.UploadAsync(command.AvgCopyStream,   command.AvgCopyFileName!,   folder, ct) : ctEvent.AverageScriptUrl;

        ctEvent.UploadKhata(
            bestUrl ?? string.Empty,  command.BestCopyStream  != null ? command.BestStudentId  : ctEvent.BestStudentId,
            worstUrl ?? string.Empty, command.WorstCopyStream != null ? command.WorstStudentId : ctEvent.WorstStudentId,
            avgUrl ?? string.Empty,   command.AvgCopyStream   != null ? command.AvgStudentId   : ctEvent.AverageStudentId);

        uow.GetRepository<CTEvent>().Update(ctEvent);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("CT khata uploaded successfully.");
    }
}



