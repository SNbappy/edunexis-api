namespace EduNexis.Application.Features.Announcements.Commands;

public record AnnouncementDto(
    Guid Id, Guid CourseId, Guid AuthorId,
    string AuthorName, string Content,
    string? AttachmentUrl, DateTime CreatedAt
);

public record CreateAnnouncementCommand(
    Guid CourseId,
    Guid AuthorId,
    string Content,
    Stream? AttachmentStream,
    string? AttachmentFileName
) : ICommand<ApiResponse<AnnouncementDto>>;

public sealed class CreateAnnouncementCommandValidator : AbstractValidator<CreateAnnouncementCommand>
{
    public CreateAnnouncementCommandValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
    }
}

public sealed class CreateAnnouncementCommandHandler(
    IUnitOfWork uow,
    IFileStorageService storage
) : ICommandHandler<CreateAnnouncementCommand, ApiResponse<AnnouncementDto>>
{
    public async ValueTask<ApiResponse<AnnouncementDto>> Handle(
        CreateAnnouncementCommand command, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(command.CourseId, ct)
            ?? throw new NotFoundException("Course", command.CourseId);

        bool isTeacher = course.TeacherId == command.AuthorId;
        var member = await uow.CourseMembers.GetMemberAsync(course.Id, command.AuthorId, ct);
        bool isCR = member?.IsCR ?? false;

        if (!isTeacher && !isCR)
            throw new UnauthorizedException("Only teacher or CR can post announcements.");

        string? attachmentUrl = null;
        if (command.AttachmentStream is not null && command.AttachmentFileName is not null)
        {
            attachmentUrl = await storage.UploadAsync(
                command.AttachmentStream, command.AttachmentFileName,
                $"announcements/{command.CourseId}", ct);
        }

        var announcement = Announcement.Create(
            command.CourseId, command.AuthorId, command.Content, attachmentUrl);

        await uow.GetRepository<Announcement>().AddAsync(announcement, ct);
        await uow.SaveChangesAsync(ct);

        var author = await uow.Users.GetWithProfileAsync(command.AuthorId, ct);

        return ApiResponse<AnnouncementDto>.Ok(new AnnouncementDto(
            announcement.Id, announcement.CourseId, announcement.AuthorId,
            author?.Profile?.FullName ?? "Unknown",
            announcement.Content, announcement.AttachmentUrl, announcement.CreatedAt));
    }
}
