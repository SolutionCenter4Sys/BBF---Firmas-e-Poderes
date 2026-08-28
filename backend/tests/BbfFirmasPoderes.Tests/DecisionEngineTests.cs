using BbfFirmasPoderes.Domain.Decisioning;
using BbfFirmasPoderes.Domain.Entities;
using BbfFirmasPoderes.Domain.Enums;

namespace BbfFirmasPoderes.Tests;

public class DecisionEngineTests
{
    private static readonly DateTimeOffset AsOf = new(2026, 4, 29, 14, 25, 0, TimeSpan.Zero);

    [Fact]
    public void Acme_Diretor_500k_Aprovado_Isolada()
    {
        var result = DecisionEngine.Evaluate(Context(500_000m, ["João da Silva (Diretor)"]));

        Assert.Equal(DecisionStatus.APROVADO, result.Status);
        Assert.Equal("pw1", result.MatchedPowerId);
        Assert.Contains(result.Motivos, m => m.Contains("RN02") && m.Contains("RN03") && m.Contains("isolada"));
        Assert.Contains(result.Motivos, m => m.Contains("RN01") && m.Contains("ATIVO"));
        Assert.Contains(result.Motivos, m => m.Contains("RN04"));
        var evidence = Assert.Single(result.Evidencias);
        Assert.Equal(DecisionEvidence.TypeDocumento, evidence.Type);
        Assert.Equal(4, evidence.Trace!.Page);
        Assert.Equal(1280, evidence.Trace.OffsetStart);
        Assert.Equal(1480, evidence.Trace.OffsetEnd);
        Assert.Contains("isoladamente", evidence.Trace.Snippet);
    }

    [Fact]
    public void Acme_CargoDiretor_500k_Aprovado()
    {
        var result = DecisionEngine.Evaluate(Context(500_000m, ["Diretor"]));

        Assert.Equal(DecisionStatus.APROVADO, result.Status);
        Assert.Equal("pw1", result.MatchedPowerId);
    }

    [Fact]
    public void Acme_PersonIdP1_500k_Aprovado()
    {
        var result = DecisionEngine.Evaluate(Context(500_000m, ["p1"]));

        Assert.Equal(DecisionStatus.APROVADO, result.Status);
        Assert.Equal("pw1", result.MatchedPowerId);
    }

    [Fact]
    public void Acme_Diretor_AcimaIsolada_Reprovado_SemProcurador()
    {
        var result = DecisionEngine.Evaluate(Context(500_000.01m, ["João da Silva (Diretor)"]));

        Assert.Equal(DecisionStatus.REPROVADO, result.Status);
        Assert.Null(result.MatchedPowerId);
        Assert.Contains(result.Motivos, m => m.Contains("isolada") && m.Contains("conjunta"));
    }

    [Fact]
    public void Acme_DiretorEProcuradora_600k_Aprovado_Conjunta()
    {
        var result = DecisionEngine.Evaluate(Context(
            600_000m,
            ["João da Silva (Diretor)", "Maria Souza (Procuradora)"]));

        Assert.Equal(DecisionStatus.APROVADO, result.Status);
        Assert.Equal("pw2", result.MatchedPowerId);
        Assert.Contains(result.Motivos, m => m.Contains("conjunta"));
        var evidence = Assert.Single(result.Evidencias);
        Assert.Equal(1500, evidence.Trace!.OffsetStart);
        Assert.Contains("assinatura conjunta", evidence.Trace.Snippet);
    }

    [Fact]
    public void Acme_Conjunta_AcimaLimite_Reprovado()
    {
        var result = DecisionEngine.Evaluate(Context(
            5_000_000.01m,
            ["João da Silva (Diretor)", "Maria Souza (Procuradora)"]));

        Assert.Equal(DecisionStatus.REPROVADO, result.Status);
        Assert.Contains(result.Motivos, m => m.Contains("limite conjunta"));
    }

    [Fact]
    public void Acme_CarlosInativo_Reprovado_Rn01()
    {
        var result = DecisionEngine.Evaluate(Context(100_000m, ["Carlos Pereira (Conselheiro)"]));

        Assert.Equal(DecisionStatus.REPROVADO, result.Status);
        Assert.Contains(result.Motivos, m => m.Contains("RN01") && m.Contains("INATIVO"));
    }

