using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Profile.Commands;

public record UpdateProfileCommand(
    Guid UserId,
    string FullName,
    string Department,
    string? Designation,
    string? StudentId,
    string? Bio,
    string? Headline,
    string? PhoneNumber,
    string? OfficeLocation,
    string? OfficeHours,
    string? ResearchInterestsCsv,
    string? FieldsOfWorkCsv,
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
        RuleFor(x => x.Headline).MaximumLength(160);
        RuleFor(x => x.OfficeLocation).MaximumLength(120);
        RuleFor(x => x.OfficeHours).MaximumLength(160);
        RuleFor(x => x.ResearchInterestsCsv).MaximumLength(500);
        RuleFor(x => x.FieldsOfWorkCsv).MaximumLength(500);

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[0-9]{7,15}$")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Invalid phone number format.");

        RuleFor(x => x.LinkedInUrl)
            .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Invalid LinkedIn URL.");
        RuleFor(x => x.FacebookUrl)
            .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Invalid Facebook URL.");
        RuleFor(x => x.TwitterUrl)
            .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Invalid Twitter URL.");
        RuleFor(x => x.GitHubUrl)
            .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Invalid GitHub URL.");
        RuleFor(x => x.WebsiteUrl)
            .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Invalid Website URL.");
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

        // Role-aware required field enforcement
        if (user.Role == UserRole.Teacher && string.IsNullOrWhiteSpace(command.Designation))
            return ApiResponse<UserProfileDto>.Fail("Designation is required for teachers.");
        if (user.Role == UserRole.Student && string.IsNullOrWhiteSpace(command.StudentId))
            return ApiResponse<UserProfileDto>.Fail("Student ID is required for students.");

        profile.Update(
            command.FullName, command.Department,
            command.Designation, command.StudentId,
            command.Bio, command.Headline, command.PhoneNumber,
            command.OfficeLocation, command.OfficeHours,
            command.ResearchInterestsCsv, command.FieldsOfWorkCsv,
            command.LinkedInUrl, command.FacebookUrl,
            command.TwitterUrl, command.GitHubUrl, command.WebsiteUrl);

        if (profile.MeetsRequirement(user.Role)) user.MarkProfileComplete();
        else user.MarkProfileIncomplete();

        uow.UserProfiles.Update(profile);
        uow.Users.Update(user);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<UserProfileDto>.Ok(MapToDto(profile));
    }

    internal static UserProfileDto MapToDto(UserProfile p) =>
        new(p.Id, p.FullName, p.Department, p.Designation, p.StudentId,
            p.Bio, p.Headline, p.ProfilePhotoUrl, p.CoverPhotoUrl, p.PhoneNumber,
            p.OfficeLocation, p.OfficeHours, p.ResearchInterestsCsv, p.FieldsOfWorkCsv,
            p.LinkedInUrl, p.FacebookUrl, p.TwitterUrl, p.GitHubUrl,
            p.WebsiteUrl, p.ProfileCompletionPercent);
}