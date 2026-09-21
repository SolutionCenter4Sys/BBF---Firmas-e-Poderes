using System.Security.Claims;

namespace BbfFirmasPoderes.Domain.Auth;

/// <summary>
/// Perfis WF-05. <c>operador</c> equivale a <c>analista</c> do catálogo mocks.ts.
/// </summary>
public static class Roles
{
    public const string Operador = "operador";
    public const string Auditor = "auditor";
    public const string Admin = "admin";
    public const string Consumer = "consumer";

    /// <summary>Alias do front/mocks para o perfil interno operador.</summary>
    public const string AnalistaAlias = "analista";

    public static readonly string[] All = [Operador, Auditor, Admin, Consumer];

    public static void NormalizeClaims(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity)
            return;

        var roleValues = identity.FindAll("role")
            .Concat(identity.FindAll(ClaimTypes.Role))
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (roleValues.Contains(AnalistaAlias) && !roleValues.Contains(Operador))
            identity.AddClaim(new Claim(ClaimTypes.Role, Operador));
    }
}
