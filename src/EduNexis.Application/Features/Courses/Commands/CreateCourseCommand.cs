using EduNexis.Application.DTOs;
using EduNexis.Application.Extensions;

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
    public async ValueTask<ApiResponse<CourseDto>> Handle(
        CreateCourseCommand cmd, CancellationToken ct)
    {
        var exists = await uow.Courses.ExistsAsync(
            c => c.CourseCode == cmd.CourseCode, ct);
        if (exists)
            return ApiResponse<CourseDto>.Fail("Course code already exists.");

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
