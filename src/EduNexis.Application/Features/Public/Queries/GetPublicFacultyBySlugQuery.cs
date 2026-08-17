using EduNexis.Application.DTOs;

namespace EduNexis.Application.Features.Public.Queries;

public record GetPublicFacultyBySlugQuery(string Slug) : IQuery<ApiResponse<PublicFacultyProfileDto>>;

public sealed class GetPublicFacultyBySlugQueryHandler(IUnitOfWork uow)
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

        // Education + Publications via generic repo (no dedicated interface needed)
        var educationRows = await uow.GetRepository<UserEducation>()
            .FindAsync(e => e.UserId == user.Id, ct);
        var education = educationRows
            .OrderByDescending(e => e.StartYear)
            .Select(e => new UserEducationDto(
                e.Id, e.Institution, e.Degree, e.FieldOfStudy,
                e.StartYear, e.EndYear, e.Description))
            .ToList();

        var publicationRows = await uow.GetRepository<UserPublication>()
            .FindAsync(p => p.UserId == user.Id, ct);
        var publications = publicationRows
            .OrderBy(p => p.OrderIndex)
            .ThenByDescending(p => p.Year)
            .Select(p => new UserPublicationDto(
                p.Id, p.Title, p.Authors, p.Venue,
                p.Year, p.Url, p.Type.ToString(), p.OrderIndex,
                p.IsPdfPublic ? p.PdfUrl : null,
                p.IsPdfPublic ? p.PdfSizeBytes : null,
                p.IsPdfPublic ? p.PdfUploadedAt : null,
                p.IsPdfPublic))
            .ToList();

        // Courses via dedicated repo
        var courseRows = await uow.Courses.GetByTeacherAsync(user.Id, ct);
        var courses = courseRows
            // Courses the teacher has deleted (30-day trash) or that were purged
            // must never surface on a public profile.
            .Where(c => !c.IsDeletedByOwner && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new PublicCourseDto(
                c.Id, c.Title, c.CourseCode, c.Department, c.AcademicSession,
                c.Semester, c.CourseType.ToString(), c.IsArchived))
            .ToList();

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