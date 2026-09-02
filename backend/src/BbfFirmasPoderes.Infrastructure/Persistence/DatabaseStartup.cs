using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BbfFirmasPoderes.Infrastructure.Persistence;

public static class DatabaseStartup
{
    public static void MigrateIfEnabled(IServiceProvider services, IConfiguration configuration)
    {
        if (!configuration.GetValue("Database:MigrateOnStartup", false))
            return;

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!db.Database.IsNpgsql())
            return;

        var pending = db.Database.GetPendingMigrations().ToArray();
        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()
            ?.CreateLogger("BbfFirmasPoderes.Infrastructure.Persistence.DatabaseStartup");
        if (pending.Length > 0)
            logger?.LogInformation("Aplicando {Count} migration(s): {Migrations}", pending.Length, string.Join(", ", pending));

        db.Database.Migrate();
    }
}
