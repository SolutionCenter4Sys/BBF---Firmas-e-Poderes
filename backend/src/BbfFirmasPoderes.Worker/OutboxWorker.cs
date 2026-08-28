using BbfFirmasPoderes.Infrastructure.Kaas;
using Microsoft.Extensions.Options;

namespace BbfFirmasPoderes.Worker;

public sealed class OutboxWorker(
    IServiceScopeFactory scopes,
    IOptions<KasOptions> options,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = options.Value.PollIntervalSeconds;
        if (intervalSeconds <= 0)
            intervalSeconds = 5;

        var interval = TimeSpan.FromSeconds(intervalSeconds);
        logger.LogInformation(
            "Worker KAAS iniciado. Poll {Interval}s. Timeout HTTP {Timeout}s. Jornada testes-firmas-e-poderes.",
            intervalSeconds,
            options.Value.TimeoutSeconds > 0 ? options.Value.TimeoutSeconds : 300);

        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            logger.LogWarning(
                "Kas__ApiKey vazio. Defina a chave só neste Worker. Rotas Next /api/kas estão deprecadas.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<OutboxKaasProcessor>();
                var processed = await processor.ProcessNextAsync(stoppingToken);
                if (processed)
                    continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ciclo do outbox KAAS falhou");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
