using BbfFirmasPoderes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BbfFirmasPoderes.Infrastructure.Persistence.Configurations;

internal sealed class DecisionConfiguration : IEntityTypeConfiguration<Decision>
{
    public void Configure(EntityTypeBuilder<Decision> builder)
    {
        builder.ToTable("decisions", t =>
        {
            t.HasCheckConstraint("ck_decisions_status", "status IN ('APROVADO','REPROVADO','MANUAL')");
        });

        builder.HasKey(e => e.DecisionId);
        builder.Property(e => e.DecisionId).HasColumnName("decision_id").HasMaxLength(64);
        builder.Property(e => e.DocumentId).HasColumnName("document_id").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Cnpj).HasColumnName("cnpj").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Operacao).HasColumnName("operacao").HasMaxLength(256).IsRequired();
        builder.Property(e => e.SignatariosSolicitados)
            .HasColumnName("signatarios_solicitados")
            .HasColumnType("text[]");
        builder.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(e => e.Motivos)
            .HasColumnName("motivos")
            .HasColumnType("text[]");
        builder.Property(e => e.EvidenciasJson)
            .HasColumnName("evidencias")
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(e => e.VersionRules).HasColumnName("version_rules").HasMaxLength(32).IsRequired();
        builder.Property(e => e.VersionCanonical).HasColumnName("version_canonical").HasMaxLength(32).IsRequired();
        builder.Property(e => e.VersionAiPrompt).HasColumnName("version_ai_prompt").HasMaxLength(128).IsRequired();
        builder.Property(e => e.VersionAiModel).HasColumnName("version_ai_model").HasMaxLength(64).IsRequired();
        builder.Property(e => e.EvaluatedAt).HasColumnName("evaluated_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.LatencyMs).HasColumnName("latency_ms");

        builder.HasIndex(e => e.DocumentId).HasDatabaseName("ix_decisions_document_id");
        builder.HasIndex(e => e.Cnpj).HasDatabaseName("ix_decisions_cnpj");

        builder.HasMany(e => e.AuditEvents)
            .WithOne(e => e.Decision)
            .HasForeignKey(e => e.DecisionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
