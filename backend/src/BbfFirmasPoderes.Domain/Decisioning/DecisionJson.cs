using System.Text.Json;
using System.Text.Json.Serialization;

namespace BbfFirmasPoderes.Domain.Decisioning;

public static class DecisionJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    public static string SerializeEvidencias(IReadOnlyList<DecisionEvidence> evidencias)
        => JsonSerializer.Serialize(evidencias, Options);

    public static IReadOnlyList<DecisionEvidence> DeserializeEvidencias(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
            return [];

        return JsonSerializer.Deserialize<List<DecisionEvidence>>(json, Options) ?? [];
    }

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
