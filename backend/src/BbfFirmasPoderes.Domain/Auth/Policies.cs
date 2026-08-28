namespace BbfFirmasPoderes.Domain.Auth;

public static class Policies
{
    public const string HealthRead = "health:read";
    public const string Operador = "role:operador";
    public const string Auditor = "role:auditor";
    public const string Admin = "role:admin";
    public const string Consumer = "role:consumer";
    public const string DocumentsUpload = "documents:upload";
    public const string DocumentsRead = "documents:read";
    public const string DecisionRead = "decision:read";
    public const string DecisionReplay = "decision:replay";
    public const string ApiDecisionRead = "api:decision:read";
    public const string VerificationHealth = "verification:health";
    public const string AuditRead = "audit:read";
}
