using EduNexis.Application.Abstractions;
using EduNexis.Application.DTOs;
using EduNexis.Domain.Entities;
using EduNexis.Domain.Enums;

namespace EduNexis.Application.Features.Profile.Queries;

public record GetPublicProfileQuery(Guid UserId) : IQuery<ApiResponse<PublicProfileDto>>;

public sealed class GetPublicProfileQueryHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser
) : IQueryHandler<GetPublicProfileQuery, ApiResponse<PublicProfileDto>>
{
    public async ValueTask<ApiResponse<PublicProfileDto>> Handle(
        GetPublicProfileQuery query, CancellationToken ct)
    {
        var user = await uow.Users.GetWithProfileAsync(query.UserId, ct)
            ?? throw new NotFoundException("User", query.UserId);

        var profile = user.Profile
            ?? throw new NotFoundException("UserProfile", query.UserId);

        var viewerId = Guid.Parse(currentUser.UserId);

        // ── Determine viewer relation ──
        string relation;
        bool canSeeContact;

        if (viewerId == query.UserId)
        {
            relation = "Self";
            canSeeContact = true;
        }
        else
        {
            var viewerMemberships = await uow.CourseMembers.FindAsync(
                m => m.UserId == viewerId && m.IsActive, ct);
            var viewerCourseIds = viewerMemberships.Select(m => m.CourseId).ToHashSet();

            var ownerMemberships = await uow.CourseMembers.FindAsync(
                m => m.UserId == query.UserId && m.IsActive, ct);
            var ownerCourseIds = ownerMemberships.Select(m => m.CourseId).ToHashSet();

            var viewerTaughtCourses = await uow.Courses.FindAsync(
                c => c.TeacherId == viewerId, ct);
            var viewerTaughtIds = viewerTaughtCourses.Select(c => c.Id).ToHashSet();

            var ownerTaughtCourses = await uow.Courses.FindAsync(
                c => c.TeacherId == query.UserId, ct);
            var ownerTaughtIds = ownerTaughtCourses.Select(c => c.Id).ToHashSet();

            var sharesCourse =
                viewerCourseIds.Overlaps(ownerCourseIds) ||
                viewerCourseIds.Overlaps(ownerTaughtIds) ||
                viewerTaughtIds.Overlaps(ownerCourseIds);

            relation = sharesCourse ? "CourseMate" : "Stranger";
            canSeeContact = sharesCourse;
        }

        // ── Education (always visible) ──
        var educations = (await uow.GetRepository<UserEducation>()
            .FindAsync(e => e.UserId == query.UserId, ct))
            .OrderByDescending(e => e.StartYear)
            .ToList();

        // ── Publications (always visible) ──
        var publications = (await uow.GetRepository<UserPublication>()
            .FindAsync(p => p.UserId == query.UserId, ct))
            .OrderBy(p => p.OrderIndex)
            .ThenByDescending(p => p.Year)
            .ToList();

        // ── Courses ──
        // Show preview list (max 6) + counts. Full lists fetched on /users/{id}/courses page.
        List<PublicCourseDto> courses = new();
        int runningCount = 0;
        int archivedCount = 0;

        if (user.Role == UserRole.Teacher)
        {
            // Deleted courses are excluded: a course in the teacher's recycle
            // bin was still listed on their profile, and counted in the
            // running/archived totals.
            var taught = await uow.Courses.FindAsync(
                c => c.TeacherId == query.UserId && !c.IsDeletedByOwner && !c.IsDeleted, ct);
            var taughtList = taught.ToList();
            runningCount = taughtList.Count(c => !c.IsArchived);
            archivedCount = taughtList.Count(c => c.IsArchived);

            courses = taughtList
                .OrderByDescending(c => !c.IsArchived)
                .ThenByDescending(c => c.CreatedAt)
                .Take(6)
                .Select(c => new PublicCourseDto(
                    c.Id, c.Title, c.CourseCode, c.Department, c.AcademicSession,
                    c.Semester, c.CourseType.ToString(), c.IsArchived))
                .ToList();
        }
        else if (canSeeContact)
        {
            var memberships = await uow.CourseMembers.FindAsync(
                cm => cm.UserId == query.UserId && cm.IsActive, ct);
            var ids = memberships.Select(cm => cm.CourseId).ToHashSet();
            var enrolled = await uow.Courses.FindAsync(c => ids.Contains(c.Id), ct);
            var enrolledList = enrolled.ToList();
            runningCount = enrolledList.Count(c => !c.IsArchived);
            archivedCount = enrolledList.Count(c => c.IsArchived);

            courses = enrolledList
                .OrderByDescending(c => !c.IsArchived)
                .ThenByDescending(c => c.CreatedAt)
                .Take(6)
                .Select(c => new PublicCourseDto(
                    c.Id, c.Title, c.CourseCode, c.Department, c.AcademicSession,
                    c.Semester, c.CourseType.ToString(), c.IsArchived))
                .ToList();
        }

        return ApiResponse<PublicProfileDto>.Ok(new PublicProfileDto(
            UserId: query.UserId,
            FullName: profile.FullName,
            Department: profile.Department,
            Designation: profile.Designation,
            StudentId: canSeeContact ? profile.StudentId : null,
            Bio: profile.Bio,
            Headline: profile.Headline,
            ProfilePhotoUrl: profile.ProfilePhotoUrl,
            CoverPhotoUrl: profile.CoverPhotoUrl,
            PhoneNumber: canSeeContact ? profile.PhoneNumber : null,
            OfficeLocation: user.Role == UserRole.Teacher ? profile.OfficeLocation : null,
            OfficeHours: user.Role == UserRole.Teacher ? profile.OfficeHours : null,
            ResearchInterestsCsv: user.Role == UserRole.Teacher ? profile.ResearchInterestsCsv : null,
            FieldsOfWorkCsv: user.Role == UserRole.Teacher ? profile.FieldsOfWorkCsv : null,
            LinkedInUrl: profile.LinkedInUrl,
            FacebookUrl: profile.FacebookUrl,
            TwitterUrl: profile.TwitterUrl,
            GitHubUrl: profile.GitHubUrl,
            WebsiteUrl: profile.WebsiteUrl,
            Email: canSeeContact ? user.Email : null,
            Role: user.Role.ToString(),
            Education: educations.Select(e => new UserEducationDto(
                                      e.Id, e.Institution, e.Degree, e.FieldOfStudy,
                                      e.StartYear, e.EndYear, e.Description)).ToList(),
            Publications: publications.Select(p => new UserPublicationDto(
                                      p.Id, p.Title, p.Authors, p.Venue, p.Year,
                                      p.Url, p.Type.ToString(), p.OrderIndex,
                                      p.PdfUrl, p.PdfSizeBytes, p.PdfUploadedAt, p.IsPdfPublic)).ToList(),
            Courses: courses,
            RunningCoursesCount: runningCount,
            ArchivedCoursesCount: archivedCount,
            ViewerRelation: relation
        ));
    }
}