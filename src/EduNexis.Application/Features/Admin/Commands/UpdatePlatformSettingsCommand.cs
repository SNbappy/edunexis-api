using EduNexis.Domain.Entities;

namespace EduNexis.Application.Features.Admin.Commands;

public record UpdatePlatformSettingsCommand(bool CourseQuotaEnforced) : ICommand<ApiResponse>;

public sealed class UpdatePlatformSettingsCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser
) : ICommandHandler<UpdatePlatformSettingsCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        UpdatePlatformSettingsCommand cmd, CancellationToken ct)
    {
        var adminId = Guid.Parse(currentUser.UserId);
        var repo = uow.GetRepository<PlatformSetting>();

        var settings = (await repo.GetAllAsync(ct)).FirstOrDefault();
        if (settings is null)
        {
            settings = PlatformSetting.CreateDefault();
            await repo.AddAsync(settings, ct);
        }

        settings.SetCourseQuotaEnforced(cmd.CourseQuotaEnforced, adminId);
        repo.Update(settings);
        await uow.SaveChangesAsync(ct);

        var message = cmd.CourseQuotaEnforced
            ? "Course creation quota is now enforced. Teachers are limited to 1 free course unless granted more."
            : "Course creation quota is now disabled. All teachers can create unlimited courses.";

        return ApiResponse.Ok(message);
    }
}