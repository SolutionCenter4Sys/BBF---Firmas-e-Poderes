using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BbfFirmasPoderes.Infrastructure.Persistence;

public static class DatabaseStartup
{
    public static void MigrateIfEnabled(IServiceProvider services, IConfiguration configuration)
    {
        if (!configuration.GetValue("Database:MigrateOnStartup", false))
            return;

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.IsNpgsql())
            db.Database.Migrate();
    }
}
