using System.Security.Claims;

namespace BbfFirmasPoderes.Domain.Auth;

public static class ScopeClaims
{
    public static bool HasScope(ClaimsPrincipal user, string scope)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (string.IsNullOrWhiteSpace(scope))
            return false;

        return user.FindAll("scope")
            .Concat(user.FindAll("scp"))
            .SelectMany(c => (c.Value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Contains(scope, StringComparer.OrdinalIgnoreCase);
    }

    public static bool CanReadAuthority(ClaimsPrincipal user)
    {
        if (user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Operador))
            return true;
        if (user.IsInRole(Roles.Consumer))
            return true;
        return HasScope(user, Scopes.DecisionRead);
    }
}
