using BbfFirmasPoderes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BbfFirmasPoderes.Infrastructure.Persistence.Configurations;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_events");

        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventId).HasColumnName("event_id").HasMaxLength(64);
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(64).IsRequired();
        builder.Property(e => e.DocumentId).HasColumnName("document_id").HasMaxLength(64);
        builder.Property(e => e.DecisionId).HasColumnName("decision_id").HasMaxLength(64);
        builder.Property(e => e.Type).HasColumnName("type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Actor).HasColumnName("actor").HasMaxLength(256).IsRequired();
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.Details).HasColumnName("details").HasMaxLength(2048).IsRequired();

        builder.HasIndex(e => e.CorrelationId).HasDatabaseName("ix_audit_events_correlation_id");
        builder.HasIndex(e => e.DocumentId).HasDatabaseName("ix_audit_events_document_id");
        builder.HasIndex(e => e.OccurredAt).HasDatabaseName("ix_audit_events_occurred_at");
    }
}
