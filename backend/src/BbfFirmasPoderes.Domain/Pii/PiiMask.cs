using System.Text;
using System.Text.RegularExpressions;

namespace BbfFirmasPoderes.Domain.Pii;

/// <summary>
/// Máscara LGPD para CPF/CNPJ. Colunas são varchar (aceitam valor cheio ou já mascarado).
/// Use na borda de log/API — não no valor persistido do seed quando o mock já vem mascarado.
/// </summary>
public static partial class PiiMask
{
    public static string Cpf(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var digits = DigitsOnly(value);
        if (digits.Length != 11)
            return value;

        return $"{digits[..3]}.{digits[3..6]}.***-{digits[9..]}";
    }

    public static string Cnpj(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var digits = DigitsOnly(value);
        if (digits.Length != 14)
            return value;

        return $"{digits[..2]}.***.***/****-{digits[12..]}";
    }

    /// <summary>
    /// Mascara CPF/CNPJ em texto livre (logs). CNPJ primeiro para não colidir com CPF.
    /// </summary>
    public static string InText(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        var masked = CnpjPattern().Replace(value, static m => Cnpj(m.Value));
        return CpfPattern().Replace(masked, static m => Cpf(m.Value));
    }

    private static string DigitsOnly(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (char.IsDigit(c))
                sb.Append(c);
        }

        return sb.ToString();
    }

    [GeneratedRegex(@"\d{2}\.?\d{3}\.?\d{3}/?\d{4}-?\d{2}")]
    private static partial Regex CnpjPattern();

    [GeneratedRegex(@"\d{3}\.?\d{3}\.?\d{3}-?\d{2}")]
    private static partial Regex CpfPattern();
}
