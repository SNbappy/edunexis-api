using EduNexis.Application.DTOs;
using EduNexis.Application.Extensions;

namespace EduNexis.Application.Features.Courses.Commands;

public record UpdateCourseCommand(
    Guid Id,
    string Title,
    string CourseCode,
    decimal CreditHours,
    string Department,
    string AcademicSession,
    string Semester,
    string? Section,
    CourseType CourseType,
    string? Description
) : ICommand<ApiResponse<CourseDto>>;

public sealed class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CourseCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Department).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AcademicSession).NotEmpty();
        RuleFor(x => x.Semester).NotEmpty();
        RuleFor(x => x.CreditHours).InclusiveBetween(0.5m, 6m);
    }
}

public sealed class UpdateCourseCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<UpdateCourseCommand, ApiResponse<CourseDto>>
{
    public async ValueTask<ApiResponse<CourseDto>> Handle(
        UpdateCourseCommand cmd, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(cmd.Id, ct);
        if (course is null)
            return ApiResponse<CourseDto>.Fail("Course not found.");

        course.Update(
            cmd.Title, cmd.CourseCode, cmd.CreditHours,
            cmd.Department, cmd.AcademicSession, cmd.Semester,
            cmd.Section, cmd.CourseType, cmd.Description);

        uow.Courses.Update(course);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<CourseDto>.Ok(course.ToDto(), "Course updated.");
    }
}
