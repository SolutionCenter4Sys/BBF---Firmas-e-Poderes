namespace BbfFirmasPoderes.Domain.Verification;

public interface IOfficialSourceHealthStore
{
    IReadOnlyList<OfficialSourceHealth> Snapshot();
    OfficialSourceHealth? Get(string sourceId);
    bool IsOpen(string sourceId);
    void SetCircuitBreaker(string sourceId, string state, string? observacao = null);
}
