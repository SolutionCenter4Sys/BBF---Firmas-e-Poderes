using BbfFirmasPoderes.Domain.Enums;

namespace BbfFirmasPoderes.Domain.Entities;

public class Decision
{
    public string DecisionId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Operacao { get; set; } = string.Empty;
    public string[] SignatariosSolicitados { get; set; } = [];
    public DecisionStatus Status { get; set; }
    public string[] Motivos { get; set; } = [];
    public string EvidenciasJson { get; set; } = "[]";
    public string VersionRules { get; set; } = string.Empty;
    public string VersionCanonical { get; set; } = string.Empty;
    public string VersionAiPrompt { get; set; } = string.Empty;
    public string VersionAiModel { get; set; } = string.Empty;
    public DateTimeOffset EvaluatedAt { get; set; }
    public int LatencyMs { get; set; }

    public Document Document { get; set; } = null!;
    public ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();
}
