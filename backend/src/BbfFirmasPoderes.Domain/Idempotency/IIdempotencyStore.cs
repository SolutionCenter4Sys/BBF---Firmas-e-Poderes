namespace BbfFirmasPoderes.Domain.Idempotency;

public abstract record IdempotencyLookup
{
    public sealed record Miss : IdempotencyLookup;
    public sealed record Hit(string DecisionId) : IdempotencyLookup;
    public sealed record Conflict : IdempotencyLookup;
}

public interface IIdempotencyStore
{
    IdempotencyLookup Lookup(string consumerId, string key, string fingerprint);
    void Remember(string consumerId, string key, string fingerprint, string decisionId);
}

public static class IdempotencyFingerprint
{
    public static string For(string cnpjDigits, string operation, IEnumerable<string> signers, decimal valor)
    {
        var people = string.Join(',', signers
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase));
        return $"{cnpjDigits}|{operation.Trim()}|{people}|{valor.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
    }
}
