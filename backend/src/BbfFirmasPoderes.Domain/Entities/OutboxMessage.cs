namespace BbfFirmasPoderes.Domain.Entities;

/// <summary>
/// Outbox transacional. Worker (WF-07) consome <c>ProcessedAt == null</c>. API não processa OCR.
/// </summary>
public class OutboxMessage
{
    public Guid OutboxId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public string? LastError { get; set; }
    public string? DocumentId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;

    public Document? Document { get; set; }
}
