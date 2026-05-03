using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Public.Queries;

public record GetPublicDepartmentsQuery() : IQuery<ApiResponse<List<string>>>;

public sealed class GetPublicDepartmentsQueryHandler(AppDbContext db)
    : IQueryHandler<GetPublicDepartmentsQuery, ApiResponse<List<string>>>
{
    public async ValueTask<ApiResponse<List<string>>> Handle(
        GetPublicDepartmentsQuery query, CancellationToken ct)
    {
        var departments = await (
            from p in db.UserProfiles.AsNoTracking()
            join u in db.Users.AsNoTracking() on p.UserId equals u.Id
            where p.IsPublicProfile && u.Role == UserRole.Teacher && u.IsActive
            select p.Department
        ).Distinct().OrderBy(d => d).ToListAsync(ct);

        return ApiResponse<List<string>>.Ok(departments);
    }
}