using EduNexis.Domain.Entities;

namespace EduNexis.Application.Features.Admin.Queries;

public record PlatformSettingsDto(bool CourseQuotaEnforced);

public record GetPlatformSettingsQuery : IQuery<ApiResponse<PlatformSettingsDto>>;

public sealed class GetPlatformSettingsQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetPlatformSettingsQuery, ApiResponse<PlatformSettingsDto>>
{
    public async ValueTask<ApiResponse<PlatformSettingsDto>> Handle(
        GetPlatformSettingsQuery query, CancellationToken ct)
    {
        var settings = (await uow.GetRepository<PlatformSetting>().GetAllAsync(ct)).FirstOrDefault();

        // Should always exist (seeded via migration), but default safe if somehow missing.
        var enforced = settings?.CourseQuotaEnforced ?? false;

        return ApiResponse<PlatformSettingsDto>.Ok(new PlatformSettingsDto(enforced));
    }
}