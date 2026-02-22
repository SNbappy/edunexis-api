namespace EduNexis.Domain.Interfaces.Services;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName,
        string folder, CancellationToken ct = default);
    Task DeleteAsync(string publicId, CancellationToken ct = default);
    string GetDefaultCoverImageUrl();
}
