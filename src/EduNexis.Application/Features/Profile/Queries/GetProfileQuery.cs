using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Profile.Queries;

public record GetProfileQuery(Guid UserId) : IQuery<ApiResponse<UserProfileDto>>;

public sealed class GetProfileQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetProfileQuery, ApiResponse<UserProfileDto>>
{
    public async ValueTask<ApiResponse<UserProfileDto>> Handle(
        GetProfileQuery query, CancellationToken ct)
    {
        var profile = await uow.UserProfiles.GetByUserIdAsync(query.UserId, ct)
            ?? throw new NotFoundException("UserProfile", query.UserId);

        return ApiResponse<UserProfileDto>.Ok(new UserProfileDto(
            profile.Id, profile.FullName, profile.Department,
            profile.Designation, profile.StudentId, profile.Bio,
            profile.ProfilePhotoUrl, profile.PhoneNumber,
            profile.LinkedInUrl, profile.ProfileCompletionPercent));
    }
}
