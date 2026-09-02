using System.Text.Json;
using System.Text.Json.Nodes;

namespace BbfFirmasPoderes.Domain.Canonical;

public sealed record NormalizedKaasAnalysis(
    string AnalysisJson,
    CreditReadinessScore Score);

public static class KaasAnalysisNormalizer
{
    public static NormalizedKaasAnalysis Normalize(JsonElement root)
    {
        var score = CreditReadinessScorer.Calculate(root);
        var output = FindAnalysisOutput(root) ?? root;
        var powersExtraction = GetObject(output, "powers_extraction");
        var envelope = powersExtraction is null ? null : GetObject(powersExtraction.Value, "envelope");
        var documentAnalysis = GetObject(output, "document_analysis");
        var authority = GetObject(output, "authority_validation");

        var normalized = new JsonObject
        {
            ["document"] = Clone(GetObject(output, "document")) ?? BuildDocument(documentAnalysis),
            ["company"] = BuildCompany(
                GetObject(output, "company"),
                envelope is null ? null : GetObject(envelope.Value, "grantor")),
            ["people"] = Clone(GetProperty(output, "people"))
                ?? Clone(envelope is null ? null : GetProperty(envelope.Value, "representatives"))
                ?? new JsonArray(),
            ["powers"] = Clone(GetProperty(output, "powers"))
                ?? Clone(envelope is null ? null : GetProperty(envelope.Value, "powers"))
                ?? new JsonArray(),
            ["signature_rules"] = Clone(GetProperty(output, "signature_rules"))
                ?? Clone(envelope is null ? null : GetProperty(envelope.Value, "signature_rules"))
                ?? new JsonArray(),
            ["signatures"] = Clone(GetProperty(output, "signatures"))
                ?? Clone(documentAnalysis is null ? null : GetProperty(documentAnalysis.Value, "signatures"))
                ?? new JsonArray(),
            ["stamps"] = Clone(GetProperty(output, "stamps"))
                ?? Clone(documentAnalysis is null ? null : GetProperty(documentAnalysis.Value, "stamps"))
                ?? new JsonArray(),
            ["validations"] = Clone(GetProperty(output, "validations"))
                ?? Clone(authority is null ? null : GetProperty(authority.Value, "checks"))
                ?? new JsonArray(),
            ["risks"] = Clone(GetProperty(output, "risks"))
                ?? Clone(authority is null ? null : GetProperty(authority.Value, "conflicts"))
                ?? new JsonArray(),
            ["pendencies"] = Clone(GetProperty(output, "pendencies"))
                ?? Clone(authority is null ? null : GetProperty(authority.Value, "pending_requirements"))
                ?? new JsonArray(),
            ["authority_decision"] = StringNode(output, "authority_decision")
                ?? StringNode(authority, "decision"),
            ["authority_reason"] = StringNode(output, "authority_reason")
                ?? StringNode(authority, "reason"),
            ["verdict"] = StringNode(output, "verdict"),
            ["score"] = score.Score,
            ["score_classification"] = score.Classification,
            ["recommendation"] = score.Recommendation,
            ["score_breakdown"] = JsonSerializer.SerializeToNode(score.Breakdown),
            ["score_justification"] = score.Justification,
            ["critical_blockers"] = JsonSerializer.SerializeToNode(score.CriticalBlockers)
        };

        return new NormalizedKaasAnalysis(
            normalized.ToJsonString(new JsonSerializerOptions { WriteIndented = false }),
            score);
    }

    private static JsonObject BuildDocument(JsonElement? analysis)
    {
        var result = new JsonObject();
        if (analysis is null)
            return result;

        var corpus = GetObject(analysis.Value, "corpus");
        Copy(result, "mime_type", corpus, "mime_type");
        Copy(result, "page_count", corpus, "page_count");
        Copy(result, "warnings", corpus, "warnings");
        Copy(result, "truncated", corpus, "truncated");
        Copy(result, "skipped_pages", corpus, "skipped_pages");
        Copy(result, "needs_ocr_pages", corpus, "needs_ocr_pages");
        Copy(result, "has_signatures", analysis, "has_signatures");
        Copy(result, "status", analysis, "status");
        Copy(result, "reason", analysis, "reason");
        Copy(result, "context_fields", analysis, "context_fields");
        return result;
    }

    private static JsonObject BuildCompany(JsonElement? company, JsonElement? grantor)
    {
        var result = Clone(company) as JsonObject ?? new JsonObject();
        if (grantor is null)
            return result;

        foreach (var name in new[] { "cnpj", "legal_name", "legal_form" })
        {
            var current = result[name];
            if (current is JsonValue value
                && value.TryGetValue<string>(out var text)
                && !string.IsNullOrWhiteSpace(text)
                && !text.StartsWith("$.", StringComparison.Ordinal))
            {
                continue;
            }

            var fallback = GetProperty(grantor.Value, name);
            if (fallback is not null)
                result[name] = Clone(fallback);
        }
        return result;
    }

    private static void Copy(
        JsonObject target,
        string targetName,
        JsonElement? source,
        string sourceName)
    {
        var value = source is null ? null : GetProperty(source.Value, sourceName);
        if (value is not null)
            target[targetName] = Clone(value);
    }

    private static JsonElement? FindAnalysisOutput(JsonElement element, int depth = 0)
    {
        if (depth > 8 || element.ValueKind != JsonValueKind.Object)
            return null;
        if (GetProperty(element, "powers_extraction") is not null
            || GetProperty(element, "document_analysis") is not null
            || GetProperty(element, "authority_validation") is not null)
        {
            return element;
        }

        foreach (var name in new[] { "result", "output", "body", "data", "payload" })
        {
            var child = GetObject(element, name);
            if (child is null)
                continue;
            var found = FindAnalysisOutput(child.Value, depth + 1);
            if (found is not null)
                return found;
        }
        return null;
    }

    private static JsonNode? StringNode(JsonElement element, string name)
        => StringNode((JsonElement?)element, name);

    private static JsonNode? StringNode(JsonElement? element, string name)
    {
        var value = element is null ? null : GetProperty(element.Value, name);
        return value is { ValueKind: JsonValueKind.String }
            ? JsonValue.Create(value.Value.GetString())
            : null;
    }

    private static JsonNode? Clone(JsonElement? value)
        => value is null ? null : JsonNode.Parse(value.Value.GetRawText());

    private static JsonElement? GetObject(JsonElement element, string name)
    {
        var value = GetProperty(element, name);
        return value is { ValueKind: JsonValueKind.Object } ? value : null;
    }

    private static JsonElement? GetProperty(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;
        if (element.TryGetProperty(name, out var direct))
            return direct;
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return property.Value;
        }
        return null;
    }
}
