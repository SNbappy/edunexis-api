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
            // CourseMate = share at least one active course membership or teacher relationship
            var viewerMemberships = await uow.CourseMembers.FindAsync(
                m => m.UserId == viewerId && m.IsActive, ct);
            var viewerCourseIds = viewerMemberships.Select(m => m.CourseId).ToHashSet();

            var ownerMemberships = await uow.CourseMembers.FindAsync(
                m => m.UserId == query.UserId && m.IsActive, ct);
            var ownerCourseIds = ownerMemberships.Select(m => m.CourseId).ToHashSet();

            // Also check teacher-of-viewer's-course relationship
            var viewerTaughtCourses = await uow.Courses.FindAsync(
                c => c.TeacherId == viewerId, ct);
            var viewerTaughtIds = viewerTaughtCourses.Select(c => c.Id).ToHashSet();

            var ownerTaughtCourses = await uow.Courses.FindAsync(
                c => c.TeacherId == query.UserId, ct);
            var ownerTaughtIds = ownerTaughtCourses.Select(c => c.Id).ToHashSet();

            var sharesCourse =
                viewerCourseIds.Overlaps(ownerCourseIds) ||       // both enrolled as students
                viewerCourseIds.Overlaps(ownerTaughtIds)  ||      // viewer student in owner's taught course
                viewerTaughtIds.Overlaps(ownerCourseIds);         // viewer teacher of owner's enrolled course

            relation = sharesCourse ? "CourseMate" : "Stranger";
            canSeeContact = sharesCourse;
        }

        // ── Education (always visible) ──
        var educations = (await uow.GetRepository<UserEducation>()
            .FindAsync(e => e.UserId == query.UserId, ct))
            .OrderByDescending(e => e.StartYear)
            .ToList();

        // ── Courses visibility rules ──
        // Teachers: their taught courses are always visible (it's their public portfolio).
        // Students: enrolled courses only visible to Self + CourseMate (privacy).
        List<PublicCourseDto> courses;
        if (user.Role == UserRole.Teacher)
        {
            var taught = await uow.Courses.FindAsync(
                c => c.TeacherId == query.UserId && !c.IsArchived, ct);
            courses = taught.Select(c => new PublicCourseDto(
                c.Id, c.Title, c.CourseCode, c.Department, c.Semester, c.CourseType.ToString()
            )).ToList();
        }
        else if (canSeeContact)
        {
            var memberships = await uow.CourseMembers.FindAsync(
                cm => cm.UserId == query.UserId && cm.IsActive, ct);
            var ids = memberships.Select(cm => cm.CourseId).ToHashSet();
            var enrolled = await uow.Courses.FindAsync(
                c => ids.Contains(c.Id) && !c.IsArchived, ct);
            courses = enrolled.Select(c => new PublicCourseDto(
                c.Id, c.Title, c.CourseCode, c.Department, c.Semester, c.CourseType.ToString()
            )).ToList();
        }
        else
        {
            courses = new List<PublicCourseDto>();
        }

        return ApiResponse<PublicProfileDto>.Ok(new PublicProfileDto(
            UserId:           query.UserId,
            FullName:         profile.FullName,
            Department:       profile.Department,
            Designation:      profile.Designation,
            StudentId:        canSeeContact ? profile.StudentId : null,
            Bio:              profile.Bio,
            ProfilePhotoUrl:  profile.ProfilePhotoUrl,
            CoverPhotoUrl:    profile.CoverPhotoUrl,
            PhoneNumber:      canSeeContact ? profile.PhoneNumber : null,
            LinkedInUrl:      profile.LinkedInUrl,
            FacebookUrl:      profile.FacebookUrl,
            TwitterUrl:       profile.TwitterUrl,
            GitHubUrl:        profile.GitHubUrl,
            WebsiteUrl:       profile.WebsiteUrl,
            Email:            canSeeContact ? user.Email : null,
            Role:             user.Role.ToString(),
            Education:        educations.Select(e => new UserEducationDto(
                e.Id, e.Institution, e.Degree, e.FieldOfStudy,
                e.StartYear, e.EndYear, e.Description)).ToList(),
            Courses:          courses,
            ViewerRelation:   relation
        ));
    }
}
