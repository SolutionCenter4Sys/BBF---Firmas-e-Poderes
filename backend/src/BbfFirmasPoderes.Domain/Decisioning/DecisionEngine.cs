using BbfFirmasPoderes.Domain.Entities;
using BbfFirmasPoderes.Domain.Enums;

namespace BbfFirmasPoderes.Domain.Decisioning;

/// <summary>
/// Avalia RN01–RN04 e TH01 sobre o canônico. Determinístico: mesmos inputs → mesmos
/// <see cref="DecisionStatus"/>, motivos e evidências (sem relógio, sem I/O).
/// </summary>
public static class DecisionEngine
{
    public static DecisionEvaluationResult Evaluate(DecisionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var th01 = EvaluateThreshold(context);
        if (th01 is not null)
            return th01;

        var signers = SignerParser.Resolve(context.SignatariosSolicitados, context.People, out var unresolved);
        var rn01 = EvaluateSigners(signers, unresolved);
        if (rn01 is not null)
            return rn01;

        var forOperation = context.Powers
            .Where(p => DecisionText.OperationMatches(p.Operacao, context.Operacao))
            .ToList();
        if (forOperation.Count == 0)
        {
            return Reprovado(
                [
                    $"Nenhum poder canônico cobre a operação informada (Regra {DecisionRuleCatalog.Rn02} v{DecisionRuleCatalog.RulesVersion})."
                ],
                []);
        }

        var asOfDate = DateOnly.FromDateTime(context.AsOf.UtcDateTime);
        var vigente = forOperation.Where(p => IsVigente(p, asOfDate)).ToList();
        if (vigente.Count == 0)
        {
            return Reprovado(
                [
                    $"Nenhum poder vigente em {asOfDate:yyyy-MM-dd} para a operação (Regra {DecisionRuleCatalog.Rn04} v{DecisionRuleCatalog.RulesVersion})."
                ],
                EvidenciasFrom(forOperation));
        }

        var cargos = signers.Select(s => s.Cargo).ToList();
        var covering = vigente
            .Where(p => CurrencyMatches(p, context.Currency)
                        && ModeSatisfied(p, cargos)
                        && context.ValorOperacao <= p.LimiteValue)
            .OrderBy(p => p.LimiteValue)
            .ThenBy(p => p.PowerId, StringComparer.Ordinal)
            .ToList();

        if (covering.Count == 0)
        {
            return Reprovado(
                [
                    BuildLimitMotive(context, vigente, cargos)
                ],
                EvidenciasFrom(vigente));
        }

        var matched = covering[0];
        var nomes = string.Join(" e ", signers.Select(s => s.Person.Nome));
        var motivos = new[]
        {
            $"Operação dentro do limite de R$ {DecisionText.FormatBrl(matched.LimiteValue)} com modo {matched.ModoAssinaturaTipo} {matched.Pessoa} (Regra {DecisionRuleCatalog.Rn02} + {DecisionRuleCatalog.Rn03} v{DecisionRuleCatalog.RulesVersion}).",
            $"Signatário(s) {nomes} confirmado(s) como ATIVO no modelo canônico (Regra {DecisionRuleCatalog.Rn01} v{DecisionRuleCatalog.RulesVersion}). Junta Comercial real fora de escopo (WF-11).",
            $"Poder vigente (validFrom {matched.VigenciaFrom:yyyy-MM-dd}{(matched.VigenciaTo is { } to ? $", validTo {to:yyyy-MM-dd}" : ", sem revogação")}) (Regra {DecisionRuleCatalog.Rn04} v{DecisionRuleCatalog.RulesVersion})."
        };

        return new DecisionEvaluationResult(
            DecisionStatus.APROVADO,
            motivos,
            EvidenciasFrom([matched]),
            matched.PowerId);
    }

    private static DecisionEvaluationResult? EvaluateThreshold(DecisionContext context)
    {
        var scores = new (string Stage, decimal Score)[]
        {
            ("OCR", context.ConfiancaOcr),
            ("IAGen", context.ConfiancaIagen),
            ("NER", context.ConfiancaNer)
        };
        var min = scores.MinBy(s => s.Score);
        if (min.Score >= DecisionRuleCatalog.ConfidenceThreshold)
            return null;

        var evidencias = EvidenciasFrom(context.Powers);
        return new DecisionEvaluationResult(
            DecisionStatus.MANUAL,
            [
                $"Confiança da extração {min.Stage} abaixo do threshold ({DecisionText.Percent(min.Score)}% < {DecisionText.Percent(DecisionRuleCatalog.ConfidenceThreshold)}%) — Regra {DecisionRuleCatalog.Th01} v{DecisionRuleCatalog.ThresholdVersion}."
            ],
            evidencias,
            null);
    }

