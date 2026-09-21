namespace BbfFirmasPoderes.Domain.Correlation;

public static class IdempotencyKeys
{
    public const string HeaderName = "Idempotency-Key";
    public static readonly TimeSpan Window = TimeSpan.FromHours(24);
}
