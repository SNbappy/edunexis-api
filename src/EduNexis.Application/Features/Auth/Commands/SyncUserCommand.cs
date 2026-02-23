using EduNexis.Application.DTOs;
using EduNexis.Domain.Interfaces.Services;

namespace EduNexis.Application.Features.Auth.Commands;

public record SyncUserCommand(
    string Email,
    string FullName
) : ICommand<ApiResponse<UserDto>>;

public sealed class SyncUserCommandValidator : AbstractValidator<SyncUserCommand>
{
    public SyncUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress();
        RuleFor(x => x.FullName)
            .NotEmpty().MinimumLength(2);
    }
}

public sealed class SyncUserCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<SyncUserCommand, ApiResponse<UserDto>>
{
    public async ValueTask<ApiResponse<UserDto>> Handle(
        SyncUserCommand command, CancellationToken ct)
    {
        var user = await uow.Users.GetByEmailAsync(command.Email, ct);
        if (user is null)
            return ApiResponse<UserDto>.Fail("User not found. Please register first.");

        var profile = await uow.UserProfiles.GetByUserIdAsync(user.Id, ct)
                      ?? UserProfile.Create(user.Id, command.FullName);

        if (profile.FullName != command.FullName)
        {
            profile.Update(
                fullName: command.FullName,
                department: profile.Department,
                designation: profile.Designation,
                studentId: profile.StudentId,
                bio: profile.Bio,
                phoneNumber: profile.PhoneNumber,
                linkedInUrl: profile.LinkedInUrl);
        }

        await uow.SaveChangesAsync(ct);

        var dto = new UserDto(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.IsProfileComplete,
            new UserProfileDto(
                profile.Id,
                profile.FullName,
                profile.Department,
                profile.Designation,
                profile.StudentId,
                profile.Bio,
                profile.ProfilePhotoUrl,
                profile.PhoneNumber,
                profile.LinkedInUrl,
                profile.ProfileCompletionPercent));

        return ApiResponse<UserDto>.Ok(dto, "User synced successfully.");
    }
}
