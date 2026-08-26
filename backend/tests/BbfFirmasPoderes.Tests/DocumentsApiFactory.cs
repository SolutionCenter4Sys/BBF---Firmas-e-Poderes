using BbfFirmasPoderes.Infrastructure.Persistence;
using BbfFirmasPoderes.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BbfFirmasPoderes.Tests;

public sealed class DocumentsApiFactory : WebApplicationFactory<Program>
{
    public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), "bbf-docs-" + Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(StorageRoot);

        builder.UseEnvironment("Development");
        builder.UseSetting("Jwt:Issuer", TestAuth.Issuer);
        builder.UseSetting("Jwt:Audience", TestAuth.Audience);
        builder.UseSetting("Jwt:SigningKey", TestAuth.SigningKey);
        builder.UseSetting("Documents:StorageRoot", StorageRoot);
        builder.UseSetting("Documents:MaxUploadBytes", "1024");
        builder.UseSetting(
            "ConnectionStrings:Postgres",
            "Host=127.0.0.1;Port=5432;Database=bbf_firmas_test;Username=bbf;Password=bbf");

        builder.ConfigureServices(services =>
        {
            foreach (var descriptor in services
                         .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                                     || d.ServiceType == typeof(AppDbContext))
                         .ToList())
            {
                services.Remove(descriptor);
            }

            var dbName = $"docs-{Guid.NewGuid():N}";
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase(dbName);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                options.AddInterceptors(sp.GetRequiredService<AppendOnlyAuditInterceptor>());
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        try
        {
            if (Directory.Exists(StorageRoot))
                Directory.Delete(StorageRoot, recursive: true);
        }
        catch (IOException)
        {
            // temp leftovers are acceptable in CI
        }
    }
}
