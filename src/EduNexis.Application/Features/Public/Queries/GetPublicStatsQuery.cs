using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Public.Queries;

public record GetPublicStatsQuery() : IQuery<ApiResponse<PublicStatsDto>>;

public sealed class GetPublicStatsQueryHandler(AppDbContext db)
    : IQueryHandler<GetPublicStatsQuery, ApiResponse<PublicStatsDto>>
{
    public async ValueTask<ApiResponse<PublicStatsDto>> Handle(
        GetPublicStatsQuery query, CancellationToken ct)
    {
        var teacherCount = await db.Users.AsNoTracking()
            .CountAsync(u => u.Role == UserRole.Teacher && u.IsActive, ct);
        var studentCount = await db.Users.AsNoTracking()
            .CountAsync(u => u.Role == UserRole.Student && u.IsActive, ct);
        var courseCount = await db.Courses.AsNoTracking()
            .CountAsync(c => !c.IsArchived, ct);
        var assignmentCount = await db.Assignments.AsNoTracking()
            .CountAsync(ct);

        return ApiResponse<PublicStatsDto>.Ok(new PublicStatsDto(
            teacherCount, studentCount, courseCount, assignmentCount));
    }
}