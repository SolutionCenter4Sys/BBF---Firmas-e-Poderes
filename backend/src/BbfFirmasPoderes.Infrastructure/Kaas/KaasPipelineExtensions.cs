using BbfFirmasPoderes.Domain.Kaas;
using BbfFirmasPoderes.Infrastructure.Kaas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BbfFirmasPoderes.Infrastructure;

public static class KaasPipelineExtensions
{
    /// <summary>
    /// Adapter KAAS + processor de outbox. Chamar só no Worker (chave <c>Kas__ApiKey</c>).
    /// A API pública não registra <see cref="IKasClient"/>.
    /// </summary>
    public static IServiceCollection AddKaasPipeline(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KasOptions>(configuration.GetSection(KasOptions.SectionName));
        services
            .AddHttpClient<IKasClient, HttpKasClient>((sp, client) =>
            {
                var timeout = sp.GetRequiredService<IOptions<KasOptions>>().Value.TimeoutSeconds;
                if (timeout <= 0)
                    timeout = KasDefaults.TimeoutSeconds;
                client.Timeout = TimeSpan.FromSeconds(timeout);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                ConnectTimeout = TimeSpan.FromSeconds(30),
                KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always,
                PooledConnectionLifetime = TimeSpan.FromMinutes(15)
            });
        services.AddScoped<OutboxKaasProcessor>();
        return services;
    }
}
