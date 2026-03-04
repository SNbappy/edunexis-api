using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Profile.Commands;

public record UpdateProfileCommand(
    Guid UserId,
    string FullName,
    string Department,
    string? Designation,
    string? StudentId,
    string? Bio,
    string? PhoneNumber,
    string? LinkedInUrl,
    string? FacebookUrl,
    string? TwitterUrl,
    string? GitHubUrl,
    string? WebsiteUrl
) : ICommand<ApiResponse<UserProfileDto>>;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Department).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[0-9]{7,15}$")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Invalid phone number format.");
        foreach (var prop in new[] { "LinkedInUrl", "FacebookUrl", "TwitterUrl", "WebsiteUrl" })
            RuleFor(x => x.LinkedInUrl)
                .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("Invalid URL.");
    }
}

public sealed class UpdateProfileCommandHandler(IUnitOfWork uow)
    : ICommandHandler<UpdateProfileCommand, ApiResponse<UserProfileDto>>
{
    public async ValueTask<ApiResponse<UserProfileDto>> Handle(
        UpdateProfileCommand command, CancellationToken ct)
    {
        var user = await uow.Users.GetWithProfileAsync(command.UserId, ct)
            ?? throw new NotFoundException("User", command.UserId);
        var profile = user.Profile ?? throw new NotFoundException("UserProfile", command.UserId);

        profile.Update(
            command.FullName, command.Department,
            command.Designation, command.StudentId,
            command.Bio, command.PhoneNumber,
            command.LinkedInUrl, command.FacebookUrl,
            command.TwitterUrl, command.GitHubUrl, command.WebsiteUrl);

        if (profile.MeetsRequirement()) user.MarkProfileComplete();
        else user.MarkProfileIncomplete();

        uow.UserProfiles.Update(profile);
        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<UserProfileDto>.Ok(MapToDto(profile));
    }

    internal static UserProfileDto MapToDto(UserProfile p) =>
        new(p.Id, p.FullName, p.Department, p.Designation, p.StudentId,
            p.Bio, p.ProfilePhotoUrl, p.CoverPhotoUrl, p.PhoneNumber,
            p.LinkedInUrl, p.FacebookUrl, p.TwitterUrl, p.GitHubUrl,
            p.WebsiteUrl, p.ProfileCompletionPercent);
}
