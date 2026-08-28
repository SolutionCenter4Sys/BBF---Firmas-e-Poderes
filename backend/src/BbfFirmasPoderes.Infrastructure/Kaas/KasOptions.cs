using BbfFirmasPoderes.Domain.Kaas;

namespace BbfFirmasPoderes.Infrastructure.Kaas;

public sealed class KasOptions
{
    public const string SectionName = "Kas";

    public string RunUrl { get; set; } = KasDefaults.DefaultRunUrl;

    /// <summary>Só no Worker (<c>Kas__ApiKey</c>). Nunca no Next.js.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = KasDefaults.TimeoutSeconds;

    public int PollIntervalSeconds { get; set; } = 5;
}
