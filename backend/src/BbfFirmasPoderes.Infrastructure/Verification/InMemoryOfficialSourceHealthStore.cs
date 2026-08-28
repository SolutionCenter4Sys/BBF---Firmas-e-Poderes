using System.Collections.Concurrent;
using BbfFirmasPoderes.Domain.Verification;

namespace BbfFirmasPoderes.Infrastructure.Verification;

/// <summary>
/// Stub in-memory. Circuit breaker aberto/fechado/meio-aberto sem HTTP Junta.
/// </summary>
public sealed class InMemoryOfficialSourceHealthStore : IOfficialSourceHealthStore
{
    private readonly ConcurrentDictionary<string, OfficialSourceHealth> _sources = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryOfficialSourceHealthStore()
    {
        foreach (var source in OfficialSourcesCatalog.Defaults)
            _sources[source.SourceId] = source.Clone();
    }

    public IReadOnlyList<OfficialSourceHealth> Snapshot()
        => OfficialSourcesCatalog.Defaults
            .Select(d => _sources.TryGetValue(d.SourceId, out var current) ? current.Clone() : d.Clone())
            .ToArray();

    public OfficialSourceHealth? Get(string sourceId)
        => _sources.TryGetValue(sourceId, out var source) ? source.Clone() : null;

    public bool IsOpen(string sourceId)
        => CircuitBreakerStates.IsOpen(Get(sourceId)?.CircuitBreaker);

    public void SetCircuitBreaker(string sourceId, string state, string? observacao = null)
    {
        _sources.AddOrUpdate(
            sourceId,
            _ => throw new InvalidOperationException($"Fonte {sourceId} não existe no stub."),
            (_, current) =>
            {
                var next = current.Clone();
                next.CircuitBreaker = state;
                if (CircuitBreakerStates.IsOpen(state))
                    next.Status = SourceHealthStatuses.Indisponivel;
                else if (string.Equals(state, CircuitBreakerStates.MeioAberto, StringComparison.OrdinalIgnoreCase))
                    next.Status = SourceHealthStatuses.Degradado;
                else
                    next.Status = SourceHealthStatuses.Operacional;
                if (observacao is not null)
                    next.Observacao = observacao;
                return next;
            });
    }
}
