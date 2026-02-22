namespace EduNexis.Application.Features.Materials.Queries;

public record MaterialDto(
    Guid Id,
    string Title,
    string Type,
    string? FileUrl,
    string? EmbedUrl,
    string? Description,
    string? Category,
    bool IsPinned,
    int DownloadCount,
    DateTime CreatedAt
);

public record GetMaterialsQuery(
    Guid CourseId,
    string? Category = null
) : IQuery<ApiResponse<List<MaterialDto>>>;

public sealed class GetMaterialsQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetMaterialsQuery, ApiResponse<List<MaterialDto>>>
{
    public async ValueTask<ApiResponse<List<MaterialDto>>> Handle(
        GetMaterialsQuery query, CancellationToken ct)
    {
        var materials = await uow.GetRepository<Material>()
            .FindAsync(m => m.CourseId == query.CourseId, ct);

        if (!string.IsNullOrEmpty(query.Category))
            materials = materials.Where(m => m.Category == query.Category);

        var dtos = materials
            .OrderByDescending(m => m.IsPinned)
            .ThenByDescending(m => m.CreatedAt)
            .Select(m => new MaterialDto(
                m.Id, m.Title, m.Type.ToString(), m.FileUrl, m.EmbedUrl,
                m.Description, m.Category, m.IsPinned, m.DownloadCount, m.CreatedAt))
            .ToList();

        return ApiResponse<List<MaterialDto>>.Ok(dtos);
    }
}
