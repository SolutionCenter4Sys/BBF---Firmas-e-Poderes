using BbfFirmasPoderes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BbfFirmasPoderes.Infrastructure.Persistence.Configurations;

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents", t =>
        {
            t.HasCheckConstraint(
                "ck_documents_status",
                "status IN ('pendente','processando_ocr','processando_iagen','processando_ner','canonico_pronto','validacao_oficial','decidido','revisao_humana','falha')");
        });

        builder.HasKey(e => e.DocumentId);
        builder.Property(e => e.DocumentId).HasColumnName("document_id").HasMaxLength(64);
        builder.Property(e => e.FileName).HasColumnName("file_name").HasMaxLength(512).IsRequired();
        builder.Property(e => e.Cnpj).HasColumnName("cnpj").HasMaxLength(32).IsRequired();
        builder.Property(e => e.RazaoSocial).HasColumnName("razao_social").HasMaxLength(256).IsRequired();
        builder.Property(e => e.TipoSocietario).HasColumnName("tipo_societario").HasMaxLength(16).IsRequired();
        builder.Property(e => e.UploadedAt).HasColumnName("uploaded_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.UploadedBy).HasColumnName("uploaded_by").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.FileHash).HasColumnName("file_hash").HasMaxLength(128).IsRequired();
        builder.Property(e => e.ContentType).HasColumnName("content_type").HasMaxLength(128);
        builder.Property(e => e.StoragePath).HasColumnName("storage_path").HasMaxLength(1024);
        builder.Property(e => e.Paginas).HasColumnName("paginas");
        builder.Property(e => e.ConfiancaOcr).HasColumnName("confianca_ocr").HasColumnType("numeric(5,4)");
        builder.Property(e => e.ConfiancaIagen).HasColumnName("confianca_iagen").HasColumnType("numeric(5,4)");
        builder.Property(e => e.ConfiancaNer).HasColumnName("confianca_ner").HasColumnType("numeric(5,4)");
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(64);
        builder.Property(e => e.AnalysisJson).HasColumnName("analysis_json").HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.CreditReadinessScore).HasColumnName("credit_readiness_score");
        builder.Property(e => e.CreditReadinessClassification)
            .HasColumnName("credit_readiness_classification").HasMaxLength(16);
        builder.Property(e => e.CreditReadinessRecommendation)
            .HasColumnName("credit_readiness_recommendation").HasMaxLength(32);
        builder.Property(e => e.CreditReadinessJustification)
            .HasColumnName("credit_readiness_justification").HasMaxLength(2048);

        builder.HasIndex(e => e.Cnpj).HasDatabaseName("ix_documents_cnpj");
        builder.HasIndex(e => e.Status).HasDatabaseName("ix_documents_status");
        builder.HasIndex(e => e.CorrelationId).HasDatabaseName("ix_documents_correlation_id");
        builder.HasIndex(e => e.FileHash).HasDatabaseName("ix_documents_file_hash");

        builder.HasMany(e => e.Socios).WithOne(e => e.Document).HasForeignKey(e => e.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Poderes).WithOne(e => e.Document).HasForeignKey(e => e.DocumentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(e => e.Decisions).WithOne(e => e.Document).HasForeignKey(e => e.DocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(e => e.AuditEvents).WithOne(e => e.Document).HasForeignKey(e => e.DocumentId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(e => e.KasRuns).WithOne(e => e.Document).HasForeignKey(e => e.DocumentId).OnDelete(DeleteBehavior.SetNull);
    }
}
