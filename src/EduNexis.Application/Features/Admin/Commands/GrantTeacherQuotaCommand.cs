using EduNexis.Domain.Entities;

namespace EduNexis.Application.Features.Admin.Commands;

/// <summary>
/// Issues a new course-creation grant. Always additive: <paramref name="Courses"/>
/// is how many courses to ADD, not a new total.
///
/// The previous version edited the teacher's single quota row, which meant the
/// admin had to supply the new total (so "give him 2 more" required knowing he
/// had 5 and typing 7), and it refused outright to issue a short grant to a
/// teacher who already held a longer window — extending could not move the end
/// date backwards, so the whole request failed with a domain error.
/// </summary>
public record GrantTeacherQuotaCommand(
    Guid TeacherId,
    int Courses,
    int AccessDurationDays,
    string? Note = null
) : ICommand<ApiResponse>;

public sealed class GrantTeacherQuotaCommandValidator : AbstractValidator<GrantTeacherQuotaCommand>
{
    public GrantTeacherQuotaCommandValidator()
    {
        RuleFor(x => x.Courses)
            .GreaterThan(0).WithMessage("Grant at least one course.")
            .LessThanOrEqualTo(500).WithMessage("That looks like a typo — 500 courses is the maximum per grant.");

        RuleFor(x => x.AccessDurationDays)
            .GreaterThan(0).WithMessage("Access duration must be at least one day.")
            .LessThanOrEqualTo(3650).WithMessage("Access duration cannot exceed 10 years.");

        RuleFor(x => x.Note).MaximumLength(200);
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

        var now = DateTime.UtcNow;
        var grant = TeacherQuota.Create(
            teacherId: cmd.TeacherId,
            assignedById: adminId,
            totalQuota: cmd.Courses,
            startDate: now,
            endDate: now.AddDays(cmd.AccessDurationDays),
            note: cmd.Note);

        await uow.TeacherQuotas.AddAsync(grant, ct);
        await uow.SaveChangesAsync(ct);

        var plural = cmd.Courses == 1 ? "course" : "courses";
        return ApiResponse.Ok(
            $"Granted {cmd.Courses} {plural} to {teacher.Email}, valid for {cmd.AccessDurationDays} days.");
    }
}
