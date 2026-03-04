using EduNexis.Application.DTOs;
using EduNexis.Application.Features.Profile.Commands;
using EduNexis.Domain.Entities;
using EduNexis.Domain.Enums;

namespace EduNexis.Application.Features.Profile.Queries;

public record GetPublicProfileQuery(Guid UserId) : IQuery<ApiResponse<PublicProfileDto>>;

public sealed class GetPublicProfileQueryHandler(IUnitOfWork uow)
    : IQueryHandler<GetPublicProfileQuery, ApiResponse<PublicProfileDto>>
{
    public async ValueTask<ApiResponse<PublicProfileDto>> Handle(
        GetPublicProfileQuery query, CancellationToken ct)
    {
        var user = await uow.Users.GetWithProfileAsync(query.UserId, ct)
            ?? throw new NotFoundException("User", query.UserId);
        var profile = user.Profile ?? throw new NotFoundException("UserProfile", query.UserId);

        var educations = (await uow.GetRepository<UserEducation>()
            .FindAsync(e => e.UserId == query.UserId, ct))
            .OrderByDescending(e => e.StartYear).ToList();

        List<PublicCourseDto> courses;
        if (user.Role == UserRole.Teacher)
        {
            var tc = await uow.Courses.FindAsync(c => c.TeacherId == query.UserId && !c.IsArchived, ct);
            courses = tc.Select(c => new PublicCourseDto(c.Id, c.Title, c.CourseCode, c.Department, c.Semester, c.CourseType.ToString())).ToList();
        }
        else
        {
            var memberships = await uow.CourseMembers.FindAsync(cm => cm.UserId == query.UserId && cm.IsActive, ct);
            var ids = memberships.Select(cm => cm.CourseId).ToHashSet();
            var ec = await uow.Courses.FindAsync(c => ids.Contains(c.Id) && !c.IsArchived, ct);
            courses = ec.Select(c => new PublicCourseDto(c.Id, c.Title, c.CourseCode, c.Department, c.Semester, c.CourseType.ToString())).ToList();
        }

        return ApiResponse<PublicProfileDto>.Ok(new PublicProfileDto(
            query.UserId,
            profile.FullName, profile.Department, profile.Designation,
            profile.StudentId, profile.Bio, profile.ProfilePhotoUrl, profile.CoverPhotoUrl,
            profile.PhoneNumber, profile.LinkedInUrl, profile.FacebookUrl,
            profile.TwitterUrl, profile.GitHubUrl, profile.WebsiteUrl,
            user.Email, user.Role.ToString(),
            educations.Select(e => new UserEducationDto(e.Id, e.Institution, e.Degree, e.FieldOfStudy, e.StartYear, e.EndYear, e.Description)).ToList(),
            courses));
    }
}
