using EduNexis.Application.DTOs;
using EduNexis.Domain.Entities;
using EduNexis.Domain.Enums;

namespace EduNexis.Application.Features.Profile.Commands;

// ─────────────────────────── ADD ───────────────────────────

public record AddPublicationCommand(
    Guid UserId,
    string Title,
    string Authors,
    string? Venue,
    int Year,
    string? Url,
    string Type
) : ICommand<ApiResponse<UserPublicationDto>>;

public sealed class AddPublicationCommandValidator : AbstractValidator<AddPublicationCommand>
{
    public AddPublicationCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Authors).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Venue).MaximumLength(300);
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1);
        RuleFor(x => x.Url)
            .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Invalid URL.");
        RuleFor(x => x.Type)
            .Must(t => Enum.TryParse<PublicationType>(t, true, out _))
            .WithMessage("Invalid publication type.");
    }
}

public sealed class AddPublicationCommandHandler(IUnitOfWork uow)
    : ICommandHandler<AddPublicationCommand, ApiResponse<UserPublicationDto>>
{
    public async ValueTask<ApiResponse<UserPublicationDto>> Handle(
        AddPublicationCommand command, CancellationToken ct)
    {
        var existing = await uow.GetRepository<UserPublication>()
            .FindAsync(p => p.UserId == command.UserId, ct);
        var nextIndex = existing.Any() ? existing.Max(p => p.OrderIndex) + 1 : 0;

        var type = Enum.Parse<PublicationType>(command.Type, ignoreCase: true);

        var pub = UserPublication.Create(
            command.UserId, command.Title, command.Authors,
            command.Venue, command.Year, command.Url, type, nextIndex);

        await uow.GetRepository<UserPublication>().AddAsync(pub, ct);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<UserPublicationDto>.Ok(new UserPublicationDto(
            pub.Id, pub.Title, pub.Authors, pub.Venue, pub.Year,
            pub.Url, pub.Type.ToString(), pub.OrderIndex,
            pub.PdfUrl, pub.PdfSizeBytes, pub.PdfUploadedAt, pub.IsPdfPublic));
    }
}

// ─────────────────────────── UPDATE ───────────────────────────

public record UpdatePublicationCommand(
    Guid Id,
    Guid UserId,
    string Title,
    string Authors,
    string? Venue,
    int Year,
    string? Url,
    string Type
) : ICommand<ApiResponse<UserPublicationDto>>;

public sealed class UpdatePublicationCommandValidator : AbstractValidator<UpdatePublicationCommand>
{
    public UpdatePublicationCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Authors).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Venue).MaximumLength(300);
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1);
        RuleFor(x => x.Url)
            .Must(url => string.IsNullOrEmpty(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Invalid URL.");
        RuleFor(x => x.Type)
            .Must(t => Enum.TryParse<PublicationType>(t, true, out _))
            .WithMessage("Invalid publication type.");
    }
}

public sealed class UpdatePublicationCommandHandler(IUnitOfWork uow)
    : ICommandHandler<UpdatePublicationCommand, ApiResponse<UserPublicationDto>>
{
    public async ValueTask<ApiResponse<UserPublicationDto>> Handle(
        UpdatePublicationCommand command, CancellationToken ct)
    {
        var pub = await uow.GetRepository<UserPublication>().GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("UserPublication", command.Id);

        if (pub.UserId != command.UserId)
            throw new UnauthorizedException("You cannot edit this publication.");

        var type = Enum.Parse<PublicationType>(command.Type, ignoreCase: true);

        pub.Update(command.Title, command.Authors, command.Venue,
            command.Year, command.Url, type);

        uow.GetRepository<UserPublication>().Update(pub);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<UserPublicationDto>.Ok(new UserPublicationDto(
            pub.Id, pub.Title, pub.Authors, pub.Venue, pub.Year,
            pub.Url, pub.Type.ToString(), pub.OrderIndex,
            pub.PdfUrl, pub.PdfSizeBytes, pub.PdfUploadedAt, pub.IsPdfPublic));
    }
}

// ─────────────────────────── DELETE ───────────────────────────

public record DeletePublicationCommand(Guid Id, Guid UserId)
    : ICommand<ApiResponse<bool>>;

public sealed class DeletePublicationCommandHandler(IUnitOfWork uow, IFileStorageService storage)
    : ICommandHandler<DeletePublicationCommand, ApiResponse<bool>>
{
    public async ValueTask<ApiResponse<bool>> Handle(
        DeletePublicationCommand command, CancellationToken ct)
    {
        var pub = await uow.GetRepository<UserPublication>().GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("UserPublication", command.Id);

        if (pub.UserId != command.UserId)
            throw new UnauthorizedException("You cannot delete this publication.");

        // Best-effort: delete attached PDF from Cloudinary before DB delete.
        if (!string.IsNullOrEmpty(pub.PdfPublicId))
        {
            try { await storage.DeleteAsync(pub.PdfPublicId, ct); }
            catch { /* swallow */ }
        }

        uow.GetRepository<UserPublication>().Delete(pub);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<bool>.Ok(true);
    }
}
// =========================== UPLOAD PDF ===========================

