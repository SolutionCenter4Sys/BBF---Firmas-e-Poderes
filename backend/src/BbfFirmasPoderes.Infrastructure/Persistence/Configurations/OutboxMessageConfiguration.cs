using BbfFirmasPoderes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BbfFirmasPoderes.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(e => e.OutboxId);
        builder.Property(e => e.OutboxId).HasColumnName("outbox_id");
        builder.Property(e => e.Type).HasColumnName("type").HasMaxLength(64).IsRequired();
        builder.Property(e => e.PayloadJson).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.ProcessedAt).HasColumnName("processed_at").HasColumnType("timestamptz");
        builder.Property(e => e.AttemptCount).HasColumnName("attempt_count").IsRequired();
        builder.Property(e => e.NextAttemptAt).HasColumnName("next_attempt_at").HasColumnType("timestamptz");
        builder.Property(e => e.LastError).HasColumnName("last_error").HasMaxLength(1024);
        builder.Property(e => e.DocumentId).HasColumnName("document_id").HasMaxLength(64);
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(64).IsRequired();

        builder.HasIndex(e => e.ProcessedAt)
            .HasDatabaseName("ix_outbox_processed_at")
            .HasFilter("processed_at IS NULL");
        builder.HasIndex(e => e.DocumentId).HasDatabaseName("ix_outbox_document_id");
        builder.HasIndex(e => e.Type).HasDatabaseName("ix_outbox_type");

        builder.HasOne(e => e.Document)
            .WithMany()
            .HasForeignKey(e => e.DocumentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
