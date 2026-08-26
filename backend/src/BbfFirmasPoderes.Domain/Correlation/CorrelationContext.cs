namespace BbfFirmasPoderes.Domain.Correlation;

public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> CurrentValue = new();

    public static string? Current => CurrentValue.Value;

    public static void Set(string correlationId) => CurrentValue.Value = correlationId;
}
