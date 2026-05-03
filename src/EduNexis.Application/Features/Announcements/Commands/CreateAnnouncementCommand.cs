using EduNexis.Application.Features.Notifications.Commands;

namespace EduNexis.Application.Features.Announcements.Commands;

public record AnnouncementDto(
    Guid Id, Guid CourseId, Guid AuthorId,
    string AuthorName, string Content,
    string? AttachmentUrl, bool IsPinned, DateTime CreatedAt
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
    IFileStorageService storage,
    ISender sender
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
        var authorName = author?.Profile?.FullName ?? "Someone";

        var members = await uow.CourseMembers.GetByCourseAsync(command.CourseId, ct);
        foreach (var m in members.Where(x => x.IsActive && x.UserId != command.AuthorId))
        {
            await sender.Send(new SendNotificationCommand(
                UserId: m.UserId,
                Title: $"New Announcement in {course.Title}",
                Body: $"{authorName}: {command.Content[..Math.Min(80, command.Content.Length)]}...",
                Type: NotificationType.NewAnnouncement,
                RedirectUrl: $"/courses/{course.Id}/stream"
            ), ct);
        }

        return ApiResponse<AnnouncementDto>.Ok(new AnnouncementDto(
            announcement.Id, announcement.CourseId, announcement.AuthorId,
            authorName, announcement.Content, announcement.AttachmentUrl,
            announcement.IsPinned, announcement.CreatedAt));
    }
}
