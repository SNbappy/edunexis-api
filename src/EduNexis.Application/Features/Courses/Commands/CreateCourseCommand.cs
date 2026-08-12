using EduNexis.Application.DTOs;
using EduNexis.Application.Extensions;
using EduNexis.Domain.Entities;

namespace EduNexis.Application.Features.Courses.Commands;

public record CreateCourseCommand(
    string Title,
    string CourseCode,
    decimal CreditHours,
    string Department,
    string AcademicSession,
    string Semester,
    string? Section,
    CourseType CourseType,
    string? Description,
    string CoverImageUrl,
    Guid TeacherId
) : ICommand<ApiResponse<CourseDto>>;

public sealed class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CourseCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Department).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AcademicSession).NotEmpty();
        RuleFor(x => x.Semester).NotEmpty();
        RuleFor(x => x.CreditHours).InclusiveBetween(0.5m, 6m);
        RuleFor(x => x.TeacherId).NotEmpty();
    }
}

public sealed class CreateCourseCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<CreateCourseCommand, ApiResponse<CourseDto>>
{
    // Starter quota granted automatically on first course creation attempt.
    // Teachers get 1 free class; more must be requested from an admin.
    private const int StarterCourseCount = 1;
    private const int StarterAccessYears = 100;

    public async ValueTask<ApiResponse<CourseDto>> Handle(
        CreateCourseCommand cmd, CancellationToken ct)
    {
        var exists = await uow.Courses.ExistsAsync(
            c => c.CourseCode == cmd.CourseCode
              && c.TeacherId == cmd.TeacherId
              && c.AcademicSession == cmd.AcademicSession
              && c.Semester == cmd.Semester,
            ct);
        if (exists)
            return ApiResponse<CourseDto>.Fail("Course code already exists.");

        // Quota enforcement is a platform-wide switch an admin controls at
        // runtime (PlatformSetting.CourseQuotaEnforced). While off, every
        // teacher creates unlimited courses - the pre-launch/free-rollout
        // state. Once an admin turns it on, the 1-free-course + admin-grant
        // system below applies to every teacher going forward.
        var settings = (await uow.GetRepository<PlatformSetting>().GetAllAsync(ct))
            .FirstOrDefault();
        var quotaEnforced = settings?.CourseQuotaEnforced ?? false;

        if (quotaEnforced)
        {
            // First-time creators get an auto-provisioned starter quota (1 course,
            // ~100 year validity, self-assigned). Any grants beyond that come from
            // an admin via the QuotaRequest flow.
            var quota = await uow.TeacherQuotas.GetActiveQuotaAsync(cmd.TeacherId, ct);
            if (quota is null)
            {
                quota = TeacherQuota.Create(
                    teacherId: cmd.TeacherId,
                    assignedById: cmd.TeacherId,
                    totalQuota: StarterCourseCount,
                    startDate: DateTime.UtcNow,
                    endDate: DateTime.UtcNow.AddYears(StarterAccessYears));
                await uow.TeacherQuotas.AddAsync(quota, ct);
            }
            // Throws QuotaExceededException / AccessExpiredException; middleware maps to 403.
            quota.ConsumeOne();
        }

        var course = Course.Create(
            cmd.Title, cmd.CourseCode, cmd.CreditHours,
            cmd.Department, cmd.AcademicSession, cmd.Semester,
            cmd.Section, cmd.CourseType, cmd.Description,
            cmd.CoverImageUrl, cmd.TeacherId);

        await uow.Courses.AddAsync(course, ct);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<CourseDto>.Ok(course.ToDto(), "Course created.");
    }
}