namespace BbfFirmasPoderes.Domain.Documents;

public static class UploadRules
{
    public const long DefaultMaxBytes = 50L * 1024 * 1024;

    public static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp",
        "image/tiff",
        "image/tif"
    };

    public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".tif", ".tiff"
    };

    public static bool IsAllowed(string? contentType, string? fileName)
    {
        var mime = contentType?.Split(';')[0].Trim();
        if (!string.IsNullOrWhiteSpace(mime) && AllowedContentTypes.Contains(mime))
            return true;

        var ext = Path.GetExtension(fileName ?? string.Empty);
        return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
    }

    public static string SanitizeFileName(string? fileName)
    {
        var name = Path.GetFileName(fileName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return "upload.bin";

        return name.Length <= 512 ? name : name[..512];
    }
}
