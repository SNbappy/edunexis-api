using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Courses.Commands;

public record UpdateCourseCommand(
    Guid CourseId,
    Guid RequesterId,
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
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CourseCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.CreditHours).InclusiveBetween(0.5m, 6.0m);
        RuleFor(x => x.Department).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AcademicSession).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Semester).NotEmpty().MaximumLength(20);
    }
}

public sealed class UpdateCourseCommandHandler(
    IUnitOfWork uow
) : ICommandHandler<UpdateCourseCommand, ApiResponse<CourseDto>>
{
    public async ValueTask<ApiResponse<CourseDto>> Handle(
        UpdateCourseCommand command, CancellationToken ct)
    {
        var course = await uow.Courses.GetByIdAsync(command.CourseId, ct)
            ?? throw new NotFoundException("Course", command.CourseId);

        if (course.TeacherId != command.RequesterId)
            throw new UnauthorizedException("Only the course teacher can update this course.");

        course.Update(
            command.Title, command.CourseCode, command.CreditHours,
            command.Department, command.AcademicSession, command.Semester,
            command.Section, command.CourseType, command.Description);

        uow.Courses.Update(course);
        await uow.SaveChangesAsync(ct);

        var teacher = await uow.Users.GetWithProfileAsync(course.TeacherId, ct);

        return ApiResponse<CourseDto>.Ok(MapToDto(course, teacher!));
    }

    private static CourseDto MapToDto(Course c, User teacher) =>
        new(c.Id, c.Title, c.CourseCode, c.CreditHours, c.Department,
            c.AcademicSession, c.Semester, c.Section, c.CourseType.ToString(),
            c.Description, c.CoverImageUrl, c.JoiningCode, c.TeacherId,
            teacher.Profile?.FullName ?? teacher.Email, c.IsArchived, 0, c.CreatedAt);
}
