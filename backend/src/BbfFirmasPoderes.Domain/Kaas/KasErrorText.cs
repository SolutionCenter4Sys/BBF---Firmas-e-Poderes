using System.Text.Json;

namespace BbfFirmasPoderes.Domain.Kaas;

/// <summary>
/// Extrai mensagem humana do corpo de erro KAAS (ex.: array <c>message</c> do 400 Nest/class-validator).
/// </summary>
public static class KasErrorText
{
    private static readonly HashSet<string> GenericPhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        "Bad Request",
        "Internal server error",
        "Internal Server Error",
        "Error",
        "Unauthorized",
        "Forbidden",
        "Not Found"
    };

    public static string FromHttp(int httpStatus, string? rawBody, string? statusText)
    {
        var fromBody = FromBody(rawBody);
        if (!string.IsNullOrWhiteSpace(fromBody))
            return $"KAAS HTTP {httpStatus}: {fromBody}";

        var reason = string.IsNullOrWhiteSpace(statusText) ? "Error" : statusText.Trim();
        return $"KAAS HTTP {httpStatus} ({reason})";
    }

    public static string FromException(Exception ex)
        => $"Pipeline KAAS falhou: {ex.GetType().Name}: {ex.Message}";

    public static string? FromBody(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var message = ReadMessage(root);
            string? error = null;
            if (TryGetProperty(root, "error", out var err) && err.ValueKind == JsonValueKind.String)
                error = NullIfEmpty(err.GetString());

            var specific = PreferSpecific(error, message);
            if (!string.IsNullOrWhiteSpace(specific))
                return specific;
        }
        catch (JsonException)
        {
            // corpo não-JSON: devolve recorte
        }

        var trimmed = json.Trim();
        return trimmed.Length == 0 ? null : Truncate(trimmed, 300);
    }

    private static string? ReadMessage(JsonElement root)
    {
        if (!TryGetProperty(root, "message", out var msg))
            return null;

        if (msg.ValueKind == JsonValueKind.String)
            return NullIfEmpty(msg.GetString());

        if (msg.ValueKind != JsonValueKind.Array)
            return null;

        var parts = new List<string>();
        foreach (var item in msg.EnumerateArray())
        {
            var piece = item.ValueKind switch
            {
                JsonValueKind.String => item.GetString(),
                JsonValueKind.Object when TryGetProperty(item, "message", out var nested)
                    && nested.ValueKind == JsonValueKind.String => nested.GetString(),
                _ => null
            };
            if (!string.IsNullOrWhiteSpace(piece))
                parts.Add(piece);
        }

        return parts.Count == 0 ? null : string.Join("; ", parts);
    }

    private static bool TryGetProperty(JsonElement obj, string name, out JsonElement value)
    {
        value = default;
        if (obj.ValueKind != JsonValueKind.Object)
            return false;
        if (obj.TryGetProperty(name, out value))
            return true;

        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        return false;
    }

    private static string? PreferSpecific(string? error, string? message)
    {
        var errorGeneric = string.IsNullOrWhiteSpace(error) || GenericPhrases.Contains(error);
        var messageGeneric = string.IsNullOrWhiteSpace(message) || GenericPhrases.Contains(message);

        if (!errorGeneric)
            return error;
        if (!messageGeneric)
            return message;
        return NullIfEmpty(message) ?? NullIfEmpty(error);
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
