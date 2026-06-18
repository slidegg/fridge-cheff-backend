namespace RecipeApp.Api.Options;

public class ImageStorageOptions
{
    public const string Section = "ImageStorage";

    public string UploadPath { get; set; } = "uploads";
    public int MaxFileSizeMb { get; set; } = 8;
    public string[] AllowedContentTypes { get; set; } = ["image/jpeg", "image/png", "image/webp", "image/heic"];
}
