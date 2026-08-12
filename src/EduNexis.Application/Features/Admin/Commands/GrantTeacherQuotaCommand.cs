using EduNexis.Domain.Entities;

namespace EduNexis.Application.Features.Admin.Commands;

public record GrantTeacherQuotaCommand(
    Guid TeacherId,
    int TotalQuota,
    int AccessDurationDays
) : ICommand<ApiResponse>;

public sealed class GrantTeacherQuotaCommandValidator : AbstractValidator<GrantTeacherQuotaCommand>
{
    public GrantTeacherQuotaCommandValidator()
    {
        RuleFor(x => x.TotalQuota).GreaterThan(0);
        RuleFor(x => x.AccessDurationDays).GreaterThan(0);
    }
}

public sealed class GrantTeacherQuotaCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser
) : ICommandHandler<GrantTeacherQuotaCommand, ApiResponse>
{
    public async ValueTask<ApiResponse> Handle(
        GrantTeacherQuotaCommand cmd, CancellationToken ct)
    {
        var adminId = Guid.Parse(currentUser.UserId);

        var teacher = await uow.Users.GetByIdAsync(cmd.TeacherId, ct);
        if (teacher is null)
            return ApiResponse.Fail("Teacher not found.");

        var existing = await uow.TeacherQuotas.GetActiveQuotaAsync(cmd.TeacherId, ct);

        if (existing is not null)
        {
            existing.UpdateQuota(cmd.TotalQuota);
            existing.ExtendAccess(DateTime.UtcNow.AddDays(cmd.AccessDurationDays));
            uow.GetRepository<TeacherQuota>().Update(existing);
        }
        else
        {
            var quota = TeacherQuota.Create(
                teacherId: cmd.TeacherId,
                assignedById: adminId,
                totalQuota: cmd.TotalQuota,
                startDate: DateTime.UtcNow,
                endDate: DateTime.UtcNow.AddDays(cmd.AccessDurationDays));
            await uow.TeacherQuotas.AddAsync(quota, ct);
        }

        await uow.SaveChangesAsync(ct);

        return ApiResponse.Ok($"Quota updated: {cmd.TotalQuota} courses for {teacher.Email}.");
    }
}