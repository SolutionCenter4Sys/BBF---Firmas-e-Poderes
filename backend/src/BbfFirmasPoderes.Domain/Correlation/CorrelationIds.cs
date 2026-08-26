namespace BbfFirmasPoderes.Domain.Correlation;

public static class CorrelationIds
{
    public const string HeaderName = "X-Correlation-Id";

    public static string New() => $"corr_{Guid.NewGuid():D}";

    public static string FromHeaderOrNew(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return New();

        return header.Trim();
    }
}
