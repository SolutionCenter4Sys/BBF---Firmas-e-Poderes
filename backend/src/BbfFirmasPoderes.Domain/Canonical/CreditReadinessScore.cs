using System.Text.Json;

namespace BbfFirmasPoderes.Domain.Canonical;

public sealed record ScoreComponent(string Name, int Score, int Weight);

public sealed record CreditReadinessScore(
    int Score,
    string Classification,
    string Recommendation,
    string Justification,
    IReadOnlyList<ScoreComponent> Breakdown,
    IReadOnlyList<string> CriticalBlockers);

/// <summary>
/// Score determinístico de prontidão documental para análise de crédito.
/// Não substitui decisão de crédito; mede qualidade, completude, consistência,
/// validade de representação e rastreabilidade das evidências recebidas do KAAS.
/// </summary>
public static class CreditReadinessScorer
{
    public static CreditReadinessScore Calculate(JsonElement root)
    {
        var allText = root.GetRawText();
        var blockers = CriticalBlockers(allText);

        var document = FindObject(root, "document") ?? FindObject(root, "document_analysis");
        var company = FindBestCompany(root);
        var people = FindArray(root, "people", "representatives", "pessoas", "socios");
        var powers = FindArray(root, "powers", "poderes");
        var validations = FindArray(root, "validations", "checks");
        var risks = FindArray(root, "risks", "conflicts");
        var signatures = FindArray(root, "signatures");

        var quality = DocumentQuality(document, allText);
        var completeness = Completeness(company, people, powers, signatures);
        var consistency = Consistency(validations, risks, allText);
        var authority = Authority(root, powers, allText);
        var traceability = Traceability(people, powers);
        var authorityDecision = ReadFirstString(root, "authority_decision", "decision", "verdict")?.ToLowerInvariant();

        var components = new[]
        {
            new ScoreComponent("document_quality", quality, 20),
            new ScoreComponent("completeness", completeness, 20),
            new ScoreComponent("consistency", consistency, 20),
            new ScoreComponent("authority_and_powers", authority, 30),
            new ScoreComponent("traceability", traceability, 10)
        };

        var weighted = components.Sum(c => c.Score * c.Weight) / 100;
        if (blockers.Count > 0)
            weighted = Math.Min(weighted, 49);

        var requiresManualReview = authorityDecision is "review" or "manual_analysis" or "revisao_manual";
        var recommendation = blockers.Any(IsRejectionBlocker)
            ? "reprovado"
            : requiresManualReview
                ? "revisao_manual"
            : weighted >= 75 && blockers.Count == 0
                ? "aprovado"
                : "revisao_manual";
        var classification = weighted >= 75 ? "alto" : weighted >= 50 ? "medio" : "baixo";
        var justification = BuildJustification(weighted, recommendation, components, blockers);

        return new CreditReadinessScore(weighted, classification, recommendation, justification, components, blockers);
    }

    private static int DocumentQuality(JsonElement? document, string text)
    {
        var score = 100;
        if (ContainsAny(text, "documento ilegível", "documento ilegivel", "password protected", "pdf protegido"))
            return 0;
        if (ReadBool(document, "truncated") == true)
            score -= 35;
        score -= Math.Min(30, ArrayCount(document, "skipped_pages") * 10);
        score -= Math.Min(20, ArrayCount(document, "warnings") * 5);
        return Math.Clamp(score, 0, 100);
    }

    private static int Completeness(
        JsonElement? company,
        JsonElement? people,
        JsonElement? powers,
        JsonElement? signatures)
    {
        var score = 0;
        if (HasValue(company, "cnpj"))
            score += 20;
        if (HasValue(company, "legal_name", "razao_social", "razaoSocial"))
            score += 20;
        if (people is { ValueKind: JsonValueKind.Array } && people.Value.GetArrayLength() > 0)
            score += 25;
        if (powers is { ValueKind: JsonValueKind.Array } && powers.Value.GetArrayLength() > 0)
            score += 25;
        if (signatures is { ValueKind: JsonValueKind.Array } && signatures.Value.GetArrayLength() > 0)
            score += 10;
        return score;
    }

    private static int Consistency(JsonElement? validations, JsonElement? risks, string text)
    {
        var score = 100;
        if (risks is { ValueKind: JsonValueKind.Array })
            score -= Math.Min(60, risks.Value.GetArrayLength() * 20);

        if (validations is { ValueKind: JsonValueKind.Array } && validations.Value.GetArrayLength() > 0)
        {
            var failures = validations.Value.EnumerateArray().Count(v =>
                ContainsAny(v.GetRawText(), "\"status\":\"fail", "\"status\":\"failed", "\"ok\":false",
                    "\"result\":\"divergente", "\"result\":\"invalid"));
            score -= Math.Min(60, failures * 15);
        }

        if (ContainsAny(text, "cnpj divergente", "divergência no qsa", "divergencia no qsa"))
            score -= 35;
        return Math.Clamp(score, 0, 100);
    }

    private static int Authority(JsonElement root, JsonElement? powers, string text)
    {
        var decision = ReadFirstString(root, "authority_decision", "decision", "verdict")?.ToLowerInvariant();
        var score = decision switch
        {
            "allow" or "approved" or "aprovado" => 100,
            "deny" or "denied" or "rejected" or "reprovado" => 0,
            "review" or "manual_analysis" or "revisao_manual" => 50,
            _ => powers is { ValueKind: JsonValueKind.Array } && powers.Value.GetArrayLength() > 0 ? 70 : 20
        };

        if (ContainsAny(text, "mandato vencido", "procuração vencida", "procuracao vencida",
                "sem poderes", "representação inválida", "representacao invalida"))
            score = 0;
        return score;
    }

