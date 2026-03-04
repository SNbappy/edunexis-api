using EduNexis.Application.DTOs;
using EduNexis.Domain.Interfaces.Services;

namespace EduNexis.Application.Features.Auth.Commands;

public record RegisterUserCommand(string Email, string Password, string FullName) : ICommand<ApiResponse<AuthResponseDto>>;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress()
            .Must(e => e.EndsWith("@just.edu.bd", StringComparison.OrdinalIgnoreCase) ||
                       e.EndsWith("@student.just.edu.bd", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only @just.edu.bd or @student.just.edu.bd emails are allowed.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Must contain at least one uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Must contain at least one number.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Must contain at least one special character.");
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(2);
    }
}

public sealed class RegisterUserCommandHandler(
    IUnitOfWork uow, IJwtTokenService jwtService, IPasswordHasher passwordHasher
) : ICommandHandler<RegisterUserCommand, ApiResponse<AuthResponseDto>>
{
    public async ValueTask<ApiResponse<AuthResponseDto>> Handle(RegisterUserCommand command, CancellationToken ct)
    {
        var existing = await uow.Users.GetByEmailAsync(command.Email, ct);
        if (existing is not null) return ApiResponse<AuthResponseDto>.Fail("Email already registered.");

        var role = command.Email.EndsWith("@student.just.edu.bd", StringComparison.OrdinalIgnoreCase)
            ? UserRole.Student : UserRole.Teacher;

        var user = User.Create(command.Email, passwordHasher.Hash(command.Password), role);
        await uow.Users.AddAsync(user, ct);

        var profile = UserProfile.Create(user.Id, command.FullName);
        await uow.UserProfiles.AddAsync(profile, ct);

        var accessToken = jwtService.GenerateAccessToken(user.Id, user.Email, role.ToString());
        var refreshToken = jwtService.GenerateRefreshToken();
        user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
        await uow.SaveChangesAsync(ct);

        return ApiResponse<AuthResponseDto>.Ok(new AuthResponseDto(
            AccessToken: accessToken, RefreshToken: refreshToken, ExpiresIn: 3600,
            User: new UserDto(user.Id, user.Email, role.ToString(), false,
                new UserProfileDto(profile.Id, command.FullName, null, null, null,
                    null, null, null, null, null, null, null, null, null, 30))),
            "Registration successful.");
    }
}
