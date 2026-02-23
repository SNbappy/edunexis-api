using EduNexis.Application.DTOs;
using EduNexis.Domain.Interfaces.Services;

namespace EduNexis.Application.Features.Auth.Commands;

public record RegisterUserCommand(
    string Email,
    string Password,
    string FullName
) : ICommand<ApiResponse<AuthResponseDto>>;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .Must(BeAValidUniversityEmail)
            .WithMessage("Only @just.edu.bd or @student.just.edu.bd emails are allowed.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Must contain at least one uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Must contain at least one number.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Must contain at least one special character.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(2).WithMessage("Full name must be at least 2 characters.");
    }

    private static bool BeAValidUniversityEmail(string email) =>
        email.EndsWith("@just.edu.bd", StringComparison.OrdinalIgnoreCase) ||
        email.EndsWith("@student.just.edu.bd", StringComparison.OrdinalIgnoreCase);
}

public sealed class RegisterUserCommandHandler(
    IUnitOfWork uow,
    IJwtTokenService jwtService,
    IPasswordHasher passwordHasher
) : ICommandHandler<RegisterUserCommand, ApiResponse<AuthResponseDto>>
{
    public async ValueTask<ApiResponse<AuthResponseDto>> Handle(
        RegisterUserCommand command, CancellationToken ct)
    {
        var existing = await uow.Users.GetByEmailAsync(command.Email, ct);
        if (existing is not null)
            return ApiResponse<AuthResponseDto>.Fail("Email already registered.");

        var role = command.Email.EndsWith("@student.just.edu.bd", StringComparison.OrdinalIgnoreCase)
            ? UserRole.Student
            : UserRole.Teacher;

        var passwordHash = passwordHasher.Hash(command.Password);
        var user = User.Create(command.Email, passwordHash, role);
        await uow.Users.AddAsync(user, ct);

        var profile = UserProfile.Create(user.Id, command.FullName);
        await uow.UserProfiles.AddAsync(profile, ct);

        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, role.ToString());
        var refreshToken = jwtService.GenerateRefreshToken();
        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));

        await uow.SaveChangesAsync(ct);

        return ApiResponse<AuthResponseDto>.Ok(
            new AuthResponseDto(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                ExpiresIn: 3600,
                User: new UserDto(
                    user.Id,
                    user.Email,
                    role.ToString(),
                    false,
                    new UserProfileDto(
                        Id: profile.Id,
                        FullName: command.FullName,
                        Department: null,
                        Designation: null,
                        StudentId: null,
                        Bio: null,
                        ProfilePhotoUrl: null,
                        PhoneNumber: null,
                        LinkedInUrl: null,
                        ProfileCompletionPercent: 30))),
            "Registration successful.");
    }
}
