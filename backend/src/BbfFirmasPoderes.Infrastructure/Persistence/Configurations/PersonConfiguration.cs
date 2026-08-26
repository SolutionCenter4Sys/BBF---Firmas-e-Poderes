using BbfFirmasPoderes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BbfFirmasPoderes.Infrastructure.Persistence.Configurations;

internal sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("people", t =>
        {
            t.HasCheckConstraint("ck_people_status", "status IN ('ativo','inativo')");
        });

        builder.HasKey(e => e.PersonId);
        builder.Property(e => e.PersonId).HasColumnName("person_id").HasMaxLength(64);
        builder.Property(e => e.DocumentId).HasColumnName("document_id").HasMaxLength(64).IsRequired();
        builder.Property(e => e.Nome).HasColumnName("nome").HasMaxLength(256).IsRequired();
        builder.Property(e => e.Cpf).HasColumnName("cpf").HasMaxLength(32).IsRequired();
        builder.Property(e => e.Qualificacao).HasColumnName("qualificacao").HasMaxLength(128).IsRequired();
        builder.Property(e => e.Cargo).HasColumnName("cargo").HasMaxLength(128).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16).IsRequired();

        builder.HasIndex(e => e.DocumentId).HasDatabaseName("ix_people_document_id");
    }
}
