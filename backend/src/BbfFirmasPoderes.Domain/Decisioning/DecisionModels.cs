using BbfFirmasPoderes.Domain.Entities;
using BbfFirmasPoderes.Domain.Enums;

namespace BbfFirmasPoderes.Domain.Decisioning;

public sealed record DecisionSourceTrace(int Page, int OffsetStart, int OffsetEnd, string Snippet);

public sealed record DecisionEvidence(
    string Type,
    DecisionSourceTrace? Trace,
    string? Fonte,
    string? Detalhe)
{
    public const string TypeDocumento = "documento";
    public const string TypeFonteOficial = "fonte_oficial";
}

public sealed record DecisionContext(
    string DocumentId,
    string Cnpj,
    decimal ConfiancaOcr,
    decimal ConfiancaIagen,
    decimal ConfiancaNer,
    IReadOnlyList<Person> People,
    IReadOnlyList<Power> Powers,
    string Operacao,
    decimal ValorOperacao,
    string Currency,
    IReadOnlyList<string> SignatariosSolicitados,
    DateTimeOffset AsOf);

public sealed record DecisionEvaluationResult(
    DecisionStatus Status,
    IReadOnlyList<string> Motivos,
    IReadOnlyList<DecisionEvidence> Evidencias,
    string? MatchedPowerId);
