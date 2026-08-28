namespace BbfFirmasPoderes.Domain.Kaas;

public static class KasDefaults
{
    public const string Journey = "testes-firmas-e-poderes";

    public const string DefaultRunUrl =
        "https://kaas-core-dev.up.railway.app/kas/triggers/journeys/testes-firmas-e-poderes/run";

    public const string ApiKeyHeader = "X-Flow-Api-Key";
    public const string ModeSync = "sync";
    public const string ActionIngest = "ingest";
    public const string ActionResult = "result";
    public const int TimeoutSeconds = 300;
}
