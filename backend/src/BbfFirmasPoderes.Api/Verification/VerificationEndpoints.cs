using BbfFirmasPoderes.Domain.Auth;
using BbfFirmasPoderes.Domain.Verification;

namespace BbfFirmasPoderes.Api.Verification;

public static class VerificationEndpoints
{
    public static WebApplication MapVerification(this WebApplication app)
    {
        app.MapGet("/v1/verification/health", GetHealth)
            .RequireAuthorization(Policies.VerificationHealth)
            .WithTags("Validação Oficial")
            .WithSummary("Saúde das fontes oficiais (stub)")
            .WithDescription("Circuit breaker aberto/fechado/meio-aberto. Sem HTTP Junta. 503 nas fontes não derruba este GET.");

        return app;
    }

    private static IResult GetHealth(IOfficialSourceHealthStore sources)
    {
        var items = sources.Snapshot().Select(s => new SourceHealthDto(
            s.SourceId,
            s.Nome,
            s.Status,
            s.Uptime24h,
            s.LatenciaP95Ms,
            s.ErrorRate,
            s.CacheHitRate,
            s.UltimaConsulta,
            s.CircuitBreaker,
            s.Observacao)).ToArray();

        return Results.Json(items);
    }
}

public sealed record SourceHealthDto(
    string SourceId,
    string Nome,
    string Status,
    decimal Uptime24h,
    int LatenciaP95Ms,
    decimal ErrorRate,
    decimal CacheHitRate,
    DateTimeOffset UltimaConsulta,
    string CircuitBreaker,
    string? Observacao);
