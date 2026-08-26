using BbfFirmasPoderes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BbfFirmasPoderes.Infrastructure.Persistence.Configurations;

internal sealed class PowerConfiguration : IEntityTypeConfiguration<Power>
{
    public void Configure(EntityTypeBuilder<Power> builder)
    {
        builder.ToTable("powers", t =>
        {
            t.HasCheckConstraint("ck_powers_modo_assinatura", "modo_assinatura_tipo IN ('isolada','conjunta')");
        });

        builder.HasKey(e => e.PowerId);
        builder.Property(e => e.PowerId).HasColumnName("power_id").HasMaxLength(64);
        builder.Property(e => e.DocumentId).HasColumnName("document_id").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Pessoa).HasColumnName("pessoa").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Operacao).HasColumnName("operacao").HasMaxLength(256).IsRequired();
        builder.Property(e => e.LimiteCurrency).HasColumnName("limite_currency").HasMaxLength(3).IsRequired();
        builder.Property(e => e.LimiteValue).HasColumnName("limite_value").HasColumnType("numeric(18,2)");
        builder.Property(e => e.LimiteExpression).HasColumnName("limite_expression").HasMaxLength(256).IsRequired();
        builder.Property(e => e.ModoAssinaturaTipo).HasColumnName("modo_assinatura_tipo").HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(e => e.ModoAssinaturaN).HasColumnName("modo_assinatura_n");
        builder.Property(e => e.ModoAssinaturaM).HasColumnName("modo_assinatura_m");
        builder.Property(e => e.ModoAssinaturaQualificacoes)
            .HasColumnName("modo_assinatura_qualificacoes")
            .HasColumnType("text[]");
        builder.Property(e => e.VigenciaFrom).HasColumnName("vigencia_from").HasColumnType("date");
        builder.Property(e => e.VigenciaTo).HasColumnName("vigencia_to").HasColumnType("date");
        builder.Property(e => e.SourcePage).HasColumnName("source_page");
        builder.Property(e => e.SourceOffsetStart).HasColumnName("source_offset_start");
        builder.Property(e => e.SourceOffsetEnd).HasColumnName("source_offset_end");
        builder.Property(e => e.SourceSnippet).HasColumnName("source_snippet").HasMaxLength(1024).IsRequired();

        builder.HasIndex(e => e.DocumentId).HasDatabaseName("ix_powers_document_id");
    }
}
