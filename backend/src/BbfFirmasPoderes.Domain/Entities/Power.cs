using BbfFirmasPoderes.Domain.Enums;

namespace BbfFirmasPoderes.Domain.Entities;

public class Power
{
    public string PowerId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string Pessoa { get; set; } = string.Empty;
    public string Operacao { get; set; } = string.Empty;
    public string LimiteCurrency { get; set; } = "BRL";
    public decimal LimiteValue { get; set; }
    public string LimiteExpression { get; set; } = string.Empty;
    public SignatureModeType ModoAssinaturaTipo { get; set; }
    public int? ModoAssinaturaN { get; set; }
    public int? ModoAssinaturaM { get; set; }
    public string[]? ModoAssinaturaQualificacoes { get; set; }
    public DateOnly VigenciaFrom { get; set; }
    public DateOnly? VigenciaTo { get; set; }
    public int SourcePage { get; set; }
    public int SourceOffsetStart { get; set; }
    public int SourceOffsetEnd { get; set; }
    public string SourceSnippet { get; set; } = string.Empty;

    public Document Document { get; set; } = null!;
}
