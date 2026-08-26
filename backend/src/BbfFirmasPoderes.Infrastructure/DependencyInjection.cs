using BbfFirmasPoderes.Domain.Audit;
using BbfFirmasPoderes.Domain.Documents;
using BbfFirmasPoderes.Infrastructure.Audit;
using BbfFirmasPoderes.Infrastructure.Documents;
using BbfFirmasPoderes.Infrastructure.Persistence;
using BbfFirmasPoderes.Infrastructure.Persistence.Interceptors;
using BbfFirmasPoderes.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BbfFirmasPoderes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Connection string 'Postgres' não configurada. Defina ConnectionStrings__Postgres.");

        services.Configure<DocumentsOptions>(configuration.GetSection(DocumentsOptions.SectionName));
        services.TryAddScoped<IAuditContext, SystemAuditContext>();
        services.AddScoped<AppendOnlyAuditInterceptor>();
        services.AddSingleton<IDocumentBlobStore, FileDocumentBlobStore>();
        services.AddScoped<DocumentIngestService>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(sp.GetRequiredService<AppendOnlyAuditInterceptor>());
        });

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("postgres");

        return services;
    }
}
