using BbfFirmasPoderes.Api.Decisions;
using BbfFirmasPoderes.Domain.Auth;
using BbfFirmasPoderes.Domain.Correlation;
using BbfFirmasPoderes.Infrastructure.Decisioning;
using BbfFirmasPoderes.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace BbfFirmasPoderes.Api.Authority;

public static class AuthorityEndpoints
{
    public static WebApplication MapAuthority(this WebApplication app)
    {
        app.MapGet("/v1/authority/decision", Decide)
            .RequireAuthorization(Policies.ApiDecisionRead)
            .RequireRateLimiting(RateLimitingOptions.PolicyName)
            .WithTags("Decisão")
            .WithSummary("Avaliar autoridade de assinatura (consumidor)")
            .WithDescription("Query cnpj, operation, signers. Headers X-Correlation-Id e Idempotency-Key (24h). Escopo OAuth consumer api:decision:read. Sem HTTP Junta.");

        return app;
    }

    private static async Task<IResult> Decide(
        HttpContext http,
        DecisionService decisions,
        string? cnpj,
        string? operation,
        string? signers,
        decimal? valor,
        CancellationToken cancellationToken)
    {
        http.Request.Headers.TryGetValue(IdempotencyKeys.HeaderName, out var idempotencyValues);
        var idempotencyKey = idempotencyValues.FirstOrDefault();
        var correlationId = CorrelationContext.Current
            ?? http.Items[CorrelationIds.HeaderName]?.ToString()
            ?? CorrelationIds.New();
        var actor = http.User.Identity?.Name ?? "anonymous";

        var outcome = await decisions.EvaluateAuthorityAsync(
            new AuthorityCommand(cnpj, operation, signers, valor, idempotencyKey, actor),
            actor,
            correlationId,
            cancellationToken);

        return outcome switch
        {
            EvaluateOutcome.Ok ok => Results.Json(DecisionEndpoints.ToRecord(ok.Decision, ok.Evidencias)),
            EvaluateOutcome.BadRequest bad => Problem(
                http,
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                bad.Detail),
            EvaluateOutcome.NotFound => Problem(
                http,
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                "cnpj sem documento canônico."),
            EvaluateOutcome.Unprocessable unprocessable => Problem(
                http,
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                unprocessable.Detail),
            _ => Problem(
                http,
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                "Parâmetros inválidos.")
        };
    }

    private static IResult Problem(HttpContext http, int status, string title, string type, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = http.Request.Path
        };
        problem.Extensions["correlationId"] = CorrelationContext.Current
            ?? http.Items[CorrelationIds.HeaderName]?.ToString();

        return Results.Json(problem, statusCode: status, contentType: "application/problem+json");
    }
}
