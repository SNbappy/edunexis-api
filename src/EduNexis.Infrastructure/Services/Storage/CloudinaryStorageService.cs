using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace EduNexis.Infrastructure.Services.Storage;

public class CloudinaryStorageService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly string _defaultCoverUrl;

    private static readonly HashSet<string> ImageExtensions =
        [".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp"];

    public CloudinaryStorageService(IConfiguration configuration)
    {
        var account = new Account(
            configuration["Cloudinary:CloudName"]!,
            configuration["Cloudinary:ApiKey"]!,
            configuration["Cloudinary:ApiSecret"]!);

        _cloudinary = new Cloudinary(account);
        _defaultCoverUrl = configuration["Cloudinary:DefaultCoverUrl"]
            ?? "https://res.cloudinary.com/demo/image/upload/sample.jpg";
    }

    public async Task<string> UploadAsync(
        Stream fileStream, string fileName,
        string folder, CancellationToken ct = default)
    {
        var result = await UploadWithIdAsync(fileStream, fileName, folder, ct);
        return result.Url;
    }

    public async Task<FileUploadResult> UploadWithIdAsync(
        Stream fileStream, string fileName,
        string folder, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var isImage = ImageExtensions.Contains(extension);

        if (isImage)
        {
            var imageParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true
            };

            var imageResult = await _cloudinary.UploadAsync(imageParams);

            if (imageResult.Error is not null)
                throw new DomainException($"Image upload failed: {imageResult.Error.Message}");

            return new FileUploadResult(
                Url: imageResult.SecureUrl.ToString(),
                PublicId: imageResult.PublicId,
                SizeBytes: imageResult.Bytes);
        }
        else
        {
            var rawParams = new RawUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true
            };

            var rawResult = await _cloudinary.UploadAsync(rawParams);

            if (rawResult.Error is not null)
                throw new DomainException($"File upload failed: {rawResult.Error.Message}");

            return new FileUploadResult(
                Url: rawResult.SecureUrl.ToString(),
                PublicId: rawResult.PublicId,
                SizeBytes: rawResult.Bytes);
        }
    }

    public async Task DeleteAsync(string publicId, CancellationToken ct = default)
    {
        // For raw resources (PDFs), need to specify ResourceType
        var deleteParams = new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Raw
        };
        await _cloudinary.DestroyAsync(deleteParams);

        // Also try image deletion as fallback (for legacy uploads with no resource type)
        // — silently ignored if not found
        var imageDeleteParams = new DeletionParams(publicId)
        {
            ResourceType = ResourceType.Image
        };
        await _cloudinary.DestroyAsync(imageDeleteParams);
    }

    public string GetDefaultCoverImageUrl() => _defaultCoverUrl;
}