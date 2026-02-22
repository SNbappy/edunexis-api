namespace EduNexis.Application.Features.CT.Commands;

public record UploadCTCopiesCommand(
    Guid CTEventId,
    Guid StudentId,
    Stream? BestCopyStream,
    string? BestCopyFileName,
    Stream? WorstCopyStream,
    string? WorstCopyFileName,
    Stream? AvgCopyStream,
    string? AvgCopyFileName
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

        var submission = await uow.GetRepository<CTSubmission>()
            .FirstOrDefaultAsync(s =>
                s.CTEventId == command.CTEventId &&
                s.StudentId == command.StudentId, ct)
            ?? CTSubmission.Create(command.CTEventId, command.StudentId);

        var folder = $"ct/{command.CTEventId}/{command.StudentId}";

        string? bestUrl = null, worstUrl = null, avgUrl = null;

        if (command.BestCopyStream is not null && command.BestCopyFileName is not null)
            bestUrl = await storage.UploadAsync(command.BestCopyStream, command.BestCopyFileName, folder, ct);

        if (command.WorstCopyStream is not null && command.WorstCopyFileName is not null)
            worstUrl = await storage.UploadAsync(command.WorstCopyStream, command.WorstCopyFileName, folder, ct);

        if (command.AvgCopyStream is not null && command.AvgCopyFileName is not null)
            avgUrl = await storage.UploadAsync(command.AvgCopyStream, command.AvgCopyFileName, folder, ct);

        submission.UploadCopies(bestUrl, worstUrl, avgUrl);

        if (submission.Id == Guid.Empty)
            await uow.GetRepository<CTSubmission>().AddAsync(submission, ct);
        else
            uow.GetRepository<CTSubmission>().Update(submission);

        await uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("CT copies uploaded successfully.");
    }
}
