using System.Text.RegularExpressions;

namespace BbfFirmasPoderes.Domain.Cnpj;

public static partial class CnpjFormat
{
    public const int DigitCount = 14;

    public static string DigitsOnly(string? value)
        => new((value ?? string.Empty).Where(char.IsDigit).ToArray());

    public static bool TryNormalize(string? raw, out string formatted)
    {
        formatted = string.Empty;
        var digits = DigitsOnly(raw);
        if (digits.Length != DigitCount)
            return false;
        if (digits.Distinct().Count() == 1)
            return false;

        formatted = $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}";
        return true;
    }

    public static bool IsFormattedOrDigits(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        var trimmed = raw.Trim();
        return Formatted().IsMatch(trimmed) || DigitsOnly(trimmed).Length == DigitCount;
    }

    [GeneratedRegex(@"^\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2}$")]
    private static partial Regex Formatted();
}
