using EduNexis.Application.DTOs;
using EduNexis.Domain.Entities;

namespace EduNexis.Application.Features.Profile.Commands;

// ---------- ADD ----------
public record AddEducationCommand(
    Guid UserId,
    string Institution,
    string Degree,
    string FieldOfStudy,
    int StartYear,
    int? EndYear,
    string? Description
) : ICommand<ApiResponse<UserEducationDto>>;

public sealed class AddEducationCommandValidator : AbstractValidator<AddEducationCommand>
{
    public AddEducationCommandValidator()
    {
        RuleFor(x => x.Institution).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Degree).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FieldOfStudy).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StartYear).InclusiveBetween(1950, DateTime.UtcNow.Year);
        RuleFor(x => x.EndYear)
            .GreaterThanOrEqualTo(x => x.StartYear)
            .When(x => x.EndYear.HasValue);
    }
}

public sealed class AddEducationCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<AddEducationCommand, ApiResponse<UserEducationDto>>
{
    public async ValueTask<ApiResponse<UserEducationDto>> Handle(
        AddEducationCommand command, CancellationToken ct)
    {
        var edu = UserEducation.Create(
            command.UserId, command.Institution, command.Degree,
            command.FieldOfStudy, command.StartYear, command.EndYear, command.Description);

        await uow.GetRepository<UserEducation>().AddAsync(edu, ct);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<UserEducationDto>.Ok(
            new UserEducationDto(edu.Id, edu.Institution, edu.Degree,
                edu.FieldOfStudy, edu.StartYear, edu.EndYear, edu.Description));
    }
}

// ---------- UPDATE ----------
public record UpdateEducationCommand(
    Guid UserId,
    Guid EducationId,
    string Institution,
    string Degree,
    string FieldOfStudy,
    int StartYear,
    int? EndYear,
    string? Description
) : ICommand<ApiResponse<UserEducationDto>>;

public sealed class UpdateEducationCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<UpdateEducationCommand, ApiResponse<UserEducationDto>>
{
    public async ValueTask<ApiResponse<UserEducationDto>> Handle(
        UpdateEducationCommand command, CancellationToken ct)
    {
        var edu = await uow.GetRepository<UserEducation>()
            .FirstOrDefaultAsync(e => e.Id == command.EducationId && e.UserId == command.UserId, ct)
            ?? throw new NotFoundException("Education", command.EducationId);

        edu.Update(command.Institution, command.Degree, command.FieldOfStudy,
            command.StartYear, command.EndYear, command.Description);

        uow.GetRepository<UserEducation>().Update(edu);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<UserEducationDto>.Ok(
            new UserEducationDto(edu.Id, edu.Institution, edu.Degree,
                edu.FieldOfStudy, edu.StartYear, edu.EndYear, edu.Description));
    }
}

// ---------- DELETE ----------
public record DeleteEducationCommand(
    Guid UserId,
    Guid EducationId
) : ICommand<ApiResponse<bool>>;

public sealed class DeleteEducationCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<DeleteEducationCommand, ApiResponse<bool>>
{
    public async ValueTask<ApiResponse<bool>> Handle(
        DeleteEducationCommand command, CancellationToken ct)
    {
        var edu = await uow.GetRepository<UserEducation>()
            .FirstOrDefaultAsync(e => e.Id == command.EducationId && e.UserId == command.UserId, ct)
            ?? throw new NotFoundException("Education", command.EducationId);

        uow.GetRepository<UserEducation>().Delete(edu);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<bool>.Ok(true, "Education entry removed.");
    }
}
