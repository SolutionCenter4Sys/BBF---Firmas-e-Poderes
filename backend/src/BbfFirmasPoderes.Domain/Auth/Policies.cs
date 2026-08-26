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
}
