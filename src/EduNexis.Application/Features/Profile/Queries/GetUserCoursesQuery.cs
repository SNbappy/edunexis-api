using EduNexis.Application.Abstractions;
using EduNexis.Application.DTOs;
using EduNexis.Domain.Enums;

namespace EduNexis.Application.Features.Profile.Queries;

public record GetUserCoursesQuery(Guid UserId, string? Status)
    : IQuery<ApiResponse<UserCoursesDto>>;

public record UserCoursesDto(
    List<PublicCourseDto> Running,
    List<PublicCourseDto> Archived
);

public sealed class GetUserCoursesQueryHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser
) : IQueryHandler<GetUserCoursesQuery, ApiResponse<UserCoursesDto>>
{
    public async ValueTask<ApiResponse<UserCoursesDto>> Handle(
        GetUserCoursesQuery query, CancellationToken ct)
    {
        var user = await uow.Users.GetWithProfileAsync(query.UserId, ct)
            ?? throw new NotFoundException("User", query.UserId);

        var viewerId = Guid.Parse(currentUser.UserId);
        var isSelf = viewerId == query.UserId;

        // Visibility check for students (teachers' courses are public portfolio)
        if (!isSelf && user.Role == UserRole.Student)
        {
            var viewerMemberships = await uow.CourseMembers.FindAsync(
                m => m.UserId == viewerId && m.IsActive, ct);
            var viewerCourseIds = viewerMemberships.Select(m => m.CourseId).ToHashSet();
            var ownerMemberships = await uow.CourseMembers.FindAsync(
                m => m.UserId == query.UserId && m.IsActive, ct);
            var ownerCourseIds = ownerMemberships.Select(m => m.CourseId).ToHashSet();
            var viewerTaught = await uow.Courses.FindAsync(c => c.TeacherId == viewerId, ct);
            var viewerTaughtIds = viewerTaught.Select(c => c.Id).ToHashSet();

            var shares = viewerCourseIds.Overlaps(ownerCourseIds) ||
                         viewerTaughtIds.Overlaps(ownerCourseIds);

            if (!shares)
                return ApiResponse<UserCoursesDto>.Ok(new UserCoursesDto(new(), new()));
        }

        IEnumerable<Domain.Entities.Course> all;
        if (user.Role == UserRole.Teacher)
        {
            all = await uow.Courses.FindAsync(c => c.TeacherId == query.UserId, ct);
        }
        else
        {
            var memberships = await uow.CourseMembers.FindAsync(
                cm => cm.UserId == query.UserId && cm.IsActive, ct);
            var ids = memberships.Select(cm => cm.CourseId).ToHashSet();
            all = await uow.Courses.FindAsync(c => ids.Contains(c.Id), ct);
        }

        // Deleted courses never appear in a profile's course lists — they live
        // only in the owner's "Recently deleted" view until restored or purged.
        var allList = all
            .Where(c => !c.IsDeletedByOwner && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        List<PublicCourseDto> ToDto(IEnumerable<Domain.Entities.Course> src) =>
            src.Select(c => new PublicCourseDto(
                c.Id, c.Title, c.CourseCode, c.Department, c.AcademicSession,
                c.Semester, c.CourseType.ToString(), c.IsArchived)).ToList();

        var running = ToDto(allList.Where(c => !c.IsArchived));
        var archived = ToDto(allList.Where(c => c.IsArchived));

        // Optional status filter
        if (query.Status?.Equals("running", StringComparison.OrdinalIgnoreCase) == true)
            archived = new();
        else if (query.Status?.Equals("archived", StringComparison.OrdinalIgnoreCase) == true)
            running = new();

        return ApiResponse<UserCoursesDto>.Ok(new UserCoursesDto(running, archived));
    }
}