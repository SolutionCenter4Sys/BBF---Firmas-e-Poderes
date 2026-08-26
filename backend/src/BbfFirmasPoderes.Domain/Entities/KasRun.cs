using BbfFirmasPoderes.Domain.Enums;

namespace BbfFirmasPoderes.Domain.Entities;

/// <summary>
/// Execução KAAS. <see cref="PayloadJson"/> guarda o JSON bruto de retorno (coluna jsonb).
/// </summary>
public class KasRun
{
    public Guid KasRunId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? DocumentId { get; set; }
    public string? ExecutionId { get; set; }
    public KasRunAction Action { get; set; }
    public string? FileName { get; set; }
    public int HttpStatus { get; set; }
    public bool Ok { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public int? DurationMs { get; set; }
    public string PayloadJson { get; set; } = "{}";

    public Document? Document { get; set; }
}
