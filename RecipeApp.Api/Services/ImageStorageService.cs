using Microsoft.Extensions.Options;
using RecipeApp.Api.Data.Entities;
using RecipeApp.Api.Exceptions;
using RecipeApp.Api.Options;

namespace RecipeApp.Api.Services;

public class ImageStorageService(
    IWebHostEnvironment env,
    IOptions<ImageStorageOptions> options,
    IOptions<FreeTierOptions> freeTierOptions,
    ILogger<ImageStorageService> logger)
{
    private string UploadRoot =>
        Path.IsPathRooted(options.Value.UploadPath)
            ? options.Value.UploadPath
            : Path.Combine(env.ContentRootPath, options.Value.UploadPath);

    public void ValidateImages(IFormFileCollection images)
    {
        if (images.Count == 0)
            throw new AppValidationException("At least one image is required.");

        if (images.Count > freeTierOptions.Value.MaxImagesPerScan)
            throw new AppValidationException(
                $"Maximum {freeTierOptions.Value.MaxImagesPerScan} images per scan.");

        var maxBytes = options.Value.MaxFileSizeMb * 1024L * 1024L;

        foreach (var image in images)
        {
            if (image.Length > maxBytes)
                throw new AppValidationException(
                    $"Image '{image.FileName}' exceeds the {options.Value.MaxFileSizeMb}MB size limit.");

            if (!options.Value.AllowedContentTypes.Contains(image.ContentType?.ToLowerInvariant()))
                throw new AppValidationException(
                    $"Image '{image.FileName}' has an unsupported format. Allowed: JPEG, PNG, WebP, HEIC.");
        }
    }

    public async Task<List<ScanImage>> SaveImagesAsync(Guid scanId, IFormFileCollection images)
    {
        var scanDir = Path.Combine(UploadRoot, scanId.ToString());
        Directory.CreateDirectory(scanDir);

        var saved = new List<ScanImage>();

        foreach (var image in images)
        {
            var ext = Path.GetExtension(image.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(scanDir, fileName);

            await using var stream = File.Create(filePath);
            await image.CopyToAsync(stream);

            logger.LogInformation("Saved image {FileName} for scan {ScanId}", fileName, scanId);

            saved.Add(new ScanImage
            {
                Id = Guid.NewGuid(),
                FridgeScanId = scanId,
                ImagePath = filePath,
                OriginalFileName = image.FileName,
                ContentType = image.ContentType ?? "image/jpeg",
                SizeBytes = image.Length,
                CreatedAtUtc = DateTime.UtcNow,
            });
        }

        return saved;
    }
}