public record UploadPublicationPdfCommand(
    Guid PublicationId,
    Guid UserId,
    Stream FileStream,
    string FileName,
    long FileSize
) : ICommand<ApiResponse<UserPublicationDto>>;

public sealed class UploadPublicationPdfCommandValidator : AbstractValidator<UploadPublicationPdfCommand>
{
    private const long MaxBytes = 10 * 1024 * 1024;

    public UploadPublicationPdfCommandValidator()
    {
        RuleFor(x => x.FileName)
            .Must(n => Path.GetExtension(n).ToLowerInvariant() == ".pdf")
            .WithMessage("Only PDF files are allowed.");
        RuleFor(x => x.FileSize)
            .LessThanOrEqualTo(MaxBytes)
            .WithMessage("PDF must be 10 MB or smaller.");
    }
}

public sealed class UploadPublicationPdfCommandHandler(IUnitOfWork uow, IFileStorageService storage)
    : ICommandHandler<UploadPublicationPdfCommand, ApiResponse<UserPublicationDto>>
{
    public async ValueTask<ApiResponse<UserPublicationDto>> Handle(
        UploadPublicationPdfCommand command, CancellationToken ct)
    {
        var pub = await uow.GetRepository<UserPublication>().GetByIdAsync(command.PublicationId, ct)
            ?? throw new NotFoundException("UserPublication", command.PublicationId);

        if (pub.UserId != command.UserId)
            throw new UnauthorizedException("You cannot upload a PDF for this publication.");

        // If a PDF already exists, delete the old one from Cloudinary first.
        if (!string.IsNullOrEmpty(pub.PdfPublicId))
        {
            try { await storage.DeleteAsync(pub.PdfPublicId, ct); }
            catch { /* swallow */ }
        }

        var result = await storage.UploadWithIdAsync(
            command.FileStream, command.FileName,
            $"publications/{command.UserId}", ct);

        pub.SetPdf(result.Url, result.PublicId, result.SizeBytes);
        uow.GetRepository<UserPublication>().Update(pub);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<UserPublicationDto>.Ok(new UserPublicationDto(
            pub.Id, pub.Title, pub.Authors, pub.Venue, pub.Year,
            pub.Url, pub.Type.ToString(), pub.OrderIndex,
            pub.PdfUrl, pub.PdfSizeBytes, pub.PdfUploadedAt, pub.IsPdfPublic),
            "PDF uploaded.");
    }
}

// =========================== REMOVE PDF ===========================

public record RemovePublicationPdfCommand(Guid PublicationId, Guid UserId)
    : ICommand<ApiResponse<UserPublicationDto>>;

public sealed class RemovePublicationPdfCommandHandler(IUnitOfWork uow, IFileStorageService storage)
    : ICommandHandler<RemovePublicationPdfCommand, ApiResponse<UserPublicationDto>>
{
    public async ValueTask<ApiResponse<UserPublicationDto>> Handle(
        RemovePublicationPdfCommand command, CancellationToken ct)
    {
        var pub = await uow.GetRepository<UserPublication>().GetByIdAsync(command.PublicationId, ct)
            ?? throw new NotFoundException("UserPublication", command.PublicationId);

        if (pub.UserId != command.UserId)
            throw new UnauthorizedException("You cannot remove this PDF.");

        if (!string.IsNullOrEmpty(pub.PdfPublicId))
        {
            try { await storage.DeleteAsync(pub.PdfPublicId, ct); }
            catch { /* swallow */ }
        }

        pub.RemovePdf();
        uow.GetRepository<UserPublication>().Update(pub);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<UserPublicationDto>.Ok(new UserPublicationDto(
            pub.Id, pub.Title, pub.Authors, pub.Venue, pub.Year,
            pub.Url, pub.Type.ToString(), pub.OrderIndex,
            pub.PdfUrl, pub.PdfSizeBytes, pub.PdfUploadedAt, pub.IsPdfPublic),
            "PDF removed.");
    }
}

// ===================== UPDATE PDF VISIBILITY =====================

public record UpdatePublicationPdfVisibilityCommand(
    Guid PublicationId, Guid UserId, bool IsPublic
) : ICommand<ApiResponse<UserPublicationDto>>;

public sealed class UpdatePublicationPdfVisibilityCommandHandler(IUnitOfWork uow)
    : ICommandHandler<UpdatePublicationPdfVisibilityCommand, ApiResponse<UserPublicationDto>>
{
    public async ValueTask<ApiResponse<UserPublicationDto>> Handle(
        UpdatePublicationPdfVisibilityCommand command, CancellationToken ct)
    {
        var pub = await uow.GetRepository<UserPublication>().GetByIdAsync(command.PublicationId, ct)
            ?? throw new NotFoundException("UserPublication", command.PublicationId);

        if (pub.UserId != command.UserId)
            throw new UnauthorizedException("You cannot change visibility of this PDF.");

        pub.SetPdfPublic(command.IsPublic);
        uow.GetRepository<UserPublication>().Update(pub);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<UserPublicationDto>.Ok(new UserPublicationDto(
            pub.Id, pub.Title, pub.Authors, pub.Venue, pub.Year,
            pub.Url, pub.Type.ToString(), pub.OrderIndex,
            pub.PdfUrl, pub.PdfSizeBytes, pub.PdfUploadedAt, pub.IsPdfPublic));
    }
}