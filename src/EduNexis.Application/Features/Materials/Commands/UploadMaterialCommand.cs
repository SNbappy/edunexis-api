using EduNexis.Application.Features.Notifications.Commands;

namespace EduNexis.Application.Features.Materials.Commands;

public record UploadMaterialCommand(
    Guid CourseId,
    Guid UploadedById,
    string Title,
    MaterialType Type,
    Stream? FileStream,
    string? FileName,
    string? EmbedUrl,
    string? Description,
    string? Category,
    Guid? ParentFolderId
) : ICommand<ApiResponse>;

public sealed class UploadMaterialCommandValidator : AbstractValidator<UploadMaterialCommand>
{
    public UploadMaterialCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FileStream)
            .NotNull().When(x => x.Type == MaterialType.File)
            .WithMessage("File is required for File type materials.");
        // Every link-shaped material needs a URL, not just Link — YouTube and
        // GoogleDrive are stored the same way and were previously accepted
        // with an empty EmbedUrl, producing a material that pointed nowhere.
        RuleFor(x => x.EmbedUrl)
            .NotEmpty()
            .When(x => x.Type is MaterialType.Link
                              or MaterialType.YouTube
                              or MaterialType.GoogleDrive)
            .WithMessage("A URL is required for link, YouTube and Drive materials.");
    }
}

public sealed class UploadMaterialCommandHandler(
    IUnitOfWork uow,
    IFileStorageService storage,
    ISender sender
) : ICommandHandler<UploadMaterialCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        UploadMaterialCommand command, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(command.CourseId, ct)
            ?? throw new NotFoundException("Course", command.CourseId);

        bool isTeacher = course.TeacherId == command.UploadedById;
        var member = await uow.CourseMembers.GetMemberAsync(course.Id, command.UploadedById, ct);
        bool isCR = member?.IsCR ?? false;

        if (!isTeacher && !isCR)
            throw new UnauthorizedException("Only teacher or CR can upload materials.");

        string? fileUrl = null;
        long? fileSizeBytes = null;

        if (command.Type == MaterialType.File &&
            command.FileStream is not null && command.FileName is not null)
        {
            fileSizeBytes = command.FileStream.CanSeek ? command.FileStream.Length : null;
            fileUrl = await storage.UploadAsync(
                command.FileStream, command.FileName,
                "materials/" + command.CourseId, ct);
        }

        var material = Material.Create(
            command.CourseId, command.Title, command.Type,
            fileUrl, command.FileName, fileSizeBytes,
            command.EmbedUrl, null,
            command.Description, command.Category,
            command.UploadedById, command.ParentFolderId);

        await uow.GetRepository<Material>().AddAsync(material, ct);
        await uow.SaveChangesAsync(ct);

        var members = await uow.CourseMembers.GetByCourseAsync(command.CourseId, ct);
        foreach (var m in members.Where(x => x.IsActive && x.UserId != command.UploadedById))
        {
            await sender.Send(new SendNotificationCommand(
                UserId: m.UserId,
                Title: $"New Material in {course.Title}",
                Body: $"\"{command.Title}\" has been uploaded.",
                Type: NotificationType.NewMaterial,
                RedirectUrl: $"/courses/{course.Id}/materials"
            ), ct);
        }

        return ApiResponse.Ok("Material uploaded successfully.");
    }
}
