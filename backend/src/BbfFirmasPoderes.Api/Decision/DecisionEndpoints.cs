using BbfFirmasPoderes.Domain.Auth;
using BbfFirmasPoderes.Domain.Correlation;
using BbfFirmasPoderes.Domain.Decisioning;
using BbfFirmasPoderes.Domain.Entities;
using BbfFirmasPoderes.Domain.Enums;
using BbfFirmasPoderes.Infrastructure.Decisioning;
using Microsoft.AspNetCore.Mvc;

namespace BbfFirmasPoderes.Api.Decisions;

public static class DecisionEndpoints
{
    public static WebApplication MapDecisions(this WebApplication app)
    {
        var group = app.MapGroup("/v1/decision").WithTags("Decisão");

        group.MapPost("/evaluate", Evaluate)
            .RequireAuthorization(Policies.DecisionRead)
            .WithSummary("Avaliar decisão (uso interno)")
            .WithDescription("Recebe EvaluationRequest, aplica RN01–RN04 e TH01 no canônico, persiste snapshot de versões. Sem consulta Junta (WF-11).");

        group.MapPost("/{decisionId}/replay", Replay)
            .RequireAuthorization(Policies.DecisionReplay)
            .WithSummary("Replay determinístico de decisão")
            .WithDescription("Devolve o DecisionRecord persistido (mesmo snapshot). Não reconsulta fonte oficial.");

        return app;
    }

    private static async Task<IResult> Evaluate(
        HttpContext http,
        EvaluationRequestDto? body,
        DecisionService decisions,
        CancellationToken cancellationToken)
    {
        if (body is null)
        {
            return Problem(
                http,
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                "Body JSON EvaluationRequest é obrigatório.");
        }

        var correlationId = CorrelationContext.Current
            ?? http.Items[CorrelationIds.HeaderName]?.ToString()
            ?? CorrelationIds.New();
        var actor = http.User.Identity?.Name ?? "anonymous";

        var outcome = await decisions.EvaluateAsync(
            new EvaluateCommand(
                body.DocumentId,
                body.Cnpj,
                body.Operacao,
                body.ValorOperacao,
                body.Currency,
                body.SignatariosSolicitados,
                body.AsOf),
            actor,
            correlationId,
            cancellationToken);

        return outcome switch
        {
            EvaluateOutcome.Ok ok => Results.Json(ToRecord(ok.Decision, ok.Evidencias)),
            EvaluateOutcome.BadRequest bad => Problem(
                http,
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                bad.Detail),
            EvaluateOutcome.NotFound => Problem(
                http,
                StatusCodes.Status404NotFound,
                "Not Found",
                "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                "Documento não encontrado."),
            EvaluateOutcome.Unprocessable unprocessable => Problem(
                http,
                StatusCodes.Status422UnprocessableEntity,
                "Unprocessable Entity",
                "https://tools.ietf.org/html/rfc4918#section-11.2",
                unprocessable.Detail),
            _ => Problem(
                http,
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                "Pedido inválido.")
        };
    }

    private static async Task<IResult> Replay(
        HttpContext http,
        string decisionId,
        DecisionService decisions,
        CancellationToken cancellationToken)
    {
        var decision = await decisions.GetAsync(decisionId, cancellationToken);
        if (decision is null)
        {
            return Problem(
                http,
                StatusCodes.Status404NotFound,
                "Not Found",
                "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                "Decisão não encontrada.");
        }

        return Results.Json(ToRecord(decision, DecisionJson.DeserializeEvidencias(decision.EvidenciasJson)));
    }

    internal static DecisionRecordDto ToRecord(Decision decision, IReadOnlyList<DecisionEvidence> evidencias)
        => new(
            decision.DecisionId,
            decision.DocumentId,
            decision.Cnpj,
            decision.Operacao,
            decision.SignatariosSolicitados,
            decision.Status,
            decision.Motivos,
            evidencias,
            new DecisionVersionsDto(
                decision.VersionRules,
                decision.VersionCanonical,
                decision.VersionAiPrompt,
                decision.VersionAiModel),
            decision.EvaluatedAt,
            decision.LatencyMs);

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

public sealed record EvaluationRequestDto(
    string? DocumentId,
    string? Cnpj,
    string? Operacao,
    decimal? ValorOperacao,
    string? Currency,
    string[]? SignatariosSolicitados,
    DateTimeOffset? AsOf);

public sealed record DecisionVersionsDto(string Rules, string Canonical, string AiPrompt, string AiModel);

public sealed record DecisionRecordDto(
    string DecisionId,
    string DocumentId,
    string Cnpj,
    string Operacao,
    string[] SignatariosSolicitados,
    DecisionStatus Status,
    string[] Motivos,
    IReadOnlyList<DecisionEvidence> Evidencias,
    DecisionVersionsDto Versions,
    DateTimeOffset EvaluatedAt,
    int LatencyMs);
