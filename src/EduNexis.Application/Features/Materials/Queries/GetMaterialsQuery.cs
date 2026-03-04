namespace EduNexis.Application.Features.Materials.Queries;

public record MaterialDto(
    Guid Id,
    Guid CourseId,
    string Title,
    string Type,
    string? FileUrl,
    string? FileName,
    long? FileSizeBytes,
    string? EmbedUrl,
    string? Description,
    string? Category,
    bool IsPinned,
    int DownloadCount,
    DateTime UploadedAt,
    Guid? ParentFolderId,
    Guid UploadedById,
    string UploadedByName,
    int? ChildCount
);

public record GetMaterialsQuery(
    Guid CourseId,
    string? Category = null,
    Guid? ParentFolderId = null,
    bool Flatten = false
) : IQuery<ApiResponse<List<MaterialDto>>>;

public sealed class GetMaterialsQueryHandler(
    IUnitOfWork uow
) : IQueryHandler<GetMaterialsQuery, ApiResponse<List<MaterialDto>>>
{
    public async ValueTask<ApiResponse<List<MaterialDto>>> Handle(
        GetMaterialsQuery query, CancellationToken ct)
    {
        var repo = uow.GetRepository<Material>();

        IEnumerable<Material> materials;

        if (query.Flatten)
        {
            materials = await repo.FindAsync(m =>
                m.CourseId == query.CourseId &&
                !m.IsDeleted &&
                m.Type != MaterialType.Folder, ct);
        }
        else
        {
            materials = await repo.FindAsync(m =>
                m.CourseId == query.CourseId &&
                m.ParentFolderId == query.ParentFolderId &&
                !m.IsDeleted, ct);
        }

        if (!string.IsNullOrEmpty(query.Category))
            materials = materials.Where(m => m.Category == query.Category);

        var uploaderIds = materials.Select(m => m.UploadedById).Distinct().ToList();
        var allProfiles = await uow.UserProfiles.GetAllAsync(ct);
        var profileMap = allProfiles
            .Where(p => uploaderIds.Contains(p.UserId))
            .ToDictionary(p => p.UserId, p => p.FullName);

        var folderIds = materials
            .Where(m => m.Type == MaterialType.Folder)
            .Select(m => m.Id).ToList();

        var childCountMap = new Dictionary<Guid, int>();
        if (folderIds.Count > 0)
        {
            var children = await repo.FindAsync(m =>
                m.CourseId == query.CourseId &&
                m.ParentFolderId != null &&
                !m.IsDeleted, ct);
            foreach (var fid in folderIds)
                childCountMap[fid] = children.Count(m => m.ParentFolderId == fid);
        }

        var dtos = materials
            .OrderByDescending(m => m.IsPinned)
            .ThenByDescending(m => m.CreatedAt)
            .Select(m => new MaterialDto(
                m.Id, m.CourseId, m.Title, m.Type.ToString(),
                m.FileUrl, m.FileName, m.FileSizeBytes,
                m.EmbedUrl, m.Description, m.Category,
                m.IsPinned, m.DownloadCount, m.CreatedAt,
                m.ParentFolderId, m.UploadedById,
                profileMap.TryGetValue(m.UploadedById, out var name) ? name : "Unknown",
                m.Type == MaterialType.Folder && childCountMap.TryGetValue(m.Id, out var count) ? count : null
            )).ToList();

        return ApiResponse<List<MaterialDto>>.Ok(dtos);
    }
}
