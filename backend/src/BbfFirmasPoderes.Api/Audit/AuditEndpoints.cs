using BbfFirmasPoderes.Domain.Auth;
using BbfFirmasPoderes.Domain.Correlation;
using BbfFirmasPoderes.Domain.Entities;
using BbfFirmasPoderes.Domain.Pii;
using BbfFirmasPoderes.Infrastructure.Audit;
using Microsoft.AspNetCore.Mvc;

namespace BbfFirmasPoderes.Api.Audit;

public static class AuditEndpoints
{
    public static WebApplication MapAudit(this WebApplication app)
    {
        app.MapGet("/v1/audit/trail", GetTrail)
            .RequireAuthorization(Policies.AuditRead)
            .WithTags("Auditoria")
            .WithSummary("Trilha imutável de auditoria")
            .WithDescription("Filtros opcionais documentId, correlationId, from, to. Restrito ao perfil auditor. Sem tela /audit.");

        return app;
    }

    private static async Task<IResult> GetTrail(
        HttpContext http,
        AuditTrailService trail,
        string? documentId,
        string? correlationId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        if (from is not null && to is not null && from > to)
        {
            return Problem(
                http,
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                "from não pode ser posterior a to.");
        }

        var events = await trail.QueryAsync(documentId, correlationId, from, to, cancellationToken);
        var items = events.Select(ToDto).ToArray();
        return Results.Json(items);
    }

    internal static AuditEventDto ToDto(AuditEvent e)
        => new(
            e.EventId,
            e.CorrelationId,
            e.DocumentId,
            e.DecisionId,
            e.Type,
            e.Actor,
            e.OccurredAt,
            PiiMask.InText(e.Details));

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

public sealed record AuditEventDto(
    string EventId,
    string CorrelationId,
    string? DocumentId,
    string? DecisionId,
    string Type,
    string Actor,
    DateTimeOffset Timestamp,
    string Details);
