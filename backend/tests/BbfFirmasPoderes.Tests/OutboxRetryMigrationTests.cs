using System.Reflection;
using BbfFirmasPoderes.Infrastructure.Persistence;
using BbfFirmasPoderes.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BbfFirmasPoderes.Tests;

public class OutboxRetryMigrationTests
{
    [Fact]
    public void AddOutboxRetry_IsDiscoveredByEf()
    {
        var type = typeof(AddOutboxRetry);
        var db = type.GetCustomAttribute<DbContextAttribute>();
        var migration = type.GetCustomAttribute<MigrationAttribute>();

        Assert.NotNull(db);
        Assert.Equal(typeof(AppDbContext), db!.ContextType);
        Assert.Equal("20260902193000_AddOutboxRetry", migration?.Id);
    }
}
