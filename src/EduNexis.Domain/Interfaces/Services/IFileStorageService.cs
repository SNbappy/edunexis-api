namespace EduNexis.Domain.Interfaces.Services;

public record FileUploadResult(string Url, string PublicId, long SizeBytes);

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName,
        string folder, CancellationToken ct = default);

    /// <summary>
    /// Upload and return both the URL and the Cloudinary public_id
    /// (needed for clean deletion later) plus file size.
    /// </summary>
    Task<FileUploadResult> UploadWithIdAsync(Stream fileStream, string fileName,
        string folder, CancellationToken ct = default);

    Task DeleteAsync(string publicId, CancellationToken ct = default);
    string GetDefaultCoverImageUrl();
}