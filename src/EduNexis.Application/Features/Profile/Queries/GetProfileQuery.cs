using EduNexis.Application.DTOs;
using EduNexis.Application.Features.Profile.Commands;

namespace EduNexis.Application.Features.Profile.Queries;

public record GetProfileQuery(Guid UserId) : IQuery<ApiResponse<UserProfileDto>>;

public sealed class GetProfileQueryHandler(IUnitOfWork uow)
    : IQueryHandler<GetProfileQuery, ApiResponse<UserProfileDto>>
{
    public async ValueTask<ApiResponse<UserProfileDto>> Handle(
        GetProfileQuery query, CancellationToken ct)
    {
        var profile = await uow.UserProfiles.GetByUserIdAsync(query.UserId, ct)
            ?? throw new NotFoundException("UserProfile", query.UserId);

        return ApiResponse<UserProfileDto>.Ok(UpdateProfileCommandHandler.MapToDto(profile));
    }
}
