using EduNexis.Application.DTOs;
using EduNexis.Application.Features.Profile.Commands;

namespace EduNexis.Application.Features.Auth.Queries;

public record GetCurrentUserQuery(Guid UserId) : IQuery<ApiResponse<UserDto>>;

public sealed class GetCurrentUserQueryHandler(IUnitOfWork uow) : IQueryHandler<GetCurrentUserQuery, ApiResponse<UserDto>>
{
    public async ValueTask<ApiResponse<UserDto>> Handle(GetCurrentUserQuery query, CancellationToken ct)
    {
        var user = await uow.Users.GetWithProfileAsync(query.UserId, ct)
            ?? throw new NotFoundException("User", query.UserId);

        var profile = user.Profile ?? UserProfile.Create(user.Id);

        return ApiResponse<UserDto>.Ok(new UserDto(
            user.Id, user.Email, user.Role.ToString(), user.IsProfileComplete,
            UpdateProfileCommandHandler.MapToDto(profile)));
    }
}
