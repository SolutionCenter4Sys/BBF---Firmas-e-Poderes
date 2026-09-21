namespace BbfFirmasPoderes.Domain.Audit;

public static class AuditEventTypes
{
    public const string DocumentUploaded = "document.uploaded";
    public const string OcrCompleted = "ocr.completed";
    public const string CanonicalReady = "canonical.ready";
    public const string DecisionEvaluated = "decision.evaluated";
    public const string KasIngest = "kas.ingest";
    public const string KasResult = "kas.result";
    public const string CommandPersisted = "command.persisted";
}
