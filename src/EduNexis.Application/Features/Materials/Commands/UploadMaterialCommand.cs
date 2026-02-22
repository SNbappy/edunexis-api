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
    string? Category
) : ICommand<ApiResponse>;

public sealed class UploadMaterialCommandValidator : AbstractValidator<UploadMaterialCommand>
{
    public UploadMaterialCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.FileStream)
            .NotNull().When(x => x.Type == MaterialType.File)
            .WithMessage("File is required for File type materials.");
        RuleFor(x => x.EmbedUrl)
            .NotEmpty().When(x => x.Type != MaterialType.File)
            .WithMessage("Embed URL is required for YouTube/GoogleDrive materials.");
    }
}

public sealed class UploadMaterialCommandHandler(
    IUnitOfWork uow,
    IFileStorageService storage
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
        if (command.Type == MaterialType.File &&
            command.FileStream is not null && command.FileName is not null)
        {
            fileUrl = await storage.UploadAsync(
                command.FileStream, command.FileName,
                $"materials/{command.CourseId}", ct);
        }

        var material = Material.Create(
            command.CourseId, command.Title, command.Type,
            fileUrl, command.EmbedUrl, null,
            command.Description, command.Category, command.UploadedById);

        await uow.GetRepository<Material>().AddAsync(material, ct);
        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok("Material uploaded successfully.");
    }
}
