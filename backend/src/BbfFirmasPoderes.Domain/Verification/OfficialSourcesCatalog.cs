namespace BbfFirmasPoderes.Domain.Verification;

/// <summary>
/// Stub WF-11 alinhado a <c>sourceHealth</c> de <c>src/lib/mocks.ts</c>. Sem HTTP Junta.
/// </summary>
public static class OfficialSourcesCatalog
{
    public const string JuntaSp = "junta-sp";
    public const string JuntaRj = "junta-rj";
    public const string ReceitaFederal = "receita-federal";
    public const string CompliancePep = "compliance-pep";
    public const string DefaultJunta = JuntaSp;

    public static IReadOnlyList<OfficialSourceHealth> Defaults { get; } =
    [
        new()
        {
            SourceId = JuntaSp,
            Nome = "Junta Comercial SP",
            Status = SourceHealthStatuses.Operacional,
            Uptime24h = 0.998m,
            LatenciaP95Ms = 1240,
            ErrorRate = 0.002m,
            CacheHitRate = 0.42m,
            UltimaConsulta = new DateTimeOffset(2026, 4, 29, 14, 24, 55, TimeSpan.Zero),
            CircuitBreaker = CircuitBreakerStates.Fechado
        },
        new()
        {
            SourceId = JuntaRj,
            Nome = "Junta Comercial RJ",
            Status = SourceHealthStatuses.Degradado,
            Uptime24h = 0.962m,
            LatenciaP95Ms = 3850,
            ErrorRate = 0.043m,
            CacheHitRate = 0.28m,
            UltimaConsulta = new DateTimeOffset(2026, 4, 29, 13, 48, 12, TimeSpan.Zero),
            CircuitBreaker = CircuitBreakerStates.MeioAberto,
            Observacao = "Latência elevada nas últimas 2h — possível instabilidade do provedor"
        },
        new()
        {
            SourceId = ReceitaFederal,
            Nome = "Receita Federal — Situação Cadastral",
            Status = SourceHealthStatuses.Operacional,
            Uptime24h = 0.991m,
            LatenciaP95Ms = 880,
            ErrorRate = 0.009m,
            CacheHitRate = 0.61m,
            UltimaConsulta = new DateTimeOffset(2026, 4, 29, 14, 10, 0, TimeSpan.Zero),
            CircuitBreaker = CircuitBreakerStates.Fechado,
            Observacao = "Conector ativo (Fase 2 antecipada)"
        },
        new()
        {
            SourceId = CompliancePep,
            Nome = "Listas Compliance (PEP / Sanções)",
            Status = SourceHealthStatuses.Indisponivel,
            Uptime24h = 0.821m,
            LatenciaP95Ms = 0,
            ErrorRate = 1.0m,
            CacheHitRate = 0.0m,
            UltimaConsulta = new DateTimeOffset(2026, 4, 29, 7, 12, 33, TimeSpan.Zero),
            CircuitBreaker = CircuitBreakerStates.Aberto,
            Observacao = "Provedor em manutenção desde 07:00 — fallback para AnáliseManual ativo"
        }
    ];
}
