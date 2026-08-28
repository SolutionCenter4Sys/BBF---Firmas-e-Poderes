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

namespace BbfFirmasPoderes.Infrastructure.Kaas;

public sealed class OutboxKaasProcessor(
    AppDbContext db,
    IKasClient kas,
    IDocumentBlobStore blobs,
    IAuditContext audit,
    ILogger<OutboxKaasProcessor> logger)
{
    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken = default)
    {
        var message = await db.OutboxMessages
            .Where(o => o.ProcessedAt == null && o.Type == OutboxTypes.DocumentUploaded)
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

        try
        {
            document.Status = DocStatus.processando_ocr;
            await db.SaveChangesAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(document.StoragePath))
                throw new FileNotFoundException("Documento sem storage_path.", document.DocumentId);

            var bytes = await blobs.ReadAllBytesAsync(document.StoragePath, cancellationToken);
            var documentUrl = KasDataUrl.FromBytes(bytes, document.ContentType);

            document.Status = DocStatus.processando_iagen;
            await db.SaveChangesAsync(cancellationToken);

            var call = await kas.PostAsync(
                new KasSyncEnvelope { Payload = new KasSyncPayload { DocumentUrl = documentUrl } },
                cancellationToken);

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

            if (call.Ok)
            {
                AppendAudit(
                    AuditEventTypes.OcrCompleted,
                    message.CorrelationId,
                    document.DocumentId,
                    $"OCR/jornada concluído para {document.FileName}");
                await ApplyCanonicalAsync(document, call.RawBody, cancellationToken);
            }
            else
            {
                logger.LogWarning(
                    "KAAS recusou {DocumentId}: {Error}",
                    document.DocumentId,
                    errorText);
            }

            message.ProcessedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pipeline KAAS falhou para {DocumentId}", document.DocumentId);
            document.Status = DocStatus.falha;
            message.ProcessedAt = DateTimeOffset.UtcNow;
            var failure = KasErrorText.FromException(ex);
            AppendAudit(
                AuditEventTypes.KasResult,
                message.CorrelationId,
                document.DocumentId,
                failure);
            if (!db.ChangeTracker.Entries<KasRun>().Any(e => e.Entity.DocumentId == document.DocumentId))
            {
                db.KasRuns.Add(new KasRun
                {
                    KasRunId = Guid.NewGuid(),
                    CorrelationId = message.CorrelationId,
                    DocumentId = document.DocumentId,
                    Action = KasRunAction.ingest,
                    FileName = document.FileName,
                    HttpStatus = 0,
                    Ok = false,
                    OccurredAt = DateTimeOffset.UtcNow,
                    PayloadJson = JsonSerializer.Serialize(new { error = ex.GetType().Name, message = ex.Message })
                });
            }

            await db.SaveChangesAsync(cancellationToken);
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
            details,
            documentId));
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
                if (el.TryGetProperty(name, out var found) && found.TryGetInt32(out value))
                    return true;
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
