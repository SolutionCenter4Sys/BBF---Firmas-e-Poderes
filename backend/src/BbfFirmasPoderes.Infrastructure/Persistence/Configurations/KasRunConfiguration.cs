using BbfFirmasPoderes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BbfFirmasPoderes.Infrastructure.Persistence.Configurations;

internal sealed class KasRunConfiguration : IEntityTypeConfiguration<KasRun>
{
    public void Configure(EntityTypeBuilder<KasRun> builder)
    {
        builder.ToTable("kas_runs", t =>
        {
            t.HasCheckConstraint("ck_kas_runs_action", "action IN ('ingest','result')");
        });

        builder.HasKey(e => e.KasRunId);
        builder.Property(e => e.KasRunId).HasColumnName("kas_run_id");
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(64).IsRequired();
        builder.Property(e => e.DocumentId).HasColumnName("document_id").HasMaxLength(64);
        builder.Property(e => e.ExecutionId).HasColumnName("execution_id").HasMaxLength(128);
        builder.Property(e => e.Action).HasColumnName("action").HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(e => e.FileName).HasColumnName("file_name").HasMaxLength(512);
        builder.Property(e => e.HttpStatus).HasColumnName("http_status");
        builder.Property(e => e.Ok).HasColumnName("ok");
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.DurationMs).HasColumnName("duration_ms");
        builder.Property(e => e.PayloadJson)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(e => e.CorrelationId).HasDatabaseName("ix_kas_runs_correlation_id");
        builder.HasIndex(e => e.DocumentId).HasDatabaseName("ix_kas_runs_document_id");
        builder.HasIndex(e => e.ExecutionId).HasDatabaseName("ix_kas_runs_execution_id");
    }
}
