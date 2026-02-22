using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Profile.Commands;

public record UploadProfilePhotoCommand(
    Guid UserId,
    Stream FileStream,
    string FileName
) : ICommand<ApiResponse<string>>;

public sealed class UploadProfilePhotoCommandValidator : AbstractValidator<UploadProfilePhotoCommand>
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public UploadProfilePhotoCommandValidator()
    {
        RuleFor(x => x.FileName)
            .Must(name => AllowedExtensions.Contains(
                Path.GetExtension(name).ToLowerInvariant()))
            .WithMessage("Only JPG, PNG, or WEBP images are allowed.");

        RuleFor(x => x.FileStream)
            .Must(s => s.Length <= 5 * 1024 * 1024)
            .WithMessage("File size must not exceed 5MB.");
    }
}

public sealed class UploadProfilePhotoCommandHandler(
    IUnitOfWork uow,
    IFileStorageService storage
) : ICommandHandler<UploadProfilePhotoCommand, ApiResponse<string>>
{
    public async ValueTask<ApiResponse<string>> Handle(
        UploadProfilePhotoCommand command, CancellationToken ct)
    {
        var user = await uow.Users.GetWithProfileAsync(command.UserId, ct)
            ?? throw new NotFoundException("User", command.UserId);

        var profile = user.Profile
            ?? throw new NotFoundException("UserProfile", command.UserId);

        var url = await storage.UploadAsync(
            command.FileStream, command.FileName,
            $"profiles/{command.UserId}", ct);

        profile.SetProfilePhoto(url);
        uow.UserProfiles.Update(profile);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<string>.Ok(url, "Profile photo uploaded successfully.");
    }
}
