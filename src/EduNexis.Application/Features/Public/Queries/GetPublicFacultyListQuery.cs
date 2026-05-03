using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Public.Queries;

public record GetPublicFacultyListQuery(
    string? Department,
    int Page,
    int PageSize
) : IQuery<ApiResponse<List<PublicFacultyCardDto>>>;

public sealed class GetPublicFacultyListQueryHandler(IUnitOfWork uow)
    : IQueryHandler<GetPublicFacultyListQuery, ApiResponse<List<PublicFacultyCardDto>>>
{
    public async ValueTask<ApiResponse<List<PublicFacultyCardDto>>> Handle(
        GetPublicFacultyListQuery query, CancellationToken ct)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 60);
        var page = Math.Max(1, query.Page);

        var profiles = await uow.UserProfiles.ListPublicTeachersAsync(
            query.Department, page, pageSize, ct);

        if (profiles.Count == 0)
            return ApiResponse<List<PublicFacultyCardDto>>.Ok(new List<PublicFacultyCardDto>());

        var teacherIds = profiles.Select(p => p.UserId).ToList();
        var courseCounts = await uow.Courses.GetActiveCountsByTeacherIdsAsync(teacherIds, ct);

        var dtos = profiles.Select(p => new PublicFacultyCardDto(
            Slug: p.PublicSlug ?? string.Empty,
            FullName: p.FullName,
            Department: p.Department,
            Designation: p.Designation,
            Headline: p.Headline,
            ProfilePhotoUrl: p.ProfilePhotoUrl,
            CoursesTaught: courseCounts.GetValueOrDefault(p.UserId, 0)
        )).ToList();

        return ApiResponse<List<PublicFacultyCardDto>>.Ok(dtos);
    }
}