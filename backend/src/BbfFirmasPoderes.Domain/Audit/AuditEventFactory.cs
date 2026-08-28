using BbfFirmasPoderes.Domain.Correlation;
using BbfFirmasPoderes.Domain.Entities;
using BbfFirmasPoderes.Domain.Pii;

namespace BbfFirmasPoderes.Domain.Audit;

public static class AuditEventFactory
{
    public static AuditEvent Create(
        string type,
        string correlationId,
        string actor,
        string details,
        string? documentId = null,
        string? decisionId = null,
        DateTimeOffset? occurredAt = null)
    {
        return new AuditEvent
        {
            EventId = $"ev_{Guid.NewGuid():N}",
            CorrelationId = string.IsNullOrWhiteSpace(correlationId)
                ? CorrelationIds.New()
                : correlationId,
            DocumentId = documentId,
            DecisionId = decisionId,
            Type = type,
            Actor = string.IsNullOrWhiteSpace(actor) ? "system" : actor,
            OccurredAt = occurredAt ?? DateTimeOffset.UtcNow,
            Details = PiiMask.InText(details)
        };
    }
}
