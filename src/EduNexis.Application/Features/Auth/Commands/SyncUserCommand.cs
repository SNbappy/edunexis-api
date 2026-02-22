using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Auth.Commands;

public record SyncUserCommand(string FirebaseToken) : ICommand<ApiResponse<UserDto>>;

public sealed class SyncUserCommandValidator : AbstractValidator<SyncUserCommand>
{
    public SyncUserCommandValidator()
    {
        RuleFor(x => x.FirebaseToken)
            .NotEmpty().WithMessage("Firebase token is required.");
    }
}

public sealed class SyncUserCommandHandler(
    IUnitOfWork uow,
    IFirebaseAuthService firebaseAuth
) : ICommandHandler<SyncUserCommand, ApiResponse<UserDto>>
{
    public async ValueTask<ApiResponse<UserDto>> Handle(
        SyncUserCommand command, CancellationToken ct)
    {
        var payload = await firebaseAuth.VerifyTokenAsync(command.FirebaseToken, ct)
            ?? throw new UnauthorizedException("Invalid Firebase token.");

        var user = await uow.Users.GetByFirebaseUidAsync(payload.Uid, ct)
            ?? throw new NotFoundException("User", payload.Uid);

        if (!user.IsActive)
            throw new UnauthorizedException("Your account has been deactivated.");

        var userWithProfile = await uow.Users.GetWithProfileAsync(user.Id, ct)
            ?? throw new NotFoundException("User", user.Id);

        var profile = userWithProfile.Profile ?? UserProfile.Create(user.Id);

        return ApiResponse<UserDto>.Ok(MapToDto(userWithProfile, profile));
    }

    private static UserDto MapToDto(User user, UserProfile profile) =>
        new(user.Id, user.Email, user.Role.ToString(), user.IsProfileComplete,
            new UserProfileDto(profile.Id, profile.FullName, profile.Department,
                profile.Designation, profile.StudentId, profile.Bio,
                profile.ProfilePhotoUrl, profile.PhoneNumber,
                profile.LinkedInUrl, profile.ProfileCompletionPercent));
}
