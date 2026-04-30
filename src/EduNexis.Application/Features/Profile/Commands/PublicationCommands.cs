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
            pub.Url, pub.Type.ToString(), pub.OrderIndex));
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
            pub.Url, pub.Type.ToString(), pub.OrderIndex));
    }
}

// ─────────────────────────── DELETE ───────────────────────────

public record DeletePublicationCommand(Guid Id, Guid UserId)
    : ICommand<ApiResponse<bool>>;

public sealed class DeletePublicationCommandHandler(IUnitOfWork uow)
    : ICommandHandler<DeletePublicationCommand, ApiResponse<bool>>
{
    public async ValueTask<ApiResponse<bool>> Handle(
        DeletePublicationCommand command, CancellationToken ct)
    {
        var pub = await uow.GetRepository<UserPublication>().GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("UserPublication", command.Id);

        if (pub.UserId != command.UserId)
            throw new UnauthorizedException("You cannot delete this publication.");

        uow.GetRepository<UserPublication>().Delete(pub);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<bool>.Ok(true);
    }
}