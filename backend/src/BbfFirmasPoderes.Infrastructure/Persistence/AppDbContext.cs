using BbfFirmasPoderes.Domain.Entities;
using BbfFirmasPoderes.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace BbfFirmasPoderes.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<Power> Powers => Set<Power>();
    public DbSet<Decision> Decisions => Set<Decision>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<KasRun> KasRuns => Set<KasRun>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        AcmeSeed.Apply(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>().HaveColumnType("timestamptz");
        configurationBuilder.Properties<DateTime>().HaveColumnType("timestamptz");
    }
}
