namespace BbfFirmasPoderes.Domain.Kaas;

public static class KasDefaults
{
    public const string DefaultRunUrl =
        "https://kaas-core-dev.up.railway.app/kas/triggers/journeys/testes-firmas-e-poderes/run";

    public const string ApiKeyHeader = "X-Flow-Api-Key";
    public const string ModeSync = "sync";
    // Jornada síncrona pode levar até 5 minutos. Margem evita cancelar a leitura
    // quando o KAAS termina próximo do limite operacional.
    public const int TimeoutSeconds = 600;
}
