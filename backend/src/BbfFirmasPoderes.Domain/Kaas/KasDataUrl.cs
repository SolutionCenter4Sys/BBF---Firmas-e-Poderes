namespace BbfFirmasPoderes.Domain.Kaas;

public static class KasDataUrl
{
    public static string FromBytes(byte[] bytes, string? contentType)
    {
        var mime = string.IsNullOrWhiteSpace(contentType)
            ? "application/octet-stream"
            : contentType.Split(';')[0].Trim();

        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }
}
