using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Auth.Commands;

public record RegisterUserCommand(
    string FirebaseToken,
    string Email
) : ICommand<ApiResponse<UserDto>>;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.FirebaseToken)
            .NotEmpty().WithMessage("Firebase token is required.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .Must(BeAValidUniversityEmail)
            .WithMessage("Only @just.edu.bd or @student.just.edu.bd emails are allowed.");
    }

    private static bool BeAValidUniversityEmail(string email) =>
        email.EndsWith("@just.edu.bd", StringComparison.OrdinalIgnoreCase) ||
        email.EndsWith("@student.just.edu.bd", StringComparison.OrdinalIgnoreCase);
}

public sealed class RegisterUserCommandHandler(
    IUnitOfWork uow,
    IFirebaseAuthService firebaseAuth
) : ICommandHandler<RegisterUserCommand, ApiResponse<UserDto>>
{
    public async ValueTask<ApiResponse<UserDto>> Handle(
        RegisterUserCommand command, CancellationToken ct)
    {
        var payload = await firebaseAuth.VerifyTokenAsync(command.FirebaseToken, ct)
            ?? throw new UnauthorizedException("Invalid Firebase token.");

        var existing = await uow.Users.GetByFirebaseUidAsync(payload.Uid, ct);
        if (existing is not null)
            return ApiResponse<UserDto>.Fail("User already registered. Please login.");

        var role = command.Email.EndsWith("@student.just.edu.bd", StringComparison.OrdinalIgnoreCase)
            ? UserRole.Student
            : UserRole.Teacher;

        var user = User.Create(payload.Uid, command.Email, role);
        await uow.Users.AddAsync(user, ct);

        var profile = UserProfile.Create(user.Id);
        await uow.UserProfiles.AddAsync(profile, ct);

        await uow.SaveChangesAsync(ct);

        return ApiResponse<UserDto>.Ok(MapToDto(user, profile), "Registration successful.");
    }

    private static UserDto MapToDto(User user, UserProfile profile) =>
        new(user.Id, user.Email, user.Role.ToString(), user.IsProfileComplete,
            new UserProfileDto(profile.Id, profile.FullName, profile.Department,
                profile.Designation, profile.StudentId, profile.Bio,
                profile.ProfilePhotoUrl, profile.PhoneNumber,
                profile.LinkedInUrl, profile.ProfileCompletionPercent));
}