    private static DecisionEvaluationResult? EvaluateSigners(
        IReadOnlyList<SignerParser.ResolvedSigner> signers,
        IReadOnlyList<string> unresolved)
    {
        if (unresolved.Count > 0)
        {
            var who = string.Join(", ", unresolved);
            return Reprovado(
                [
                    $"Signatário(s) não encontrado(s) no modelo canônico: {who} (Regra {DecisionRuleCatalog.Rn01} v{DecisionRuleCatalog.RulesVersion})."
                ],
                []);
        }

        var inactive = signers.Where(s => s.Person.Status != PersonStatus.ativo).ToList();
        if (inactive.Count == 0)
            return null;

        var whoInactive = string.Join(", ", inactive.Select(s => s.Person.Nome));
        return Reprovado(
            [
                $"Sócio {whoInactive} consta como INATIVO no modelo canônico (Regra {DecisionRuleCatalog.Rn01} v{DecisionRuleCatalog.RulesVersion})."
            ],
            []);
    }

    private static bool IsVigente(Power power, DateOnly asOf)
    {
        if (asOf < power.VigenciaFrom)
            return false;
        return power.VigenciaTo is not { } to || asOf <= to;
    }

    private static bool CurrencyMatches(Power power, string currency)
        => string.Equals(power.LimiteCurrency, currency, StringComparison.OrdinalIgnoreCase);

    private static bool ModeSatisfied(Power power, IReadOnlyList<string> signerCargos)
    {
        var required = RequiredRoles(power);
        if (required.Count == 0)
            return false;

        var matched = required.Count(role => signerCargos.Any(cargo => DecisionText.RoleEquals(cargo, role)));

        if (power.ModoAssinaturaTipo == SignatureModeType.isolada)
            return matched >= 1;

        var need = power.ModoAssinaturaN ?? required.Count;
        return matched >= need;
    }

    private static IReadOnlyList<string> RequiredRoles(Power power)
    {
        if (power.ModoAssinaturaQualificacoes is { Length: > 0 } quals)
            return quals;

        return DecisionText.SplitRoles(power.Pessoa);
    }

    private static string BuildLimitMotive(
        DecisionContext context,
        IReadOnlyList<Power> vigente,
        IReadOnlyList<string> cargos)
    {
        var isolada = vigente.FirstOrDefault(p => p.ModoAssinaturaTipo == SignatureModeType.isolada);
        var conjunta = vigente.FirstOrDefault(p => p.ModoAssinaturaTipo == SignatureModeType.conjunta);
        var valor = DecisionText.FormatBrl(context.ValorOperacao);

        if (isolada is not null
            && context.ValorOperacao > isolada.LimiteValue
            && conjunta is not null
            && !ModeSatisfied(conjunta, cargos))
        {
            return $"Valor R$ {valor} ultrapassa o limite isolada de R$ {DecisionText.FormatBrl(isolada.LimiteValue)} e a combinação de signatários não cobre o modo conjunta (Regra {DecisionRuleCatalog.Rn02} + {DecisionRuleCatalog.Rn03} v{DecisionRuleCatalog.RulesVersion}).";
        }

        if (conjunta is not null
            && ModeSatisfied(conjunta, cargos)
            && context.ValorOperacao > conjunta.LimiteValue)
        {
            return $"Valor R$ {valor} ultrapassa o limite conjunta de R$ {DecisionText.FormatBrl(conjunta.LimiteValue)} (Regra {DecisionRuleCatalog.Rn03} v{DecisionRuleCatalog.RulesVersion}).";
        }

        return $"Combinação de signatários ou valor não atende ao modo de assinatura da operação (Regra {DecisionRuleCatalog.Rn02} + {DecisionRuleCatalog.Rn03} v{DecisionRuleCatalog.RulesVersion}).";
    }

    private static IReadOnlyList<DecisionEvidence> EvidenciasFrom(IEnumerable<Power> powers)
        => powers
            .Where(p => p.SourcePage > 0 || !string.IsNullOrWhiteSpace(p.SourceSnippet))
            .GroupBy(p => p.PowerId)
            .Select(g => g.First())
            .Select(p => new DecisionEvidence(
                DecisionEvidence.TypeDocumento,
                new DecisionSourceTrace(p.SourcePage, p.SourceOffsetStart, p.SourceOffsetEnd, p.SourceSnippet),
                Fonte: null,
                Detalhe: $"Cláusula de poderes ({p.PowerId})"))
            .ToArray();

    private static DecisionEvaluationResult Reprovado(
        IReadOnlyList<string> motivos,
        IReadOnlyList<DecisionEvidence> evidencias)
        => new(DecisionStatus.REPROVADO, motivos, evidencias, null);
}
