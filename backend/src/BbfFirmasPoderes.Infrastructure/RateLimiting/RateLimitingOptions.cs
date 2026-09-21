namespace BbfFirmasPoderes.Infrastructure.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";
    public const string PolicyName = "authority-consumer";

    public int PermitLimit { get; set; } = 200;
    public int WindowSeconds { get; set; } = 60;
}
