using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Public.Queries;

public record GetPublicStatsQuery() : IQuery<ApiResponse<PublicStatsDto>>;

public sealed class GetPublicStatsQueryHandler(IUnitOfWork uow)
    : IQueryHandler<GetPublicStatsQuery, ApiResponse<PublicStatsDto>>
{
    public async ValueTask<ApiResponse<PublicStatsDto>> Handle(
        GetPublicStatsQuery query, CancellationToken ct)
    {
        var teacherCount = await uow.Users.CountActiveByRoleAsync(UserRole.Teacher, ct);
        var studentCount = await uow.Users.CountActiveByRoleAsync(UserRole.Student, ct);
        var courseCount = await uow.Courses.CountActiveAsync(ct);

        // Assignment count via generic repo (no dedicated interface yet)
        var assignments = await uow.GetRepository<Assignment>().GetAllAsync(ct);
        var assignmentCount = assignments.Count();

        return ApiResponse<PublicStatsDto>.Ok(new PublicStatsDto(
            teacherCount, studentCount, courseCount, assignmentCount));
    }
}