namespace BbfFirmasPoderes.Domain.Audit;

public interface IAuditContext
{
    string CorrelationId { get; }
    string Actor { get; }
}
