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
        var isNew = settings is null;

        if (settings is null)
        {
            settings = PlatformSetting.CreateDefault();
            await repo.AddAsync(settings, ct);
        }

        settings.SetCourseQuotaEnforced(cmd.CourseQuotaEnforced, adminId);

        // Only mark Modified for a row that already exists. Calling Update() on
        // a freshly Added entity flips its state from Added to Modified, so EF
        // emitted an UPDATE against a row that was never inserted — 0 rows
        // affected, DbUpdateConcurrencyException, 500. Because this method is
        // the only thing that creates the settings row, the very first toggle
        // always failed and the switch could never be turned on.
        if (!isNew)
            repo.Update(settings);

        await uow.SaveChangesAsync(ct);

        var message = cmd.CourseQuotaEnforced
            ? "Course creation quota is now enforced. Teachers are limited to 1 free course unless granted more."
            : "Course creation quota is now disabled. All teachers can create unlimited courses.";

        return ApiResponse.Ok(message);
    }
}