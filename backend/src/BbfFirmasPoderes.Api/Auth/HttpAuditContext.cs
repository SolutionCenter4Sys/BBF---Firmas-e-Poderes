using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BbfFirmasPoderes.Domain.Audit;
using BbfFirmasPoderes.Domain.Correlation;

namespace BbfFirmasPoderes.Api.Auth;

internal sealed class HttpAuditContext(IHttpContextAccessor accessor) : IAuditContext
{
    public string CorrelationId =>
        CorrelationContext.Current
        ?? accessor.HttpContext?.Items[CorrelationIds.HeaderName]?.ToString()
        ?? string.Empty;

    public string Actor
    {
        get
        {
            var user = accessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return "anonymous";

            return user.Identity.Name
                ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "anonymous";
        }
    }
}
