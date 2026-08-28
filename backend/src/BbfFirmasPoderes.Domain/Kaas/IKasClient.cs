namespace BbfFirmasPoderes.Domain.Kaas;

/// <summary>
/// Porta HTTP da jornada KAAS. Implementação e <c>X-Flow-Api-Key</c> ficam só no Worker.
/// </summary>
public interface IKasClient
{
    Task<KasCallResult> PostAsync(object envelope, CancellationToken cancellationToken = default);
}
