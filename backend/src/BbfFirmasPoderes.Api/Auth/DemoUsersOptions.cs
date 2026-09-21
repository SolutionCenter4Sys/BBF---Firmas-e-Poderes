namespace BbfFirmasPoderes.Api.Auth;

/// <summary>
/// Usuários demo WF-20. Senhas só por env (<c>DemoUsers__*Password</c> / <c>.env.example</c>).
/// </summary>
public sealed class DemoUsersOptions
{
    public const string SectionName = "DemoUsers";

    public string OperadorEmail { get; set; } = "ana.silva@bbf.com.br";
    public string OperadorPassword { get; set; } = string.Empty;
    public string AuditorEmail { get; set; } = "auditor.interno@bbf.com.br";
    public string AuditorPassword { get; set; } = string.Empty;
    public int TokenLifetimeHours { get; set; } = 8;
}

public sealed class DatabaseStartupOptions
{
    public const string SectionName = "Database";

    public bool MigrateOnStartup { get; set; }
}
