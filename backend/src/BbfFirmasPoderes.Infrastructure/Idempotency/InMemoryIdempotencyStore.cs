using System.Collections.Concurrent;
using BbfFirmasPoderes.Domain.Idempotency;
using BbfFirmasPoderes.Domain.Correlation;

namespace BbfFirmasPoderes.Infrastructure.Idempotency;

public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public IdempotencyLookup Lookup(string consumerId, string key, string fingerprint)
    {
        var mapKey = MapKey(consumerId, key);
        if (!_entries.TryGetValue(mapKey, out var entry))
            return new IdempotencyLookup.Miss();

        if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _entries.TryRemove(mapKey, out _);
            return new IdempotencyLookup.Miss();
        }

        if (!string.Equals(entry.Fingerprint, fingerprint, StringComparison.Ordinal))
            return new IdempotencyLookup.Conflict();

        return new IdempotencyLookup.Hit(entry.DecisionId);
    }

    public void Remember(string consumerId, string key, string fingerprint, string decisionId)
    {
        var mapKey = MapKey(consumerId, key);
        _entries[mapKey] = new Entry(fingerprint, decisionId, DateTimeOffset.UtcNow.Add(IdempotencyKeys.Window));
    }

    private static string MapKey(string consumerId, string key)
        => $"{consumerId}\n{key.Trim()}";

    private sealed record Entry(string Fingerprint, string DecisionId, DateTimeOffset ExpiresAt);
}
