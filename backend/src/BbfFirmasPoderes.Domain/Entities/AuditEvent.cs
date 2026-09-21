namespace BbfFirmasPoderes.Domain.Entities;

public class AuditEvent
{
    public string EventId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string? DocumentId { get; set; }
    public string? DecisionId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public string Details { get; set; } = string.Empty;

    public Document? Document { get; set; }
    public Decision? Decision { get; set; }
}
