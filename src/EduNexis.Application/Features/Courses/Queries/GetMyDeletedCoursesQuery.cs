namespace EduNexis.Application.Features.Courses.Queries;

public record DeletedCourseDto(
    Guid Id,
    string Title,
    string CourseCode,
    string Department,
    string Semester,
    DateTime DeletedAt,
    DateTime RestoreDeadline,
    bool CanRestore
);

public record GetMyDeletedCoursesQuery(Guid TeacherId) : IQuery<ApiResponse<List<DeletedCourseDto>>>;

public sealed class GetMyDeletedCoursesQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetMyDeletedCoursesQuery, ApiResponse<List<DeletedCourseDto>>>
{
    public async ValueTask<ApiResponse<List<DeletedCourseDto>>> Handle(
        GetMyDeletedCoursesQuery query, CancellationToken ct)
    {
        var deleted = await uow.Courses.GetDeletedByTeacherAsync(query.TeacherId, ct);

        var dtos = deleted.Select(c => new DeletedCourseDto(
            c.Id,
            c.Title,
            c.CourseCode,
            c.Department,
            c.Semester,
            c.DeletedByOwnerAt!.Value,
            c.DeletedByOwnerAt.Value.AddDays(30),
            !c.IsPastRestoreWindow
        )).ToList();

        return ApiResponse<List<DeletedCourseDto>>.Ok(dtos);
    }
}