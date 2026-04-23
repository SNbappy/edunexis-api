using EduNexis.Application.DTOs;
using EduNexis.Application.Extensions;
using EduNexis.Domain.Entities;
using EduNexis.Domain.Enums;

namespace EduNexis.Application.Features.Courses.Queries;

public record GetMyCoursesQuery(
    Guid UserId,
    UserRole Role
) : IQuery<ApiResponse<MyCoursesDto>>;

public sealed class GetMyCoursesQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetMyCoursesQuery, ApiResponse<MyCoursesDto>>
{
    public async ValueTask<ApiResponse<MyCoursesDto>> Handle(
        GetMyCoursesQuery query, CancellationToken ct)
    {
        // ── Enrolled courses ──
        IEnumerable<Course> enrolledCourses = query.Role == UserRole.Teacher
            ? await uow.Courses.GetByTeacherAsync(query.UserId, ct)
            : await uow.Courses.GetByStudentAsync(query.UserId, ct);

        var enrolled = new List<CourseSummaryDto>();
        foreach (var course in enrolledCourses)
        {
            var teacher = await uow.Users.GetWithProfileAsync(course.TeacherId, ct);
            var members = await uow.CourseMembers.FindAsync(
                m => m.CourseId == course.Id && m.IsActive, ct);

            enrolled.Add(course.ToSummaryDto(
                teacher?.Profile?.FullName ?? teacher?.Email ?? "Unknown",
                teacher?.Profile?.ProfilePhotoUrl,
                members.Count()));
        }

        var pending  = new List<PendingCourseDto>();
        var rejected = new List<RejectedCourseDto>();

        // ── Pending + rejected only matter for students ──
        if (query.Role == UserRole.Student)
        {
            var requests = await uow.JoinRequests.FindAsync(
                r => r.StudentId == query.UserId, ct);

            // Any course the student is currently enrolled in should never
            // surface on the rejected list, regardless of prior history.
            var enrolledCourseIds = new HashSet<Guid>(enrolled.Select(e => e.Id));

            foreach (var req in requests)
            {
                if (req.Status == JoinRequestStatus.Approved) continue;
                if (req.Status == JoinRequestStatus.Rejected && req.IsDismissedByStudent) continue;
                if (req.Status == JoinRequestStatus.Rejected && enrolledCourseIds.Contains(req.CourseId)) continue;

                var course = await uow.Courses.GetByIdAsync(req.CourseId, ct);
                if (course is null) continue;

                var teacher = await uow.Users.GetWithProfileAsync(course.TeacherId, ct);
                var teacherName = teacher?.Profile?.FullName ?? teacher?.Email ?? "Unknown";
                var teacherPhoto = teacher?.Profile?.ProfilePhotoUrl;

                if (req.Status == JoinRequestStatus.Pending)
                {
                    pending.Add(new PendingCourseDto(
                        course.Id, course.Title, course.CourseCode,
                        course.Department, course.Semester, course.CourseType.ToString(),
                        teacherName, teacherPhoto,
                        req.Id, req.CreatedAt));
                }
                else // Rejected && !dismissed
                {
                    rejected.Add(new RejectedCourseDto(
                        course.Id, course.Title, course.CourseCode,
                        course.Department, course.Semester, course.CourseType.ToString(),
                        teacherName, teacherPhoto,
                        req.Id, req.CreatedAt, req.ReviewedAt));
                }
            }
        }

        return ApiResponse<MyCoursesDto>.Ok(
            new MyCoursesDto(enrolled, pending, rejected));
    }
}

