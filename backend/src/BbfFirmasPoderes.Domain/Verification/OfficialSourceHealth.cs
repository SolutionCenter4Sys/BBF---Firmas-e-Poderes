namespace BbfFirmasPoderes.Domain.Verification;

public sealed class OfficialSourceHealth
{
    public string SourceId { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Status { get; set; } = SourceHealthStatuses.Operacional;
    public decimal Uptime24h { get; set; }
    public int LatenciaP95Ms { get; set; }
    public decimal ErrorRate { get; set; }
    public decimal CacheHitRate { get; set; }
    public DateTimeOffset UltimaConsulta { get; set; }
    public string CircuitBreaker { get; set; } = CircuitBreakerStates.Fechado;
    public string? Observacao { get; set; }

    public OfficialSourceHealth Clone()
        => new()
        {
            SourceId = SourceId,
            Nome = Nome,
            Status = Status,
            Uptime24h = Uptime24h,
            LatenciaP95Ms = LatenciaP95Ms,
            ErrorRate = ErrorRate,
            CacheHitRate = CacheHitRate,
            UltimaConsulta = UltimaConsulta,
            CircuitBreaker = CircuitBreaker,
            Observacao = Observacao
        };
}

public static class SourceHealthStatuses
{
    public const string Operacional = "operacional";
    public const string Degradado = "degradado";
    public const string Indisponivel = "indisponivel";
}
