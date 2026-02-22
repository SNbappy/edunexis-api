namespace EduNexis.Application.Features.Announcements.Queries;

using EduNexis.Application.Features.Announcements.Commands;

public record GetAnnouncementsQuery(Guid CourseId)
    : IQuery<ApiResponse<List<AnnouncementDto>>>;

public sealed class GetAnnouncementsQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetAnnouncementsQuery, ApiResponse<List<AnnouncementDto>>>
{
    public async ValueTask<ApiResponse<List<AnnouncementDto>>> Handle(
        GetAnnouncementsQuery query, CancellationToken ct)
    {
        var announcements = await uow.GetRepository<Announcement>()
            .FindAsync(a => a.CourseId == query.CourseId, ct);

        var dtos = new List<AnnouncementDto>();
        foreach (var a in announcements.OrderByDescending(a => a.CreatedAt))
        {
            var author = await uow.Users.GetWithProfileAsync(a.AuthorId, ct);
            dtos.Add(new AnnouncementDto(
                a.Id, a.CourseId, a.AuthorId,
                author?.Profile?.FullName ?? "Unknown",
                a.Content, a.AttachmentUrl, a.CreatedAt));
        }

        return ApiResponse<List<AnnouncementDto>>.Ok(dtos);
    }
}