    [Fact]
    public void Acme_NerBaixo_Manual_Th01()
    {
        var result = DecisionEngine.Evaluate(Context(500_000m, ["João da Silva (Diretor)"], ner: 0.66m));

        Assert.Equal(DecisionStatus.MANUAL, result.Status);
        Assert.Contains(result.Motivos, m => m.Contains("TH01") && m.Contains("66%") && m.Contains("75%"));
    }

    [Fact]
    public void Acme_PoderForaDeVigencia_Reprovado_Rn04()
    {
        var asOf = new DateTimeOffset(2023, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var result = DecisionEngine.Evaluate(Context(500_000m, ["João da Silva (Diretor)"], asOf: asOf));

        Assert.Equal(DecisionStatus.REPROVADO, result.Status);
        Assert.Contains(result.Motivos, m => m.Contains("RN04") && m.Contains("vigente"));
    }

    [Fact]
    public void Acme_MesmoInput_MotivosEEvidenciasIdenticos()
    {
        var ctx = Context(500_000m, ["João da Silva (Diretor)"]);
        var a = DecisionEngine.Evaluate(ctx);
        var b = DecisionEngine.Evaluate(ctx);

        Assert.Equal(a.Status, b.Status);
        Assert.Equal(a.Motivos, b.Motivos);
        Assert.Equal(DecisionJson.SerializeEvidencias(a.Evidencias), DecisionJson.SerializeEvidencias(b.Evidencias));
        Assert.Equal(a.MatchedPowerId, b.MatchedPowerId);
    }

    private static DecisionContext Context(
        decimal valor,
        string[] signatarios,
        DateTimeOffset? asOf = null,
        decimal? ner = null)
        => new(
            DocumentId: "doc_001",
            Cnpj: "12.345.678/0001-90",
            ConfiancaOcr: 0.97m,
            ConfiancaIagen: 0.92m,
            ConfiancaNer: ner ?? 0.94m,
            People: People(),
            Powers: Powers(),
            Operacao: "movimentacao_financeira",
            ValorOperacao: valor,
            Currency: "BRL",
            SignatariosSolicitados: signatarios,
            AsOf: asOf ?? AsOf);

    private static List<Person> People() =>
    [
        new()
        {
            PersonId = "p1",
            DocumentId = "doc_001",
            Nome = "João da Silva",
            Cpf = "111.222.***-44",
            Qualificacao = "Sócio-administrador",
            Cargo = "Diretor",
            Status = PersonStatus.ativo
        },
        new()
        {
            PersonId = "p2",
            DocumentId = "doc_001",
            Nome = "Maria Souza",
            Cpf = "222.333.***-55",
            Qualificacao = "Sócia",
            Cargo = "Procuradora",
            Status = PersonStatus.ativo
        },
        new()
        {
            PersonId = "p3",
            DocumentId = "doc_001",
            Nome = "Carlos Pereira",
            Cpf = "333.444.***-66",
            Qualificacao = "Sócio",
            Cargo = "Conselheiro",
            Status = PersonStatus.inativo
        }
    ];

    private static List<Power> Powers() =>
    [
        new()
        {
            PowerId = "pw1",
            DocumentId = "doc_001",
            Pessoa = "Diretor",
            Operacao = "Movimentação financeira",
            LimiteCurrency = "BRL",
            LimiteValue = 500_000m,
            LimiteExpression = "até R$ 500.000,00",
            ModoAssinaturaTipo = SignatureModeType.isolada,
            VigenciaFrom = new DateOnly(2024, 1, 1),
            SourcePage = 4,
            SourceOffsetStart = 1280,
            SourceOffsetEnd = 1480,
            SourceSnippet = "...o Diretor poderá assinar isoladamente movimentações até R$ 500.000,00..."
        },
        new()
        {
            PowerId = "pw2",
            DocumentId = "doc_001",
            Pessoa = "Diretor + Procurador",
            Operacao = "Movimentação financeira",
            LimiteCurrency = "BRL",
            LimiteValue = 5_000_000m,
            LimiteExpression = "acima de R$ 500.000,00",
            ModoAssinaturaTipo = SignatureModeType.conjunta,
            ModoAssinaturaN = 2,
            ModoAssinaturaM = 2,
            ModoAssinaturaQualificacoes = ["Diretor", "Procurador"],
            VigenciaFrom = new DateOnly(2024, 1, 1),
            SourcePage = 4,
            SourceOffsetStart = 1500,
            SourceOffsetEnd = 1750,
            SourceSnippet = "...acima desse valor, exigir-se-á assinatura conjunta de Diretor e Procurador..."
        }
    ];
}
