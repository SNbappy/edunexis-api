using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Public.Queries;

/// <summary>
/// Returns paginated list of teachers who have opted into public visibility.
/// Filterable by department. No auth required at controller level.
/// </summary>
public record GetPublicFacultyListQuery(
    string? Department,
    int Page,
    int PageSize
) : IQuery<ApiResponse<List<PublicFacultyCardDto>>>;

public sealed class GetPublicFacultyListQueryHandler(IUnitOfWork uow, AppDbContext db)
    : IQueryHandler<GetPublicFacultyListQuery, ApiResponse<List<PublicFacultyCardDto>>>
{
    public async ValueTask<ApiResponse<List<PublicFacultyCardDto>>> Handle(
        GetPublicFacultyListQuery query, CancellationToken ct)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 60);
        var page = Math.Max(1, query.Page);

        // Join UserProfiles → Users (Teacher only) → CourseMembers count
        // Direct DbContext access here — public read, no domain methods needed
        var profilesQuery =
            from p in db.UserProfiles.AsNoTracking()
            join u in db.Users.AsNoTracking() on p.UserId equals u.Id
            where p.IsPublicProfile && u.Role == UserRole.Teacher && u.IsActive
            select new { Profile = p, User = u };

        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            profilesQuery = profilesQuery.Where(x => x.Profile.Department == query.Department);
        }

        var rows = await profilesQuery
            .OrderBy(x => x.Profile.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Course counts (one query per teacher is fine for now; we have <50 teachers)
        var teacherIds = rows.Select(r => r.User.Id).ToList();
        var courseCounts = await db.Courses.AsNoTracking()
            .Where(c => teacherIds.Contains(c.TeacherId) && !c.IsArchived)
            .GroupBy(c => c.TeacherId)
            .Select(g => new { TeacherId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TeacherId, x => x.Count, ct);

        var dtos = rows.Select(r => new PublicFacultyCardDto(
            Slug: r.Profile.PublicSlug ?? string.Empty,
            FullName: r.Profile.FullName,
            Department: r.Profile.Department,
            Designation: r.Profile.Designation,
            Headline: r.Profile.Headline,
            ProfilePhotoUrl: r.Profile.ProfilePhotoUrl,
            CoursesTaught: courseCounts.GetValueOrDefault(r.User.Id, 0)
        )).ToList();

        return ApiResponse<List<PublicFacultyCardDto>>.Ok(dtos);
    }
}