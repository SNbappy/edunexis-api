using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Profile.Commands;

// ── Upload Profile Photo ──────────────────────────────────────────────────────
public record UploadProfilePhotoCommand(
    Guid UserId, Stream FileStream, string FileName
) : ICommand<ApiResponse<string>>;

public sealed class UploadProfilePhotoCommandValidator : AbstractValidator<UploadProfilePhotoCommand>
{
    private static readonly string[] Allowed = [".jpg", ".jpeg", ".png", ".webp"];
    public UploadProfilePhotoCommandValidator()
    {
        RuleFor(x => x.FileName)
            .Must(n => Allowed.Contains(Path.GetExtension(n).ToLowerInvariant()))
            .WithMessage("Only JPG, PNG, or WEBP images are allowed.");
        RuleFor(x => x.FileStream)
            .Must(s => s.Length <= 5 * 1024 * 1024)
            .WithMessage("File size must not exceed 5MB.");
    }
}

public sealed class UploadProfilePhotoCommandHandler(IUnitOfWork uow, IFileStorageService storage)
    : ICommandHandler<UploadProfilePhotoCommand, ApiResponse<string>>
{
    public async ValueTask<ApiResponse<string>> Handle(
        UploadProfilePhotoCommand command, CancellationToken ct)
    {
        var user = await uow.Users.GetWithProfileAsync(command.UserId, ct)
            ?? throw new NotFoundException("User", command.UserId);
        var profile = user.Profile ?? throw new NotFoundException("UserProfile", command.UserId);

        var url = await storage.UploadAsync(command.FileStream, command.FileName, $"profiles/{command.UserId}", ct);
        profile.SetProfilePhoto(url);
        uow.UserProfiles.Update(profile);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<string>.Ok(url, "Profile photo uploaded.");
    }
}

// ── Remove Profile Photo ──────────────────────────────────────────────────────
public record RemoveProfilePhotoCommand(Guid UserId) : ICommand<ApiResponse<bool>>;

public sealed class RemoveProfilePhotoCommandHandler(IUnitOfWork uow)
    : ICommandHandler<RemoveProfilePhotoCommand, ApiResponse<bool>>
{
    public async ValueTask<ApiResponse<bool>> Handle(
        RemoveProfilePhotoCommand command, CancellationToken ct)
    {
        var profile = await uow.UserProfiles.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("UserProfile", command.UserId);
        profile.RemoveProfilePhoto();
        uow.UserProfiles.Update(profile);
        await uow.SaveChangesAsync(ct);
        return ApiResponse<bool>.Ok(true, "Profile photo removed.");
    }
}

// ── Upload Cover Photo ────────────────────────────────────────────────────────
public record UploadCoverPhotoCommand(
    Guid UserId, Stream FileStream, string FileName
) : ICommand<ApiResponse<string>>;

public sealed class UploadCoverPhotoCommandHandler(IUnitOfWork uow, IFileStorageService storage)
    : ICommandHandler<UploadCoverPhotoCommand, ApiResponse<string>>
{
    public async ValueTask<ApiResponse<string>> Handle(
        UploadCoverPhotoCommand command, CancellationToken ct)
    {
        var profile = await uow.UserProfiles.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("UserProfile", command.UserId);

        var url = await storage.UploadAsync(command.FileStream, command.FileName, $"covers/{command.UserId}", ct);
        profile.SetCoverPhoto(url);
        uow.UserProfiles.Update(profile);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<string>.Ok(url, "Cover photo uploaded.");
    }
}

// ── Remove Cover Photo ────────────────────────────────────────────────────────
public record RemoveCoverPhotoCommand(Guid UserId) : ICommand<ApiResponse<bool>>;

public sealed class RemoveCoverPhotoCommandHandler(IUnitOfWork uow)
    : ICommandHandler<RemoveCoverPhotoCommand, ApiResponse<bool>>
{
    public async ValueTask<ApiResponse<bool>> Handle(
        RemoveCoverPhotoCommand command, CancellationToken ct)
    {
        var profile = await uow.UserProfiles.GetByUserIdAsync(command.UserId, ct)
            ?? throw new NotFoundException("UserProfile", command.UserId);
        profile.RemoveCoverPhoto();
        uow.UserProfiles.Update(profile);
        await uow.SaveChangesAsync(ct);
        return ApiResponse<bool>.Ok(true, "Cover photo removed.");
    }
}
