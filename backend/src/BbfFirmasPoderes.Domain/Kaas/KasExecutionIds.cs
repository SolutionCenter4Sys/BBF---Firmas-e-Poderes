using System.Text.Json;

namespace BbfFirmasPoderes.Domain.Kaas;

/// <summary>Espelha <c>extractExecutionId</c> de <c>src/lib/kas-ids.ts</c>.</summary>
public static class KasExecutionIds
{
    private static readonly string[] Keys = ["executionId", "execution_id", "runId", "run_id"];

    public static string? Extract(JsonElement? value, int depth = 0)
    {
        if (value is null || depth > 5)
            return null;

        var el = value.Value;
        if (el.ValueKind is not JsonValueKind.Object)
            return null;

        foreach (var key in Keys)
        {
            if (el.TryGetProperty(key, out var found)
                && found.ValueKind == JsonValueKind.String)
            {
                var text = found.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
        }

        foreach (var prop in el.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.Object)
                continue;

            var nested = Extract(prop.Value, depth + 1);
            if (nested is not null)
                return nested;
        }

        return null;
    }

    public static string? ExtractFromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return Extract(doc.RootElement.Clone());
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
