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

    /// <summary>
    /// MIME e extensão e assinatura (magic bytes). Os três têm de bater.
    /// </summary>
    public static bool IsAllowed(string? contentType, string? fileName, ReadOnlySpan<byte> header)
    {
        var mime = contentType?.Split(';')[0].Trim();
        if (string.IsNullOrWhiteSpace(mime) || !AllowedContentTypes.Contains(mime))
            return false;

        var ext = Path.GetExtension(fileName ?? string.Empty);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            return false;

        return MatchesMagic(header, ext);
    }

    public static bool MatchesMagic(ReadOnlySpan<byte> header, string extension)
    {
        var ext = extension.ToLowerInvariant();
        return ext switch
        {
            ".pdf" => header.StartsWith("%PDF"u8),
            ".png" => header.Length >= 8
                && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A,
            ".jpg" or ".jpeg" => header.Length >= 3
                && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".webp" => header.Length >= 12
                && header.StartsWith("RIFF"u8)
                && header[8] == (byte)'W' && header[9] == (byte)'E'
                && header[10] == (byte)'B' && header[11] == (byte)'P',
            ".tif" or ".tiff" => header.Length >= 4
                && ((header[0] == (byte)'I' && header[1] == (byte)'I' && header[2] == 0x2A && header[3] == 0x00)
                    || (header[0] == (byte)'M' && header[1] == (byte)'M' && header[2] == 0x00 && header[3] == 0x2A)),
            _ => false
        };
    }

    public static string SanitizeFileName(string? fileName)
    {
        var name = Path.GetFileName(fileName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return "upload.bin";

        return name.Length <= 512 ? name : name[..512];
    }
}