    private static int Traceability(JsonElement? people, JsonElement? powers)
    {
        var total = 0;
        var traced = 0;
        CountTrace(people, ref total, ref traced);
        CountTrace(powers, ref total, ref traced);
        return total == 0 ? 0 : (int)Math.Round(100m * traced / total, MidpointRounding.AwayFromZero);
    }

    private static void CountTrace(JsonElement? array, ref int total, ref int traced)
    {
        if (array is not { ValueKind: JsonValueKind.Array })
            return;
        foreach (var item in array.Value.EnumerateArray())
        {
            total++;
            if (ArrayCount(item, "citations") > 0 || FindObject(item, "sourceTrace", "source_trace") is not null)
                traced++;
        }
    }

    private static List<string> CriticalBlockers(string text)
    {
        var blockers = new List<string>();
        AddBlocker(text, blockers, "documento_ilegivel", "documento ilegível", "documento ilegivel");
        AddBlocker(text, blockers, "documento_protegido", "pdf protegido", "password protected");
        AddBlocker(text, blockers, "cnpj_divergente", "cnpj divergente");
        AddBlocker(text, blockers, "representacao_invalida", "representação inválida", "representacao invalida");
        AddBlocker(text, blockers, "mandato_vencido", "mandato vencido", "procuração vencida", "procuracao vencida");
        AddBlocker(text, blockers, "poderes_ausentes", "sem poderes", "ausência de poderes", "ausencia de poderes");
        return blockers;
    }

    private static bool IsRejectionBlocker(string blocker)
        => blocker is "documento_ilegivel" or "documento_protegido"
            or "representacao_invalida" or "mandato_vencido" or "poderes_ausentes";

    private static string BuildJustification(
        int score,
        string recommendation,
        IReadOnlyList<ScoreComponent> components,
        IReadOnlyList<string> blockers)
    {
        var weakest = components.OrderBy(c => c.Score).First();
        var text = $"Score {score}/100. Recomendação: {recommendation}. " +
            $"Menor componente: {weakest.Name} ({weakest.Score}/100, peso {weakest.Weight}%).";
        if (blockers.Count > 0)
            text += $" Bloqueios críticos: {string.Join(", ", blockers)}.";
        return text;
    }

    private static void AddBlocker(string text, ICollection<string> blockers, string code, params string[] terms)
    {
        if (ContainsAny(text, terms))
            blockers.Add(code);
    }

    private static bool ContainsAny(string text, params string[] terms)
        => terms.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));

    private static bool HasValue(JsonElement? obj, params string[] names)
        => names.Any(name => !string.IsNullOrWhiteSpace(ReadString(obj, name)));

    private static bool? ReadBool(JsonElement? obj, string name)
    {
        if (obj is null || !TryGetProperty(obj.Value, name, out var value))
            return null;
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static int ArrayCount(JsonElement? obj, string name)
        => obj is not null && TryGetProperty(obj.Value, name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.GetArrayLength()
            : 0;

    private static string? ReadFirstString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            var match = FindProperty(root, name, 0);
            if (match is { ValueKind: JsonValueKind.String })
                return match.Value.GetString();
        }
        return null;
    }

    private static string? ReadString(JsonElement? obj, string name)
        => obj is not null && TryGetProperty(obj.Value, name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static JsonElement? FindObject(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            var found = FindProperty(root, name, 0);
            if (found is { ValueKind: JsonValueKind.Object })
                return found;
        }
        return null;
    }

    private static JsonElement? FindArray(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            var found = FindProperty(root, name, 0);
            if (found is { ValueKind: JsonValueKind.Array })
                return found;
        }
        return null;
    }

    private static JsonElement? FindBestCompany(JsonElement root)
    {
        JsonElement? best = null;
        var bestScore = -1;
        Visit(root, 0);
        return best;

        void Visit(JsonElement element, int depth)
        {
            if (depth > 8)
                return;
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Object
                        && (property.Name.Equals("company", StringComparison.OrdinalIgnoreCase)
                            || property.Name.Equals("grantor", StringComparison.OrdinalIgnoreCase)))
                    {
                        var candidateScore =
                            (HasValue(property.Value, "cnpj") ? 1 : 0) +
                            (HasValue(property.Value, "legal_name", "razao_social", "razaoSocial") ? 1 : 0);
                        if (candidateScore > bestScore)
                        {
                            best = property.Value;
                            bestScore = candidateScore;
                        }
                    }
                    Visit(property.Value, depth + 1);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in element.EnumerateArray())
                    Visit(child, depth + 1);
            }
        }
    }

    private static JsonElement? FindProperty(JsonElement element, string name, int depth)
    {
        if (depth > 8)
            return null;
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (TryGetProperty(element, name, out var direct))
                return direct;
            foreach (var property in element.EnumerateObject())
            {
                var found = FindProperty(property.Value, name, depth + 1);
                if (found is not null)
                    return found;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
            {
                var found = FindProperty(child, name, depth + 1);
                if (found is not null)
                    return found;
            }
        }
        return null;
    }

    private static JsonElement? FindObject(JsonElement item, string first, string second)
    {
        foreach (var name in new[] { first, second })
        {
            if (TryGetProperty(item, name, out var value) && value.ValueKind == JsonValueKind.Object)
                return value;
        }
        return null;
    }

    private static bool TryGetProperty(JsonElement obj, string name, out JsonElement value)
    {
        value = default;
        if (obj.ValueKind != JsonValueKind.Object)
            return false;
        if (obj.TryGetProperty(name, out value))
            return true;
        foreach (var property in obj.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }
        return false;
    }
}
