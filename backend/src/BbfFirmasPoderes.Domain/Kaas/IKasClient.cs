namespace BbfFirmasPoderes.Domain.Kaas;

/// <summary>
/// Porta HTTP da jornada KAAS. Implementação e <c>X-Flow-Api-Key</c> ficam só no Worker.
/// </summary>
public interface IKasClient
{
    Task<KasCallResult> PostDocumentAsync(
        Stream document,
        string fileName,
        string contentType,
        string fileField,
        CancellationToken cancellationToken = default);
}
