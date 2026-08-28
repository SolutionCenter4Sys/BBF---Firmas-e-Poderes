using BbfFirmasPoderes.Domain.Entities;

namespace BbfFirmasPoderes.Domain.Canonical;

/// <summary>
/// Resultado do mapeamento JSON KAAS → <see cref="Person"/> / <see cref="Power"/>.
/// <see cref="Structured"/> é falso quando o payload não traz arrays de pessoas e poderes.
/// </summary>
public sealed record CanonicalMapping(
    bool Structured,
    IReadOnlyList<Person> People,
    IReadOnlyList<Power> Powers,
    string? Cnpj,
    string? RazaoSocial,
    string? TipoSocietario)
{
    public static CanonicalMapping Unstructured { get; } = new(false, [], [], null, null, null);
}
