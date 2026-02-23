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

            return imageResult.SecureUrl.ToString();
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

            return rawResult.SecureUrl.ToString();
        }
    }

    public async Task DeleteAsync(string publicId, CancellationToken ct = default)
    {
        var deleteParams = new DeletionParams(publicId);
        await _cloudinary.DestroyAsync(deleteParams);
    }

    public string GetDefaultCoverImageUrl() => _defaultCoverUrl;
}
