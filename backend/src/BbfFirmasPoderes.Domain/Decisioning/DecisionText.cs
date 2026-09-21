using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BbfFirmasPoderes.Domain.Decisioning;

public static partial class DecisionText
{
    public static string Fold(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var buffer = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            buffer.Append(char.ToLowerInvariant(ch));
        }

        return buffer.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string OperationKey(string? operacao)
    {
        var raw = operacao ?? string.Empty;
        var cut = EmDashCut().Replace(raw, " ");
        var folded = Fold(cut);
        var chars = folded.Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ').ToArray();
        return string.Join(' ', new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public static bool OperationMatches(string powerOperacao, string requested)
        => OperationKey(powerOperacao) == OperationKey(requested);

    public static bool RoleEquals(string? left, string? right)
    {
        var a = Fold(left);
        var b = Fold(right);
        if (a.Length == 0 || b.Length == 0)
            return false;
        if (a == b)
            return true;

        return a + "a" == b || b + "a" == a;
    }

    public static bool NameEquals(string? left, string? right)
        => Fold(left) == Fold(right) && Fold(left).Length > 0;

    public static IReadOnlyList<string> SplitRoles(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        return raw
            .Split(['+', '/', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => part.Length > 0)
            .ToArray();
    }

    public static string FormatBrl(decimal value)
        => value.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"));

    public static int Percent(decimal score)
        => (int)decimal.Round(score * 100m, 0, MidpointRounding.AwayFromZero);

    public static string DigitsOnly(string? value)
        => new((value ?? string.Empty).Where(char.IsDigit).ToArray());

    [GeneratedRegex(@"\s*[—–\-]\s*.*$")]
    private static partial Regex EmDashCut();
}
