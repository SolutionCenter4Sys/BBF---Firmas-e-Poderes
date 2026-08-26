using BbfFirmasPoderes.Domain.Audit;
using BbfFirmasPoderes.Domain.Correlation;

namespace BbfFirmasPoderes.Infrastructure.Audit;

internal sealed class SystemAuditContext : IAuditContext
{
    public string CorrelationId => CorrelationContext.Current ?? string.Empty;

    public string Actor => "system";
}
