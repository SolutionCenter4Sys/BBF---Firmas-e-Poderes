using BbfFirmasPoderes.Domain.Audit;
using BbfFirmasPoderes.Domain.Correlation;
using BbfFirmasPoderes.Domain.Entities;
using BbfFirmasPoderes.Domain.Pii;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BbfFirmasPoderes.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Trilha append-only: recusa update/delete em <see cref="AuditEvent"/> e
/// grava um evento por SaveChanges que persistiu comando (insert/update/delete de outras entidades).
/// </summary>
public sealed class AppendOnlyAuditInterceptor(IAuditContext auditContext) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
            return;

        foreach (var entry in context.ChangeTracker.Entries<AuditEvent>())
        {
            if (entry.State is EntityState.Modified or EntityState.Deleted)
                throw new InvalidOperationException("audit_events é append-only. Update/delete recusado.");
        }

        var alreadyTyped = context.ChangeTracker.Entries<AuditEvent>()
            .Any(e => e.State == EntityState.Added);
        if (alreadyTyped)
            return;

        var commands = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditEvent
                        && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (commands.Count == 0)
            return;

        var documentId = commands.Select(e => e.Entity).OfType<Document>().Select(d => d.DocumentId).FirstOrDefault();
        var decisionId = commands.Select(e => e.Entity).OfType<Decision>().Select(d => d.DecisionId).FirstOrDefault();

        var summary = string.Join("; ", commands.Select(e => $"{e.State} {e.Metadata.ClrType.Name}"));

        context.Set<AuditEvent>().Add(new AuditEvent
        {
            EventId = $"evt_{Guid.NewGuid():N}",
            CorrelationId = string.IsNullOrWhiteSpace(auditContext.CorrelationId)
                ? CorrelationIds.New()
                : auditContext.CorrelationId,
            DocumentId = documentId,
            DecisionId = decisionId,
            Type = AuditEventTypes.CommandPersisted,
            Actor = string.IsNullOrWhiteSpace(auditContext.Actor) ? "system" : auditContext.Actor,
            OccurredAt = DateTimeOffset.UtcNow,
            Details = PiiMask.InText(summary)
        });
    }
}
