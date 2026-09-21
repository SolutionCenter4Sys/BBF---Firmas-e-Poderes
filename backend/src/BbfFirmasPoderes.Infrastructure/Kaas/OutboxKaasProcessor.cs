using System.Text.Json;
using BbfFirmasPoderes.Domain.Audit;
using BbfFirmasPoderes.Domain.Canonical;
using BbfFirmasPoderes.Domain.Documents;
using BbfFirmasPoderes.Domain.Entities;
using BbfFirmasPoderes.Domain.Enums;
using BbfFirmasPoderes.Domain.Kaas;
using BbfFirmasPoderes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BbfFirmasPoderes.Infrastructure.Kaas;

public sealed class OutboxKaasProcessor(
    AppDbContext db,
    IKasClient kas,
    IDocumentBlobStore blobs,
    IAuditContext audit,
    IOptions<KasOptions> kasOptions,
    ILogger<OutboxKaasProcessor> logger)
{
    private const int AuditDetailsMax = 2048;
    private const int ClassificationMax = 16;
    private const int RecommendationMax = 32;
    private const int JustificationMax = 2048;

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var message = await db.OutboxMessages
            .Where(o => o.ProcessedAt == null
                && o.Type == OutboxTypes.DocumentUploaded
                && (o.NextAttemptAt == null || o.NextAttemptAt <= now))
            .OrderBy(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (message is null)
            return false;

        await ProcessMessageAsync(message, cancellationToken);
        return true;
    }

    private async Task ProcessMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        Domain.Correlation.CorrelationContext.Set(message.CorrelationId);

        var document = message.DocumentId is null
            ? null
            : await db.Documents.FirstOrDefaultAsync(d => d.DocumentId == message.DocumentId, cancellationToken);

        if (document is null)
        {
            logger.LogWarning("Outbox {OutboxId} sem documento. Marca processado.", message.OutboxId);
            message.ProcessedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        KasCallResult? lastCall = null;
        try
        {
            document.Status = DocStatus.processando_ocr;
            await db.SaveChangesAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(document.StoragePath))
                throw new FileNotFoundException("Documento sem storage_path.", document.DocumentId);

            var bytes = await blobs.ReadAllBytesAsync(document.StoragePath, cancellationToken);

            document.Status = DocStatus.processando_iagen;
            await db.SaveChangesAsync(cancellationToken);

            await using var stream = new MemoryStream(bytes, writable: false);
            var call = await kas.PostDocumentAsync(
                stream,
                document.FileName,
                document.ContentType ?? "application/octet-stream",
                kasOptions.Value.MultipartFileField,
                cancellationToken);
            lastCall = call;

            var executionId = KasExecutionIds.Extract(call.Parsed)
                ?? KasExecutionIds.ExtractFromJson(call.RawBody);
            var errorText = call.Ok
                ? null
                : KasErrorText.FromHttp(call.HttpStatus, call.RawBody, call.StatusText);

            db.KasRuns.Add(ToKasRun(
                document,
                message.CorrelationId,
                KasRunAction.ingest,
                call,
                executionId));

            var auditDetail = errorText
                ?? $"KAAS sync HTTP {call.HttpStatus} ({call.DurationMs}ms)";
            AppendAudit(
                AuditEventTypes.KasIngest,
                message.CorrelationId,
                document.DocumentId,
                auditDetail);

            AppendAudit(
                AuditEventTypes.KasResult,
                message.CorrelationId,
                document.DocumentId,
                auditDetail);

            document.Status = KasStatusMapper.FromSync(call.RawBody, call.Ok);
            ApplyExecutionHints(document, call.RawBody);
            await db.SaveChangesAsync(cancellationToken);

            if (call.Ok)
            {
                AppendAudit(
                    AuditEventTypes.OcrCompleted,
                    message.CorrelationId,
                    document.DocumentId,
                    $"OCR/jornada concluído para {document.FileName}");
                try
                {
                    await ApplyCanonicalAsync(document, call.RawBody, cancellationToken);
                }
                catch (Exception canonicalEx)
                {
                    logger.LogError(
                        canonicalEx,
                        "Canônico falhou após HTTP 200 para {DocumentId}. Payload já está em kas_runs.",
                        document.DocumentId);
                    if (document.Status != DocStatus.falha)
                        document.Status = DocStatus.revisao_humana;
                    AppendAudit(
                        AuditEventTypes.KasResult,
                        message.CorrelationId,
                        document.DocumentId,
                        Clamp($"Canônico falhou após KAAS 200: {canonicalEx.Message}", AuditDetailsMax));
                }
            }
            else
            {
                logger.LogWarning(
                    "KAAS recusou {DocumentId}: {Error}",
                    document.DocumentId,
                    errorText);
            }

            message.ProcessedAt = DateTimeOffset.UtcNow;
            message.LastError = errorText;
            message.NextAttemptAt = null;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pipeline KAAS falhou para {DocumentId}", document.DocumentId);
            if (TransportError.IsTransient(ex, lastCall)
                && message.AttemptCount + 1 < TransportError.MaxAttempts)
            {
                await ScheduleRetryAsync(message, document.DocumentId, ex, cancellationToken);
                return;
            }

            await MarkFailedAsync(message, document.DocumentId, lastCall, ex, cancellationToken);
        }
    }

    private async Task ScheduleRetryAsync(
        OutboxMessage message,
        string documentId,
        Exception ex,
        CancellationToken cancellationToken)
    {
        var failure = KasErrorText.FromException(ex);
        db.ChangeTracker.Clear();

        var outbox = await db.OutboxMessages
            .FirstOrDefaultAsync(o => o.OutboxId == message.OutboxId, cancellationToken);
        if (outbox is null)
            return;

        outbox.AttemptCount += 1;
        outbox.LastError = Clamp(failure, 1024);
        outbox.NextAttemptAt = DateTimeOffset.UtcNow + TransportError.DelayAfter(outbox.AttemptCount);
        outbox.ProcessedAt = null;

        var document = await db.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
        if (document is not null && document.Status == DocStatus.falha)
            document.Status = DocStatus.processando_iagen;

        AppendAudit(
            AuditEventTypes.KasResult,
            message.CorrelationId,
            documentId,
            $"Tentativa {outbox.AttemptCount}/{TransportError.MaxAttempts} em {outbox.NextAttemptAt:u}: {failure}");

        await db.SaveChangesAsync(cancellationToken);
        logger.LogWarning(
            "Reagendou {DocumentId} tentativa {Attempt} para {When}",
            documentId,
            outbox.AttemptCount,
            outbox.NextAttemptAt);
    }

    /// <summary>
    /// Registra a falha a partir de um contexto limpo: as entidades pendentes que causaram o erro
    /// ficam descartadas, senão o próprio SaveChanges do catch falha e o outbox nunca é concluído.
    /// </summary>
    private async Task MarkFailedAsync(
        OutboxMessage message,
        string documentId,
        KasCallResult? call,
        Exception ex,
        CancellationToken cancellationToken)
    {
        var failure = KasErrorText.FromException(ex);
        db.ChangeTracker.Clear();

        try
        {
            var document = await db.Documents
                .FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
            if (document is not null)
                document.Status = DocStatus.falha;

            var outbox = await db.OutboxMessages
                .FirstOrDefaultAsync(o => o.OutboxId == message.OutboxId, cancellationToken);
            if (outbox is not null)
            {
                outbox.ProcessedAt = DateTimeOffset.UtcNow;
                outbox.LastError = Clamp(failure, 1024);
                outbox.NextAttemptAt = null;
                outbox.AttemptCount = Math.Max(outbox.AttemptCount, 1);
            }

            AppendAudit(AuditEventTypes.KasResult, message.CorrelationId, documentId, failure);

            db.KasRuns.Add(call is null
                ? new KasRun
                {
                    KasRunId = Guid.NewGuid(),
                    CorrelationId = message.CorrelationId,
                    DocumentId = documentId,
                    Action = KasRunAction.ingest,
                    FileName = document?.FileName ?? string.Empty,
                    HttpStatus = 0,
                    Ok = false,
                    OccurredAt = DateTimeOffset.UtcNow,
                    PayloadJson = JsonSerializer.Serialize(new { message = failure })
                }
                : ToKasRun(
                    document ?? new Document { DocumentId = documentId, FileName = string.Empty },
                    message.CorrelationId,
                    KasRunAction.ingest,
                    call,
                    KasExecutionIds.Extract(call.Parsed) ?? KasExecutionIds.ExtractFromJson(call.RawBody)));

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception persistEx)
        {
            logger.LogError(persistEx, "Não foi possível registrar a falha de {DocumentId}", documentId);
        }
    }

    private static KasRun ToKasRun(
        Document document,
        string correlationId,
        KasRunAction action,
        KasCallResult call,
        string? executionId)
    {
        var payload = string.IsNullOrWhiteSpace(call.RawBody) ? "{}" : call.RawBody;
        if (call.Parsed is null && (payload.Length == 0 || payload[0] is not '{' and not '['))
            payload = JsonSerializer.Serialize(new { raw = call.RawBody, error = call.StatusText });

        return new KasRun
        {
            KasRunId = Guid.NewGuid(),
            CorrelationId = correlationId,
            DocumentId = document.DocumentId,
            ExecutionId = executionId,
            Action = action,
            FileName = document.FileName,
            HttpStatus = call.HttpStatus,
            Ok = call.Ok,
            OccurredAt = DateTimeOffset.UtcNow,
            DurationMs = call.DurationMs,
            PayloadJson = payload
        };
    }

    private async Task ApplyCanonicalAsync(
        Document document,
        string syncJson,
        CancellationToken cancellationToken)
    {
        var mapping = CanonicalMapper.TryMap(syncJson, document.DocumentId);

        if (!mapping.Structured)
        {
            if (document.Status != DocStatus.falha)
                document.Status = DocStatus.revisao_humana;
            return;
        }

        var existingPeople = await db.People
            .Where(p => p.DocumentId == document.DocumentId)
            .ToListAsync(cancellationToken);
        var existingPowers = await db.Powers
            .Where(p => p.DocumentId == document.DocumentId)
            .ToListAsync(cancellationToken);

        db.People.RemoveRange(existingPeople);
        db.Powers.RemoveRange(existingPowers);
        db.People.AddRange(mapping.People);
        db.Powers.AddRange(mapping.Powers);

        if (!string.IsNullOrWhiteSpace(mapping.Cnpj)
            && !mapping.Cnpj.Contains('•', StringComparison.Ordinal))
            document.Cnpj = mapping.Cnpj;
        if (!string.IsNullOrWhiteSpace(mapping.RazaoSocial)
            && !mapping.RazaoSocial.Contains('•', StringComparison.Ordinal))
            document.RazaoSocial = mapping.RazaoSocial;
        if (!string.IsNullOrWhiteSpace(mapping.TipoSocietario))
            document.TipoSocietario = mapping.TipoSocietario;

        document.AnalysisJson = mapping.AnalysisJson;
        document.CreditReadinessScore = mapping.CreditReadiness?.Score;
        document.CreditReadinessClassification = ClampOrNull(mapping.CreditReadiness?.Classification, ClassificationMax);
        document.CreditReadinessRecommendation = ClampOrNull(mapping.CreditReadiness?.Recommendation, RecommendationMax);
        document.CreditReadinessJustification = ClampOrNull(mapping.CreditReadiness?.Justification, JustificationMax);

        if (document.Status is not DocStatus.revisao_humana and not DocStatus.falha)
            document.Status = DocStatus.canonico_pronto;

        AppendAudit(
            AuditEventTypes.CanonicalReady,
            document.CorrelationId ?? string.Empty,
            document.DocumentId,
            $"Modelo canônico pronto ({mapping.People.Count} pessoas, {mapping.Powers.Count} poderes)");
    }

    private void AppendAudit(string type, string correlationId, string documentId, string details)
    {
        db.AuditEvents.Add(AuditEventFactory.Create(
            type,
            correlationId,
            audit.Actor,
            Clamp(details, AuditDetailsMax),
            documentId));
    }

    private static string Clamp(string? value, int max)
        => ClampOrNull(value, max) ?? string.Empty;

    private static string? ClampOrNull(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value.Length <= max ? value : value[..max];
    }

    private static void ApplyExecutionHints(Document document, string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (TryGetIntDeep(doc.RootElement, out var paginas, 0, "paginas", "pages", "page_count"))
                document.Paginas = paginas;
        }
        catch (JsonException)
        {
            // payload não-JSON: ignora hints
        }
    }

    private static bool TryGetIntDeep(JsonElement el, out int value, int depth, params string[] names)
    {
        value = 0;
        if (depth > 6)
            return false;

        if (el.ValueKind == JsonValueKind.Object)
        {
            foreach (var name in names)
            {
                if (!el.TryGetProperty(name, out var found))
                    continue;
                if (found.ValueKind == JsonValueKind.Number && found.TryGetInt32(out value))
                    return true;
                if (found.ValueKind == JsonValueKind.String
                    && int.TryParse(found.GetString(), out value))
                {
                    return true;
                }
            }

            foreach (var prop in el.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Object
                    && TryGetIntDeep(prop.Value, out value, depth + 1, names))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
