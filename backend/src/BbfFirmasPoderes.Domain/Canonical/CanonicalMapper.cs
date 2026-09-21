using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using BbfFirmasPoderes.Domain.Entities;
using BbfFirmasPoderes.Domain.Enums;
using BbfFirmasPoderes.Domain.Pii;

namespace BbfFirmasPoderes.Domain.Canonical;

/// <summary>
/// Mapeia JSON KAAS para o modelo canônico (schema <c>canonicalSchemaPreview</c> / tipos Person e Power do mock).
/// Aceita <c>pessoas|socios</c> e <c>poderes</c> na raiz ou em <c>body|canonical|result|data|payload</c>.
/// </summary>
public static class CanonicalMapper
{
    private static readonly string[] EnvelopeKeys =
        ["body", "canonical", "result", "data", "payload", "output", "envelope", "powers_extraction"];
    private static readonly string[] PeopleKeys = ["pessoas", "socios", "people", "persons", "representatives"];
    private static readonly string[] PowerKeys = ["poderes", "powers"];
    private static readonly Regex CnpjRegex = new(@"\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2}", RegexOptions.Compiled);

    // Limites das colunas em people/powers/documents. O texto integral permanece em analysis_json.
    private const int IdMax = 64;
    private const int NomeMax = 256;
    private const int RgMax = 64;
    private const int PersonTypeMax = 8;
    private const int QualificacaoMax = 128;
    private const int CargoMax = 128;
    private const int OperacaoMax = 256;
    private const int CurrencyMax = 3;
    private const int SnippetMax = 1024;
    private const int RazaoSocialMax = 256;
    private const int CnpjMax = 32;

    public static CanonicalMapping TryMap(string? json, string documentId)
    {
        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(documentId))
            return CanonicalMapping.Unstructured;

