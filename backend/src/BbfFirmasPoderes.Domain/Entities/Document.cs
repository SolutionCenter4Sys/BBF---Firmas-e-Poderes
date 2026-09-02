using BbfFirmasPoderes.Domain.Enums;

namespace BbfFirmasPoderes.Domain.Entities;

public class Document
{
    public string DocumentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string TipoSocietario { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DocStatus Status { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string? StoragePath { get; set; }
    public int Paginas { get; set; }
    public decimal ConfiancaOcr { get; set; }
    public decimal ConfiancaIagen { get; set; }
    public decimal ConfiancaNer { get; set; }
    public string? CorrelationId { get; set; }
    public string AnalysisJson { get; set; } = "{}";
    public int? CreditReadinessScore { get; set; }
    public string? CreditReadinessClassification { get; set; }
    public string? CreditReadinessRecommendation { get; set; }
    public string? CreditReadinessJustification { get; set; }

    public ICollection<Person> Socios { get; set; } = new List<Person>();
    public ICollection<Power> Poderes { get; set; } = new List<Power>();
    public ICollection<Decision> Decisions { get; set; } = new List<Decision>();
    public ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();
    public ICollection<KasRun> KasRuns { get; set; } = new List<KasRun>();
}
