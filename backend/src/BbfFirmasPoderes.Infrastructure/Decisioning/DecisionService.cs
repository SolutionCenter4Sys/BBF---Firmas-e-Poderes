using System.Diagnostics;
using BbfFirmasPoderes.Domain.Audit;
using BbfFirmasPoderes.Domain.Cnpj;
using BbfFirmasPoderes.Domain.Correlation;
using BbfFirmasPoderes.Domain.Decisioning;
using BbfFirmasPoderes.Domain.Entities;
using BbfFirmasPoderes.Domain.Enums;
using BbfFirmasPoderes.Domain.Idempotency;
using BbfFirmasPoderes.Domain.Verification;
using BbfFirmasPoderes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BbfFirmasPoderes.Infrastructure.Decisioning;

public sealed record EvaluateCommand(
    string? DocumentId,
    string? Cnpj,
    string? Operacao,
    decimal? ValorOperacao,
    string? Currency,
    string[]? SignatariosSolicitados,
    DateTimeOffset? AsOf);

public sealed record AuthorityCommand(
    string? Cnpj,
    string? Operation,
    string? Signers,
    decimal? Valor,
    string? IdempotencyKey,
    string ConsumerId);

public abstract record EvaluateOutcome
{
    public sealed record Ok(Decision Decision, IReadOnlyList<DecisionEvidence> Evidencias) : EvaluateOutcome;
    public sealed record BadRequest(string Detail) : EvaluateOutcome;
    public sealed record NotFound : EvaluateOutcome;
    public sealed record Unprocessable(string Detail) : EvaluateOutcome;
}