        try
        {
            using var doc = JsonDocument.Parse(json);
            return TryMap(doc.RootElement, documentId);
        }
        catch (JsonException)
        {
            return CanonicalMapping.Unstructured;
        }
    }

    public static CanonicalMapping TryMap(JsonElement root, string documentId)
    {
        var normalized = KaasAnalysisNormalizer.Normalize(root);
        if (!TryFindCanonicalObject(root, depth: 0, out var source))
            return CanonicalMapping.Unstructured;

        var peopleEl = FindArray(source, PeopleKeys);
        var powersEl = FindArray(source, PowerKeys);
        if (peopleEl is null || powersEl is null)
            return CanonicalMapping.Unstructured;

        var people = MapPeople(peopleEl.Value, documentId);
        var powers = MapPowers(powersEl.Value, documentId);
        var grantor = TryGetObject(source, "grantor");

        var cnpj = ReadString(source, "cnpj")
            ?? ReadString(grantor, "cnpj")
            ?? TryExtractCnpj(root);
        if (string.IsNullOrWhiteSpace(cnpj) || cnpj.Contains('•', StringComparison.Ordinal) || LooksLikeJsonPath(cnpj))
            cnpj = TryExtractCnpj(root) ?? (LooksLikeJsonPath(cnpj) ? null : cnpj);

        var razao = FirstLegalName(
            ReadAnalysisCompanyName(normalized.AnalysisJson),
            ReadString(source, "razaoSocial"),
            ReadString(source, "razao_social"),
            ReadString(grantor, "legal_name"),
            ReadString(grantor, "razaoSocial"),
            ReadString(source, "legal_name"));

        var tipo = InferTipoSocietario(
            razao,
            ReadString(source, "tipoSocietario")
            ?? ReadString(source, "tipo_societario")
            ?? ReadString(grantor, "legal_form")
            ?? ReadString(grantor, "tipoSocietario")
            ?? ReadString(source, "legal_form")
            ?? ReadString(source, "company_type"));

        return new CanonicalMapping(
            Structured: true,
            People: people,
            Powers: powers,
            Cnpj: cnpj is null ? null : Clamp(cnpj, CnpjMax),
            RazaoSocial: razao is null ? null : Clamp(razao, RazaoSocialMax),
            TipoSocietario: tipo,
            AnalysisJson: normalized.AnalysisJson,
            CreditReadiness: normalized.Score);
    }

    /// <summary>Normaliza LTDA / S.A. / EIRELI a partir do campo explícito ou do sufixo da razão social.</summary>
    public static string? InferTipoSocietario(string? razaoSocial, string? explicitTipo = null)
    {
        if (TryNormalizeTipo(explicitTipo, out var fromField))
            return fromField;

        if (string.IsNullOrWhiteSpace(razaoSocial))
            return null;

        var u = razaoSocial.ToUpperInvariant();
        if (u.Contains("EIRELI", StringComparison.Ordinal))
            return "EIRELI";
        if (u.Contains("S.A.", StringComparison.Ordinal)
            || u.Contains("S/A", StringComparison.Ordinal)
            || Regex.IsMatch(u, @"\bS\.?\s*A\.?\b"))
        {
            return "S.A.";
        }

        if (u.Contains("LTDA", StringComparison.Ordinal))
            return "LTDA";

        return null;
    }

    private static bool TryNormalizeTipo(string? raw, out string tipo)
    {
        tipo = string.Empty;
        if (string.IsNullOrWhiteSpace(raw) || LooksLikeJsonPath(raw))
            return false;

        var compact = raw.Trim().ToUpperInvariant().Replace(" ", "", StringComparison.Ordinal);
        tipo = compact switch
        {
            "LTDA" or "LTDA." or "LIMITADA" => "LTDA",
            "EIRELI" => "EIRELI",
            "SA" or "S.A." or "S.A" or "S/A" or "S/A." => "S.A.",
            _ => string.Empty
        };
        return tipo.Length > 0;
    }

    private static bool LooksLikeJsonPath(string? value)
        => !string.IsNullOrWhiteSpace(value) && value.TrimStart().StartsWith("$.", StringComparison.Ordinal);

    private static string? FirstLegalName(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (LooksLikeLegalName(candidate))
                return candidate!.Trim();
        }

        return null;
    }

    internal static bool LooksLikeLegalName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || LooksLikeJsonPath(value))
            return false;

        var text = value.Trim();
        if (text.Length < 3 || text.Length > RazaoSocialMax)
            return false;

        var lower = text.ToLowerInvariant();
        if (lower.Contains("emissão", StringComparison.Ordinal)
            || lower.Contains("emissao", StringComparison.Ordinal)
            || lower.Contains("notas fiscais", StringComparison.Ordinal)
            || lower.Contains("ultrapas", StringComparison.Ordinal)
            || lower.Contains("cujos valores", StringComparison.Ordinal))
        {
            return false;
        }

        var hasCompanySuffix = lower.Contains("ltda", StringComparison.Ordinal)
            || lower.Contains("eireli", StringComparison.Ordinal)
            || Regex.IsMatch(text, @"\bS[\./]?\s*A\.?\b", RegexOptions.IgnoreCase);
        if (hasCompanySuffix)
            return true;

        if (text.Contains('•', StringComparison.Ordinal))
            return true;

        if (char.IsLower(text[0]) && text.Contains(' ', StringComparison.Ordinal))
            return false;

        return text.Count(char.IsLetter) >= 3;
    }

    private static string? ReadAnalysisCompanyName(string analysisJson)
    {
        if (string.IsNullOrWhiteSpace(analysisJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(analysisJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object
                || !doc.RootElement.TryGetProperty("company", out var company)
                || company.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return ReadString(company, "legal_name")
                ?? ReadString(company, "razaoSocial")
                ?? ReadString(company, "razao_social");
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryFindCanonicalObject(JsonElement el, int depth, out JsonElement found)
    {
        found = default;
        if (depth > 6 || el.ValueKind != JsonValueKind.Object)
            return false;

        if (FindArray(el, PeopleKeys) is not null && FindArray(el, PowerKeys) is not null)
        {
            found = el;
            return true;
        }

        foreach (var key in EnvelopeKeys)
        {
            if (TryGetProperty(el, key, out var nested)
                && nested.ValueKind == JsonValueKind.Object
                && TryFindCanonicalObject(nested, depth + 1, out found))
            {
                return true;
            }
        }

        foreach (var prop in el.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.Object
                && TryFindCanonicalObject(prop.Value, depth + 1, out found))
            {
                return true;
            }
        }

        return false;
    }

    private static List<Person> MapPeople(JsonElement array, string documentId)
    {
        var people = new List<Person>();
        var index = 1;
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var rawId = ReadString(item, "personId") ?? ReadString(item, "person_id") ?? ReadString(item, "id");
            var personType = ReadString(item, "person_type") ?? ReadString(item, "personType") ?? "pf";
            var cpf = ReadString(item, "cpf");
            var cnpj = ReadString(item, "cnpj");
            var name = ReadString(item, "nome") ?? ReadString(item, "name") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name) || name.Contains('•', StringComparison.Ordinal))
                name = NameFromCitation(item) ?? name;

            people.Add(new Person
            {
                PersonId = MakeId(documentId, rawId, "p", index),
                DocumentId = documentId,
                Nome = Clamp(name, NomeMax),
                Cpf = PiiMask.Cpf(cpf),
                Documento = personType.Equals("pj", StringComparison.OrdinalIgnoreCase)
                    ? PiiMask.Cnpj(cnpj)
                    : PiiMask.Cpf(cpf),
                Rg = Clamp(ReadString(item, "rg"), RgMax),
                PersonType = Clamp(personType.ToLowerInvariant(), PersonTypeMax),
                Quotas = ReadDecimal(item, "quotas"),
                MandateStart = ReadDate(item, "mandate_start") ?? ReadDate(item, "mandateStart"),
                MandateEnd = ReadDate(item, "mandate_end") ?? ReadDate(item, "mandateEnd"),
                Qualificacao = Clamp(
                    ReadString(item, "qualificacao")
                        ?? ReadString(item, "qualification")
                        ?? personType.ToUpperInvariant(),
                    QualificacaoMax),
                Cargo = Clamp(ReadString(item, "cargo") ?? ReadString(item, "role"), CargoMax),
                Status = ParsePersonStatus(ReadString(item, "status"))
            });
            index++;
        }

        return people;
    }

    private static string? NameFromCitation(JsonElement item)
    {
        if (!TryGetProperty(item, "citations", out var citations)
            || citations.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var citation in citations.EnumerateArray())
        {
            if (citation.ValueKind != JsonValueKind.Object
                || !string.Equals(ReadString(citation, "field"), "name", StringComparison.OrdinalIgnoreCase))
                continue;

            var excerpt = ReadString(citation, "excerpt");
            if (string.IsNullOrWhiteSpace(excerpt))
                continue;

            var separator = excerpt.LastIndexOf(':');
            var candidate = separator >= 0 ? excerpt[(separator + 1)..] : excerpt;
            const string appointed = "sociedade ";
            var appointedAt = candidate.IndexOf(appointed, StringComparison.OrdinalIgnoreCase);
            if (appointedAt >= 0)
                candidate = candidate[(appointedAt + appointed.Length)..];

            candidate = candidate.Split(',', StringSplitOptions.TrimEntries)[0].Trim();
            if (!string.IsNullOrWhiteSpace(candidate))
                return candidate;
        }

        return null;
    }

    private static List<Power> MapPowers(JsonElement array, string documentId)
    {
        var powers = new List<Power>();
        var index = 1;
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var rawId = ReadString(item, "powerId") ?? ReadString(item, "power_id") ?? ReadString(item, "id");
            var limite = TryGetObject(item, "limite") ?? TryGetObject(item, "limit");
            var modo = TryGetObject(item, "modoAssinatura") ?? TryGetObject(item, "modo_assinatura");
            var vigencia = TryGetObject(item, "vigencia") ?? TryGetObject(item, "validity");
            var trace = TryGetObject(item, "sourceTrace") ?? TryGetObject(item, "source_trace");
            var citation = FirstCitation(item);

            powers.Add(new Power
            {
                PowerId = MakeId(documentId, rawId, "pw", index),
                DocumentId = documentId,
                Pessoa = Clamp(
                    ReadString(item, "pessoa")
                        ?? ReadString(item, "person")
                        ?? FirstString(item, "granted_to"),
                    NomeMax),
                Operacao = Clamp(
                    ReadString(item, "operacao")
                        ?? ReadString(item, "operation")
                        ?? ReadString(item, "text"),
                    OperacaoMax),
                LimiteCurrency = Clamp(
                    ReadString(limite, "currency") ?? ReadString(item, "limiteCurrency") ?? "BRL",
                    CurrencyMax),
                LimiteValue = ReadDecimal(limite, "value")
                    ?? ReadDecimal(item, "limiteValue")
                    ?? ReadDecimal(item, "value_limit")
                    ?? 0m,
                LimiteExpression = Clamp(
                    ReadString(limite, "expression")
                        ?? ReadString(item, "limiteExpression")
                        ?? JoinStrings(item, "restrictions"),
                    OperacaoMax),
                ModoAssinaturaTipo = ParseSignatureMode(
                    ReadString(modo, "tipo")
                    ?? ReadString(item, "modoAssinaturaTipo")
                    ?? ReadString(item, "form")),
                ModoAssinaturaN = ReadInt(modo, "n") ?? ReadInt(item, "modoAssinaturaN"),
                ModoAssinaturaM = ReadInt(modo, "m") ?? ReadInt(item, "modoAssinaturaM"),
                ModoAssinaturaQualificacoes = ReadStringArray(modo, "qualificacoes") ?? ReadStringArray(item, "modoAssinaturaQualificacoes"),
                VigenciaFrom = ReadDate(vigencia, "validFrom") ?? ReadDate(vigencia, "valid_from") ?? ReadDate(item, "vigenciaFrom") ?? default,
                VigenciaTo = ReadDate(vigencia, "validTo") ?? ReadDate(vigencia, "valid_to") ?? ReadDate(item, "vigenciaTo"),
                SourcePage = ReadInt(trace, "page")
                    ?? ReadInt(item, "sourcePage")
                    ?? ReadInt(item, "source_page")
                    ?? ReadInt(citation, "page")
                    ?? 0,
                SourceOffsetStart = ReadInt(trace, "offsetStart") ?? ReadInt(trace, "offset_start") ?? ReadInt(item, "sourceOffsetStart") ?? 0,
                SourceOffsetEnd = ReadInt(trace, "offsetEnd") ?? ReadInt(trace, "offset_end") ?? ReadInt(item, "sourceOffsetEnd") ?? 0,
                SourceSnippet = Clamp(
                    ReadString(trace, "snippet")
                        ?? ReadString(item, "sourceSnippet")
                        ?? ReadString(citation, "excerpt"),
                    SnippetMax)
            });
            index++;
        }

        return powers;
    }

    private static string MakeId(string documentId, string? raw, string prefix, int index)
    {
        var local = string.IsNullOrWhiteSpace(raw) ? $"{prefix}{index}" : raw.Trim();
        var id = local.StartsWith(documentId + ":", StringComparison.Ordinal)
            ? local
            : $"{documentId}:{local}";

        // Ids longos do KAAS cabem via fallback posicional, estável entre reprocessamentos.
        return id.Length <= IdMax ? id : Clamp($"{documentId}:{prefix}{index}", IdMax);
    }

    /// <summary>Corta o texto ao limite da coluna, sinalizando o truncamento com reticências.</summary>
    private static string Clamp(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var trimmed = value.Trim();
        if (trimmed.Length <= max)
            return trimmed;

        return max <= 1 ? trimmed[..max] : trimmed[..(max - 1)].TrimEnd() + "…";
    }

    private static JsonElement? FindArray(JsonElement obj, string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetProperty(obj, name, out var el) && el.ValueKind == JsonValueKind.Array)
                return el;
        }

        return null;
    }

    private static JsonElement? FirstCitation(JsonElement item)
    {
        if (!TryGetProperty(item, "citations", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var el in arr.EnumerateArray())
        {
            if (el.ValueKind == JsonValueKind.Object)
                return el;
        }

        return null;
    }

    private static string? FirstString(JsonElement item, string name)
    {
        if (!TryGetProperty(item, name, out var el))
            return null;

        if (el.ValueKind == JsonValueKind.String)
            return el.GetString();

        if (el.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var child in el.EnumerateArray())
        {
            if (child.ValueKind == JsonValueKind.String)
            {
                var s = child.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                    return s;
            }
        }

        return null;
    }

    private static string? JoinStrings(JsonElement item, string name)
    {
        if (!TryGetProperty(item, name, out var el) || el.ValueKind != JsonValueKind.Array)
            return null;

        var parts = new List<string>();
        foreach (var child in el.EnumerateArray())
        {
            if (child.ValueKind == JsonValueKind.String)
            {
                var s = child.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                    parts.Add(s);
            }
        }

        return parts.Count == 0 ? null : string.Join("; ", parts);
    }

    private static string? TryExtractCnpj(JsonElement root)
    {
        foreach (var text in EnumerateStrings(root, 0))
        {
            var match = CnpjRegex.Match(text);
            if (match.Success)
                return match.Value;
        }

        return null;
    }

    private static IEnumerable<string> EnumerateStrings(JsonElement el, int depth)
    {
        if (depth > 8)
            yield break;

        switch (el.ValueKind)
        {
            case JsonValueKind.String:
                var s = el.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                    yield return s;
                break;
            case JsonValueKind.Object:
                foreach (var prop in el.EnumerateObject())
                {
                    foreach (var nested in EnumerateStrings(prop.Value, depth + 1))
                        yield return nested;
                }
                break;
            case JsonValueKind.Array:
                foreach (var child in el.EnumerateArray())
                {
                    foreach (var nested in EnumerateStrings(child, depth + 1))
                        yield return nested;
                }
                break;
        }
    }

    private static JsonElement? TryGetObject(JsonElement? parent, string name)
    {
        if (parent is null)
            return null;
        return TryGetProperty(parent.Value, name, out var el) && el.ValueKind == JsonValueKind.Object
            ? el
            : null;
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

    private static string? ReadString(JsonElement? obj, string name)
    {
        if (obj is null || !TryGetProperty(obj.Value, name, out var el))
            return null;

        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static int? ReadInt(JsonElement? obj, string name)
    {
        if (obj is null || !TryGetProperty(obj.Value, name, out var el))
            return null;

        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n))
            return n;

        if (el.ValueKind == JsonValueKind.String
            && int.TryParse(el.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
        {
            return n;
        }

        return null;
    }

    private static decimal? ReadDecimal(JsonElement? obj, string name)
    {
        if (obj is null || !TryGetProperty(obj.Value, name, out var el))
            return null;

        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var d))
            return d;

        if (el.ValueKind == JsonValueKind.String
            && decimal.TryParse(el.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out d))
        {
            return d;
        }

        return null;
    }

    private static string[]? ReadStringArray(JsonElement? obj, string name)
    {
        if (obj is null || !TryGetProperty(obj.Value, name, out var el) || el.ValueKind != JsonValueKind.Array)
            return null;

        var values = new List<string>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var s = item.GetString();
                if (!string.IsNullOrWhiteSpace(s))
                    values.Add(s);
            }
        }

        return values.Count == 0 ? null : values.ToArray();
    }

    private static DateOnly? ReadDate(JsonElement? obj, string name)
    {
        var raw = ReadString(obj, name);
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;

        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
            return DateOnly.FromDateTime(dto.UtcDateTime);

        return null;
    }

    private static PersonStatus ParsePersonStatus(string? raw)
    {
        if (string.Equals(raw, "inativo", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "inactive", StringComparison.OrdinalIgnoreCase))
        {
            return PersonStatus.inativo;
        }

        return PersonStatus.ativo;
    }

    private static SignatureModeType ParseSignatureMode(string? raw)
    {
        if (string.Equals(raw, "conjunta", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "joint", StringComparison.OrdinalIgnoreCase))
        {
            return SignatureModeType.conjunta;
        }

        return SignatureModeType.isolada;
    }
}
