using System.Text.Json.Serialization;
using BbfFirmasPoderes.Domain.Kaas;

namespace BbfFirmasPoderes.Infrastructure.Kaas;

/// <summary>
/// Contrato live (WF-21): POST journey/run com <c>mode=sync</c> e
/// <c>payload.document_url</c>. <c>document_url</c> na raiz → HTTP 400
/// <c>property document_url should not exist</c>.
/// </summary>
public sealed class KasSyncEnvelope
{
    [JsonPropertyName("mode")]
    public string Mode { get; init; } = KasDefaults.ModeSync;

    [JsonPropertyName("payload")]
    public required KasSyncPayload Payload { get; init; }
}

public sealed class KasSyncPayload
{
    [JsonPropertyName("document_url")]
    public required string DocumentUrl { get; init; }
}
