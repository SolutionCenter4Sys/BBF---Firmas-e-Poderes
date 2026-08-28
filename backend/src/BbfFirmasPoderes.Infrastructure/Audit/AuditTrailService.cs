using BbfFirmasPoderes.Domain.Entities;
using BbfFirmasPoderes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BbfFirmasPoderes.Infrastructure.Audit;

public sealed class AuditTrailService(AppDbContext db)
{
    public async Task<IReadOnlyList<AuditEvent>> QueryAsync(
        string? documentId,
        string? correlationId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        var query = db.AuditEvents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(documentId))
            query = query.Where(e => e.DocumentId == documentId);

        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Where(e => e.CorrelationId == correlationId);

        if (from is not null)
            query = query.Where(e => e.OccurredAt >= from);

        if (to is not null)
            query = query.Where(e => e.OccurredAt <= to);

        return await query
            .OrderBy(e => e.CorrelationId)
            .ThenBy(e => e.OccurredAt)
            .ThenBy(e => e.EventId)
            .ToListAsync(cancellationToken);
    }
}
