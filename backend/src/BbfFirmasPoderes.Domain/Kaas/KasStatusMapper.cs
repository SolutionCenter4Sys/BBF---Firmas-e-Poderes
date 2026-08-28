using System.Text.Json;
using BbfFirmasPoderes.Domain.Enums;

namespace BbfFirmasPoderes.Domain.Kaas;

/// <summary>
/// Avança <see cref="DocStatus"/> a partir do JSON KAAS. Sem status reconhecido:
/// ingest ok → <c>processando_iagen</c>; result ok → <c>canonico_pronto</c>.
/// </summary>
public static class KasStatusMapper
{
    public static DocStatus FromPayload(string? json, KasRunAction action, bool ok)
    {
        if (!ok)
            return DocStatus.falha;

        if (TryFindVerdict(json, out var verdict))
            return verdict;

        if (TryFindStatus(json, out var mapped))
            return mapped;

        return action == KasRunAction.ingest
            ? DocStatus.processando_iagen
            : DocStatus.canonico_pronto;
    }

    /// <summary>Jornada sync: um POST. Sem verdict/status reconhecido → canónico pronto (depois o mapper pode baixar para revisão).</summary>
    public static DocStatus FromSync(string? json, bool ok)
    {
        if (!ok)
            return DocStatus.falha;

        if (TryFindVerdict(json, out var verdict))
            return verdict;

        if (TryFindStatus(json, out var mapped))
            return mapped;

        return DocStatus.canonico_pronto;
    }

    public static bool TryFindVerdict(string? json, out DocStatus status)
    {
        status = DocStatus.pendente;
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return TryKnownVerdictPaths(doc.RootElement, out status);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool TryFindStatus(string? json, out DocStatus status)
    {
        status = DocStatus.pendente;
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return TryKnownPaths(doc.RootElement, out status);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Só caminhos de pipeline. Não desce em arrays (fontes KAAS trazem status=failed/skipped).
    /// </summary>
    private static bool TryKnownPaths(JsonElement root, out DocStatus status)
    {
        if (TryPropertyStatus(root, out status))
            return true;

        foreach (var name in new[] { "payload", "result", "body", "output", "data" })
        {
            if (!root.TryGetProperty(name, out var child) || child.ValueKind != JsonValueKind.Object)
                continue;
            if (TryPropertyStatus(child, out status))
                return true;
            if (child.TryGetProperty("output", out var output)
                && output.ValueKind == JsonValueKind.Object
                && TryPropertyStatus(output, out status))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryKnownVerdictPaths(JsonElement root, out DocStatus status)
    {
        if (TryPropertyVerdict(root, out status))
            return true;

        foreach (var name in new[] { "payload", "result", "body", "output", "data" })
        {
            if (!root.TryGetProperty(name, out var child) || child.ValueKind != JsonValueKind.Object)
                continue;
            if (TryPropertyVerdict(child, out status))
                return true;
            if (child.TryGetProperty("output", out var output)
                && output.ValueKind == JsonValueKind.Object
                && TryPropertyVerdict(output, out status))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryPropertyVerdict(JsonElement obj, out DocStatus status)
    {
        status = DocStatus.pendente;
        if (obj.ValueKind != JsonValueKind.Object)
            return false;
        if (obj.TryGetProperty("verdict", out var verdictEl)
            && verdictEl.ValueKind == JsonValueKind.String
            && TryMapVerdict(verdictEl.GetString(), out status))
        {
            return true;
        }

        return false;
    }

    private static bool TryMapVerdict(string? raw, out DocStatus status)
    {
        status = DocStatus.pendente;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        status = raw.Trim().ToLowerInvariant() switch
        {
            "manual_analysis" or "review" or "revisao" or "revisão" or "revisao_humana" => DocStatus.revisao_humana,
            "approved" or "aprovado" or "pass" or "auto_approved" => DocStatus.canonico_pronto,
            "rejected" or "reprovado" or "deny" or "denied" => DocStatus.falha,
            _ => DocStatus.pendente
        };

        return status is not DocStatus.pendente;
    }

    private static bool TryPropertyStatus(JsonElement obj, out DocStatus status)
    {
        status = DocStatus.pendente;
        if (obj.ValueKind != JsonValueKind.Object)
            return false;
        if (obj.TryGetProperty("status", out var statusEl)
            && statusEl.ValueKind == JsonValueKind.String
            && TryMapToken(statusEl.GetString(), out status))
        {
            return true;
        }

        return false;
    }

    private static bool TryMapToken(string? raw, out DocStatus status)
    {
        status = DocStatus.pendente;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var token = raw.Trim();
        if (Enum.TryParse(token, ignoreCase: true, out status)
            && Enum.IsDefined(status))
        {
            return true;
        }

        status = token.ToLowerInvariant() switch
        {
            "failed" or "error" or "erro" => DocStatus.falha,
            "ocr" => DocStatus.processando_ocr,
            "iagen" or "ia-gen" or "ia_gen" => DocStatus.processando_iagen,
            "ner" => DocStatus.processando_ner,
            "canonical" or "canonico" or "canônico" => DocStatus.canonico_pronto,
            _ => DocStatus.pendente
        };

        return status is not DocStatus.pendente
               || token.Equals("pendente", StringComparison.OrdinalIgnoreCase);
    }
}
