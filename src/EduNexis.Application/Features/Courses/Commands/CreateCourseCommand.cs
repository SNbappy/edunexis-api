using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Courses.Commands;

public record CreateCourseCommand(
    Guid TeacherId,
    string Title,
    string CourseCode,
    decimal CreditHours,
    string Department,
    string AcademicSession,
    string Semester,
    string? Section,
    CourseType CourseType,
    string? Description,
    Stream? CoverImageStream,
    string? CoverImageFileName
) : ICommand<ApiResponse<CourseDto>>;

public sealed class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CourseCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.CreditHours).InclusiveBetween(0.5m, 6.0m);
        RuleFor(x => x.Department).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AcademicSession).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Semester).NotEmpty().MaximumLength(20);
    }
}

public sealed class CreateCourseCommandHandler(
    IUnitOfWork uow,
    IFileStorageService storage
) : ICommandHandler<CreateCourseCommand, ApiResponse<CourseDto>>
{
    public async ValueTask<ApiResponse<CourseDto>> Handle(
        CreateCourseCommand command, CancellationToken ct)
    {
        // 1. Check teacher profile is complete
        var teacher = await uow.Users.GetWithProfileAsync(command.TeacherId, ct)
            ?? throw new NotFoundException("User", command.TeacherId);

        if (!teacher.IsProfileComplete)
            throw new ProfileIncompleteException();

        // 2. Check quota
        var quota = await uow.TeacherQuotas.GetActiveQuotaAsync(command.TeacherId, ct)
            ?? throw new QuotaExceededException();

        quota.ConsumeOne();
        uow.TeacherQuotas.Update(quota);

        // 3. Upload cover image or use default
        var coverUrl = storage.GetDefaultCoverImageUrl();
        if (command.CoverImageStream is not null && command.CoverImageFileName is not null)
        {
            coverUrl = await storage.UploadAsync(
                command.CoverImageStream, command.CoverImageFileName,
                "covers", ct);
        }

        // 4. Create course
        var course = Course.Create(
            command.Title, command.CourseCode, command.CreditHours,
            command.Department, command.AcademicSession, command.Semester,
            command.Section, command.CourseType, command.Description,
            coverUrl, command.TeacherId);

        await uow.Courses.AddAsync(course, ct);
        await uow.SaveChangesAsync(ct);

        return ApiResponse<CourseDto>.Ok(MapToDto(course, teacher), "Course created successfully.");
    }

    private static CourseDto MapToDto(Course c, User teacher) =>
        new(c.Id, c.Title, c.CourseCode, c.CreditHours, c.Department,
            c.AcademicSession, c.Semester, c.Section, c.CourseType.ToString(),
            c.Description, c.CoverImageUrl, c.JoiningCode, c.TeacherId,
            teacher.Profile?.FullName ?? teacher.Email, c.IsArchived, 0, c.CreatedAt);
}
