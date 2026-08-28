namespace BbfFirmasPoderes.Domain.Verification;

public static class CircuitBreakerStates
{
    public const string Fechado = "fechado";
    public const string MeioAberto = "meio-aberto";
    public const string Aberto = "aberto";

    public static bool IsOpen(string? state)
        => string.Equals(state, Aberto, StringComparison.OrdinalIgnoreCase);
}
