using EduNexis.Application.DTOs;
using EduNexis.Application.Features.Profile.Commands;

namespace EduNexis.Application.Features.Public.Queries;

public record GetPublicFacultyBySlugQuery(string Slug) : IQuery<ApiResponse<PublicFacultyProfileDto>>;

public sealed class GetPublicFacultyBySlugQueryHandler(IUnitOfWork uow, AppDbContext db)
    : IQueryHandler<GetPublicFacultyBySlugQuery, ApiResponse<PublicFacultyProfileDto>>
{
    public async ValueTask<ApiResponse<PublicFacultyProfileDto>> Handle(
        GetPublicFacultyBySlugQuery query, CancellationToken ct)
    {
        var profile = await uow.UserProfiles.GetBySlugAsync(query.Slug, ct);
        if (profile is null)
            return ApiResponse<PublicFacultyProfileDto>.Fail("Faculty profile not found.");

        var user = await uow.Users.GetByIdAsync(profile.UserId, ct);
        if (user is null || user.Role != UserRole.Teacher || !user.IsActive)
            return ApiResponse<PublicFacultyProfileDto>.Fail("Faculty profile not found.");

        // Education + publications via direct DbContext (no auth, no domain logic needed)
        var education = await db.UserEducations.AsNoTracking()
            .Where(e => e.UserId == user.Id)
            .OrderByDescending(e => e.StartYear)
            .Select(e => new UserEducationDto(
                e.Id, e.Institution, e.Degree, e.FieldOfStudy,
                e.StartYear, e.EndYear, e.Description))
            .ToListAsync(ct);

        var publications = await db.UserPublications.AsNoTracking()
            .Where(p => p.UserId == user.Id)
            .OrderBy(p => p.OrderIndex)
            .ThenByDescending(p => p.Year)
            .Select(p => new UserPublicationDto(
                p.Id, p.Title, p.Authors, p.Venue,
                p.Year, p.Url, p.Type.ToString(), p.OrderIndex))
            .ToListAsync(ct);

        var courses = await db.Courses.AsNoTracking()
            .Where(c => c.TeacherId == user.Id)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new PublicCourseDto(
                c.Id, c.Title, c.CourseCode, c.Department,
                c.Semester, c.CourseType.ToString(), c.IsArchived))
            .ToListAsync(ct);

        var coursesTaught = courses.Count(c => !c.IsArchived);

        var dto = new PublicFacultyProfileDto(
            Slug: profile.PublicSlug ?? string.Empty,
            FullName: profile.FullName,
            Department: profile.Department,
            Designation: profile.Designation,
            Bio: profile.Bio,
            Headline: profile.Headline,
            ProfilePhotoUrl: profile.ProfilePhotoUrl,
            CoverPhotoUrl: profile.CoverPhotoUrl,
            OfficeLocation: profile.OfficeLocation,
            OfficeHours: profile.OfficeHours,
            ResearchInterestsCsv: profile.ResearchInterestsCsv,
            FieldsOfWorkCsv: profile.FieldsOfWorkCsv,
            LinkedInUrl: profile.LinkedInUrl,
            FacebookUrl: profile.FacebookUrl,
            TwitterUrl: profile.TwitterUrl,
            GitHubUrl: profile.GitHubUrl,
            WebsiteUrl: profile.WebsiteUrl,
            Education: education,
            Publications: publications,
            Courses: courses,
            CoursesTaught: coursesTaught
        );

        return ApiResponse<PublicFacultyProfileDto>.Ok(dto);
    }
}