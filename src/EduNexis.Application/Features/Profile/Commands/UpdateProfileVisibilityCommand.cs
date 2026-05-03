using EduNexis.Application.Common.Slugs;
using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Profile.Commands;

/// <summary>
/// Toggles a profile's public visibility, with optional slug override.
/// - IsPublic=true, no slug given: auto-generate from full name
/// - IsPublic=true, slug given: validate format + uniqueness, then set
/// - IsPublic=false: just flip the flag, slug stays so they can re-enable later
/// </summary>
public record UpdateProfileVisibilityCommand(
    Guid UserId,
    bool IsPublic,
    string? Slug
) : ICommand<ApiResponse<ProfileVisibilityDto>>;

public record ProfileVisibilityDto(bool IsPublic, string? Slug);

public sealed class UpdateProfileVisibilityCommandHandler(IUnitOfWork uow)
    : ICommandHandler<UpdateProfileVisibilityCommand, ApiResponse<ProfileVisibilityDto>>
{
    public async ValueTask<ApiResponse<ProfileVisibilityDto>> Handle(
        UpdateProfileVisibilityCommand command, CancellationToken ct)
    {
        var user = await uow.Users.GetByIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("User", command.UserId);

        if (user.Role != UserRole.Teacher)
            return ApiResponse<ProfileVisibilityDto>.Fail("Only teachers can have public profiles.");

        var profile = await uow.UserProfiles.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("Profile", command.UserId);

        if (!command.IsPublic)
        {
            profile.MakePrivate();
            await uow.SaveChangesAsync(ct);
            return ApiResponse<ProfileVisibilityDto>.Ok(
                new ProfileVisibilityDto(false, profile.PublicSlug),
                "Profile is now private.");
        }

        // Going public: resolve slug
        string slug;
        if (!string.IsNullOrWhiteSpace(command.Slug))
        {
            var formatError = SlugGenerator.Validate(command.Slug);
            if (formatError is not null)
                return ApiResponse<ProfileVisibilityDto>.Fail(formatError);
            if (await uow.UserProfiles.IsSlugTakenAsync(command.Slug, command.UserId, ct))
                return ApiResponse<ProfileVisibilityDto>.Fail("This URL is already taken.");
            slug = command.Slug;
        }
        else if (!string.IsNullOrWhiteSpace(profile.PublicSlug))
        {
            slug = profile.PublicSlug;
        }
        else
        {
            slug = await SlugGenerator.GenerateUniqueAsync(
                profile.FullName, command.UserId, uow.UserProfiles, ct);
        }

        profile.MakePublic(slug);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<ProfileVisibilityDto>.Ok(
            new ProfileVisibilityDto(true, slug),
            "Profile is now public.");
    }
}