public sealed class DecisionService(
    AppDbContext db,
    IOfficialSourceHealthStore sources,
    IIdempotencyStore idempotency)
{
    public async Task<EvaluateOutcome> EvaluateAsync(
        EvaluateCommand command,
        string actor,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var documentId = command.DocumentId?.Trim();
        var operacao = command.Operacao?.Trim();
        var signatarios = NormalizeSigners(command.SignatariosSolicitados);

        if (string.IsNullOrWhiteSpace(documentId))
            return new EvaluateOutcome.BadRequest("documentId é obrigatório.");
        if (string.IsNullOrWhiteSpace(operacao))
            return new EvaluateOutcome.BadRequest("operacao é obrigatória.");
        if (signatarios.Length == 0)
            return new EvaluateOutcome.BadRequest("signatariosSolicitados é obrigatório.");
        if (command.ValorOperacao is < 0)
            return new EvaluateOutcome.BadRequest("valorOperacao não pode ser negativo.");

        var document = await db.Documents
            .Include(d => d.Socios)
            .Include(d => d.Poderes)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);

        if (document is null)
            return new EvaluateOutcome.NotFound();

        if (!string.IsNullOrWhiteSpace(command.Cnpj)
            && DecisionText.DigitsOnly(command.Cnpj) != DecisionText.DigitsOnly(document.Cnpj)
            && !string.Equals(command.Cnpj.Trim(), document.Cnpj, StringComparison.OrdinalIgnoreCase))
        {
            return new EvaluateOutcome.BadRequest("cnpj não confere com o documento.");
        }

        if (document.Poderes.Count == 0 || document.Socios.Count == 0)
            return new EvaluateOutcome.Unprocessable("Documento sem modelo canônico (pessoas/poderes).");

        var currency = string.IsNullOrWhiteSpace(command.Currency)
            ? DecisionRuleCatalog.DefaultCurrency
            : command.Currency.Trim();
        var asOf = command.AsOf ?? DateTimeOffset.UtcNow;
        var valor = command.ValorOperacao ?? 0m;

        return await RunAndPersistAsync(
            document, operacao, valor, currency, signatarios, asOf, actor, correlationId, cancellationToken);
    }

    public async Task<EvaluateOutcome> EvaluateAuthorityAsync(
        AuthorityCommand command,
        string actor,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Cnpj) || !CnpjFormat.TryNormalize(command.Cnpj, out var formattedCnpj))
            return new EvaluateOutcome.BadRequest("cnpj é obrigatório no formato 00.000.000/0000-00.");
        if (string.IsNullOrWhiteSpace(command.Operation))
            return new EvaluateOutcome.BadRequest("operation é obrigatória.");
        var signatarios = SplitSigners(command.Signers);
        if (signatarios.Length == 0)
            return new EvaluateOutcome.BadRequest("signers é obrigatório (IDs separados por vírgula).");
        if (command.Valor is < 0)
            return new EvaluateOutcome.BadRequest("valor não pode ser negativo.");

        var operacao = command.Operation.Trim();
        var valor = command.Valor ?? 0m;
        var digits = CnpjFormat.DigitsOnly(formattedCnpj);
        var fingerprint = IdempotencyFingerprint.For(digits, operacao, signatarios, valor);
        var idempotencyKey = command.IdempotencyKey?.Trim();
        var consumerId = string.IsNullOrWhiteSpace(command.ConsumerId) ? "anonymous" : command.ConsumerId.Trim();

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            switch (idempotency.Lookup(consumerId, idempotencyKey, fingerprint))
            {
                case IdempotencyLookup.Hit hit:
                    var cached = await GetAsync(hit.DecisionId, cancellationToken);
                    if (cached is not null)
                        return new EvaluateOutcome.Ok(cached, DecisionJson.DeserializeEvidencias(cached.EvidenciasJson));
                    break;
                case IdempotencyLookup.Conflict:
                    return new EvaluateOutcome.BadRequest(
                        "Idempotency-Key reutilizada com parâmetros diferentes na janela de 24h.");
            }
        }

        var documents = await db.Documents
            .Include(d => d.Socios)
            .Include(d => d.Poderes)
            .Where(d => d.Cnpj == formattedCnpj || d.Cnpj == digits)
            .ToListAsync(cancellationToken);

        var document = documents
            .Where(d => CnpjFormat.DigitsOnly(d.Cnpj) == digits)
            .Where(d => d.Socios.Count > 0 && d.Poderes.Count > 0)
            .OrderByDescending(d => d.UploadedAt)
            .FirstOrDefault();

        if (document is null)
            return new EvaluateOutcome.BadRequest("cnpj sem documento canônico.");

        EvaluateOutcome outcome;
        if (sources.IsOpen(OfficialSourcesCatalog.DefaultJunta))
        {
            var junta = sources.Get(OfficialSourcesCatalog.DefaultJunta);
            var evidencias = new[]
            {
                new DecisionEvidence(
                    DecisionEvidence.TypeFonteOficial,
                    Trace: null,
                    Fonte: junta?.Nome ?? "Junta Comercial (stub)",
                    Detalhe: "Circuit breaker aberto — fallback MANUAL. Sem HTTP Junta (WF-11 stub).")
            };
            var result = new DecisionEvaluationResult(
                DecisionStatus.MANUAL,
                [
                    $"Fonte oficial indisponível (circuit breaker aberto em {junta?.Nome ?? OfficialSourcesCatalog.DefaultJunta}). Decisão MANUAL por fallback — sem consulta HTTP (WF-11 stub)."
                ],
                evidencias,
                null);
            outcome = await PersistAsync(
                document,
                operacao,
                valor,
                DecisionRuleCatalog.DefaultCurrency,
                signatarios,
                result,
                actor,
                correlationId,
                latencyMs: 0,
                cancellationToken);
        }
        else
        {
            outcome = await RunAndPersistAsync(
                document,
                operacao,
                valor,
                DecisionRuleCatalog.DefaultCurrency,
                signatarios,
                DateTimeOffset.UtcNow,
                actor,
                correlationId,
                cancellationToken);
        }

        if (outcome is EvaluateOutcome.Ok ok && !string.IsNullOrWhiteSpace(idempotencyKey))
            idempotency.Remember(consumerId, idempotencyKey, fingerprint, ok.Decision.DecisionId);

        return outcome;
    }

    public async Task<Decision?> GetAsync(string decisionId, CancellationToken cancellationToken = default)
        => await db.Decisions.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DecisionId == decisionId, cancellationToken);

    private async Task<EvaluateOutcome> RunAndPersistAsync(
        Document document,
        string operacao,
        decimal valor,
        string currency,
        string[] signatarios,
        DateTimeOffset asOf,
        string actor,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var clock = Stopwatch.StartNew();
        var result = DecisionEngine.Evaluate(new DecisionContext(
            document.DocumentId,
            document.Cnpj,
            document.ConfiancaOcr,
            document.ConfiancaIagen,
            document.ConfiancaNer,
            document.Socios.ToList(),
            document.Poderes.ToList(),
            operacao,
            valor,
            currency,
            signatarios,
            asOf));
        clock.Stop();

        return await PersistAsync(
            document,
            operacao,
            valor,
            currency,
            signatarios,
            result,
            actor,
            correlationId,
            (int)Math.Min(clock.ElapsedMilliseconds, int.MaxValue),
            cancellationToken);
    }

    private async Task<EvaluateOutcome> PersistAsync(
        Document document,
        string operacao,
        decimal valor,
        string currency,
        string[] signatarios,
        DecisionEvaluationResult result,
        string actor,
        string correlationId,
        int latencyMs,
        CancellationToken cancellationToken)
    {
        var evaluatedAt = DateTimeOffset.UtcNow;
        var decisionId = $"dec_{Guid.NewGuid():N}";
        var evidenciasJson = DecisionJson.SerializeEvidencias(result.Evidencias);

        var decision = new Decision
        {
            DecisionId = decisionId,
            DocumentId = document.DocumentId,
            Cnpj = document.Cnpj,
            Operacao = FormatOperacao(operacao, valor, currency),
            SignatariosSolicitados = signatarios,
            Status = result.Status,
            Motivos = result.Motivos.ToArray(),
            EvidenciasJson = evidenciasJson,
            VersionRules = DecisionRuleCatalog.RulesVersion,
            VersionCanonical = DecisionRuleCatalog.CanonicalVersion,
            VersionAiPrompt = DecisionRuleCatalog.AiPrompt,
            VersionAiModel = DecisionRuleCatalog.AiModel,
            EvaluatedAt = evaluatedAt,
            LatencyMs = latencyMs
        };

        db.Decisions.Add(decision);
        document.Status = result.Status == DecisionStatus.MANUAL
            ? DocStatus.revisao_humana
            : DocStatus.decidido;

        db.AuditEvents.Add(AuditEventFactory.Create(
            AuditEventTypes.DecisionEvaluated,
            correlationId,
            actor,
            $"Decisão {result.Status} (latência {decision.LatencyMs}ms)",
            document.DocumentId,
            decisionId,
            evaluatedAt));

        await db.SaveChangesAsync(cancellationToken);
        return new EvaluateOutcome.Ok(decision, result.Evidencias);
    }

    private static string[] NormalizeSigners(string[]? signatarios)
        => (signatarios ?? [])
            .Select(s => (s ?? string.Empty).Trim())
            .Where(s => s.Length > 0)
            .ToArray();

    private static string[] SplitSigners(string? signers)
        => string.IsNullOrWhiteSpace(signers)
            ? []
            : signers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string FormatOperacao(string operacao, decimal valor, string currency)
    {
        if (valor <= 0)
            return operacao;
        if (string.Equals(currency, DecisionRuleCatalog.DefaultCurrency, StringComparison.OrdinalIgnoreCase))
            return $"{operacao} — R$ {DecisionText.FormatBrl(valor)}";
        return $"{operacao} — {currency} {DecisionText.FormatBrl(valor)}";
    }
}
