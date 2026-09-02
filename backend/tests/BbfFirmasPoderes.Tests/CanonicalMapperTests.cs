using BbfFirmasPoderes.Domain.Canonical;
using BbfFirmasPoderes.Domain.Enums;

namespace BbfFirmasPoderes.Tests;

public class CanonicalMapperTests
{
    public const string AcmeCanonicalJson =
        """
        {
          "documentId": "doc_001",
          "cnpj": "12.345.678/0001-90",
          "razaoSocial": "ACME Indústrias LTDA",
          "pessoas": [
            { "personId": "p1", "nome": "João da Silva", "cpf": "111.222.***-44", "qualificacao": "Sócio-administrador", "cargo": "Diretor", "status": "ativo" },
            { "personId": "p2", "nome": "Maria Souza", "cpf": "222.333.***-55", "qualificacao": "Sócia", "cargo": "Procuradora", "status": "ativo" },
            { "personId": "p3", "nome": "Carlos Pereira", "cpf": "333.444.***-66", "qualificacao": "Sócio", "cargo": "Conselheiro", "status": "inativo" }
          ],
          "poderes": [
            {
              "powerId": "pw1",
              "pessoa": "Diretor",
              "operacao": "Movimentação financeira",
              "limite": { "currency": "BRL", "value": 500000, "expression": "até R$ 500.000,00" },
              "modoAssinatura": { "tipo": "isolada" },
              "vigencia": { "validFrom": "2024-01-01" },
              "sourceTrace": { "page": 4, "offsetStart": 1280, "offsetEnd": 1480, "snippet": "...o Diretor poderá assinar isoladamente movimentações até R$ 500.000,00..." }
            },
            {
              "powerId": "pw2",
              "pessoa": "Diretor + Procurador",
              "operacao": "Movimentação financeira",
              "limite": { "currency": "BRL", "value": 5000000, "expression": "acima de R$ 500.000,00" },
              "modoAssinatura": { "tipo": "conjunta", "n": 2, "m": 2, "qualificacoes": ["Diretor", "Procurador"] },
              "vigencia": { "validFrom": "2024-01-01" },
              "sourceTrace": { "page": 4, "offsetStart": 1500, "offsetEnd": 1750, "snippet": "...acima desse valor, exigir-se-á assinatura conjunta de Diretor e Procurador..." }
            }
          ]
        }
        """;

    [Fact]
    public void TryMap_AcmeSchema_Returns3PeopleAnd2Powers_WithSourceTrace()
    {
        var mapping = CanonicalMapper.TryMap(AcmeCanonicalJson, "doc_001");

        Assert.True(mapping.Structured);
        Assert.Equal("12.345.678/0001-90", mapping.Cnpj);
        Assert.Equal("ACME Indústrias LTDA", mapping.RazaoSocial);
        Assert.Equal("LTDA", mapping.TipoSocietario);
        Assert.Equal(3, mapping.People.Count);
        Assert.Equal(2, mapping.Powers.Count);

        Assert.Equal(["João da Silva", "Maria Souza", "Carlos Pereira"], mapping.People.Select(p => p.Nome).ToArray());
        Assert.Equal(PersonStatus.inativo, mapping.People[2].Status);
        Assert.Equal("111.222.***-44", mapping.People[0].Cpf);

        var isolada = mapping.Powers[0];
        Assert.Equal(SignatureModeType.isolada, isolada.ModoAssinaturaTipo);
        Assert.Equal(4, isolada.SourcePage);
        Assert.Equal(1280, isolada.SourceOffsetStart);
        Assert.Equal(1480, isolada.SourceOffsetEnd);
        Assert.Contains("Diretor poderá assinar isoladamente", isolada.SourceSnippet);

        var conjunta = mapping.Powers[1];
        Assert.Equal(SignatureModeType.conjunta, conjunta.ModoAssinaturaTipo);
        Assert.Equal(2, conjunta.ModoAssinaturaN);
        Assert.Equal(["Diretor", "Procurador"], conjunta.ModoAssinaturaQualificacoes ?? []);
        Assert.Equal(5000000m, conjunta.LimiteValue);
        Assert.Contains("assinatura conjunta", conjunta.SourceSnippet);
    }

    [Fact]
    public void TryMap_SociosAliasNestedInBody_IsStructured()
    {
        var json =
            """
            {"ok":true,"body":{"socios":[{"nome":"Ana","cpf":"111.222.333-44","qualificacao":"Sócia","cargo":"Diretora","status":"ativo"}],"poderes":[{"pessoa":"Diretor","operacao":"TED","limite":{"currency":"BRL","value":1,"expression":"R$ 1"},"modoAssinatura":{"tipo":"isolada"},"sourceTrace":{"page":1,"offsetStart":0,"offsetEnd":10,"snippet":"trecho"}}]}}
            """;

        var mapping = CanonicalMapper.TryMap(json, "doc_x");

        Assert.True(mapping.Structured);
        Assert.Single(mapping.People);
        Assert.Equal("111.222.***-44", mapping.People[0].Cpf);
        Assert.Equal("doc_x:p1", mapping.People[0].PersonId);
        Assert.Single(mapping.Powers);
        Assert.Equal(1, mapping.Powers[0].SourcePage);
    }

    [Fact]
    public void TryMap_LongKaasText_ClampsToColumnLimits()
    {
        var longText = new string('a', 900);
        var longName = new string('b', 400);
        var longId = new string('c', 120);
        var longSnippet = new string('d', 4000);
        var json = "{\"pessoas\":[{\"personId\":\"" + longId + "\",\"nome\":\"" + longName
            + "\",\"cargo\":\"" + longText + "\",\"qualificacao\":\"" + longText
            + "\",\"rg\":\"" + longText + "\"}],\"poderes\":[{\"powerId\":\"" + longId
            + "\",\"pessoa\":\"" + longName + "\",\"text\":\"" + longText
            + "\",\"restrictions\":[\"" + longText + "\"],\"limite\":{\"currency\":\"Reais\",\"value\":1},"
            + "\"sourceTrace\":{\"page\":1,\"offsetStart\":0,\"offsetEnd\":1,\"snippet\":\"" + longSnippet + "\"}}]}";

        var mapping = CanonicalMapper.TryMap(json, "doc_clamp");

        Assert.True(mapping.Structured);

        var person = Assert.Single(mapping.People);
        Assert.True(person.PersonId.Length <= 64);
        Assert.True(person.Nome.Length <= 256);
        Assert.True(person.Cargo.Length <= 128);
        Assert.True(person.Qualificacao.Length <= 128);
        Assert.True(person.Rg.Length <= 64);

        var power = Assert.Single(mapping.Powers);
        Assert.True(power.PowerId.Length <= 64);
        Assert.True(power.Pessoa.Length <= 256);
        Assert.True(power.Operacao.Length <= 256);
        Assert.True(power.LimiteExpression.Length <= 256);
        Assert.True(power.LimiteCurrency.Length <= 3);
        Assert.True(power.SourceSnippet.Length <= 1024);
        Assert.EndsWith("…", power.Operacao);
    }

    [Fact]
    public void TryMap_UnstructuredPayload_IsNotStructured()
    {
        var mapping = CanonicalMapper.TryMap("""{"executionId":"exec_1","status":"canonico_pronto","ok":true}""", "doc_x");

        Assert.False(mapping.Structured);
        Assert.Empty(mapping.People);
        Assert.Empty(mapping.Powers);
    }

    [Fact]
    public void TryMap_InvalidJson_IsNotStructured()
    {
        Assert.False(CanonicalMapper.TryMap("not-json", "doc_x").Structured);
        Assert.False(CanonicalMapper.TryMap(null, "doc_x").Structured);
    }

    [Fact]
    public void TryMap_PowersExtractionEnvelope_MapsRepresentativesAndPowers()
    {
        var json =
            """
            {
              "execution_id": "e27",
              "status": "done",
              "verdict": "manual_analysis",
              "result": {
                "output": {
                  "powers_extraction": {
                    "envelope": {
                      "grantor": { "cnpj": "•••", "legal_name": "Th••••DA" },
                      "representatives": [
                        {
                          "name": "Ed••••do",
                          "role": "Sócio administrador",
                          "cpf": "347.915.208-63",
                          "rg": "45.678.123-4 SSP/SP",
                          "person_type": "pf",
                          "quotas": 60000,
                          "mandate_start": "2026-04-13",
                          "citations": [
                            { "page": 1, "field": "name", "excerpt": "Sócio 1 (Pessoa Física): Eduardo Henrique Figueiredo, brasileiro" }
                          ]
                        }
                      ],
                      "powers": [
                        {
                          "text": "Administração geral da sociedade",
                          "granted_to": ["Eduardo Henrique Figueiredo"],
                          "restrictions": ["Cláusula 8º"],
                          "citations": [{ "page": 2, "excerpt": "administração da sociedade será exercida" }]
                        }
                      ]
                    }
                  },
                  "document_analysis": {
                    "corpus": { "raw_text": "CNPJ 12.631.063/0001-25" }
                  }
                }
              }
            }
            """;

        var mapping = CanonicalMapper.TryMap(json, "doc_live");

        Assert.True(mapping.Structured);
        Assert.Equal("12.631.063/0001-25", mapping.Cnpj);
        Assert.Equal("Th••••DA", mapping.RazaoSocial);
        Assert.Single(mapping.People);
        Assert.Equal("Eduardo Henrique Figueiredo", mapping.People[0].Nome);
        Assert.Equal("Sócio administrador", mapping.People[0].Cargo);
        Assert.Equal("347.915.***-63", mapping.People[0].Documento);
        Assert.Equal("45.678.123-4 SSP/SP", mapping.People[0].Rg);
        Assert.Equal("pf", mapping.People[0].PersonType);
        Assert.Equal(60000m, mapping.People[0].Quotas);
        Assert.Equal(new DateOnly(2026, 4, 13), mapping.People[0].MandateStart);
        Assert.Single(mapping.Powers);
        Assert.Equal("Eduardo Henrique Figueiredo", mapping.Powers[0].Pessoa);
        Assert.Equal("Administração geral da sociedade", mapping.Powers[0].Operacao);
        Assert.Equal(2, mapping.Powers[0].SourcePage);
        Assert.Contains("administração da sociedade", mapping.Powers[0].SourceSnippet);
        Assert.NotNull(mapping.CreditReadiness);
        Assert.InRange(mapping.CreditReadiness!.Score, 0, 100);
        Assert.False(string.IsNullOrWhiteSpace(mapping.CreditReadiness.Justification));
        Assert.Contains("\"people\"", mapping.AnalysisJson);
        Assert.Contains("\"powers\"", mapping.AnalysisJson);
    }

    [Theory]
    [InlineData(
        "12.345.678/0001-90",
        "ACME Indústrias LTDA",
        "LTDA")]
    [InlineData(
        "11.222.333/0001-44",
        "Delta EIRELI",
        "EIRELI")]
    [InlineData(
        "55.666.777/0001-22",
        "Gama Investimentos LTDA",
        "LTDA")]
    public void TryMap_CanonicalCases_ExtractsCnpjRazaoAndTipo(string cnpj, string razao, string tipo)
    {
        var json =
            $$"""
            {
              "cnpj": "{{cnpj}}",
              "razaoSocial": "{{razao}}",
              "pessoas": [{ "nome": "Sócio Teste", "cpf": "111.222.333-44", "status": "ativo" }],
              "poderes": [{ "pessoa": "Diretor", "operacao": "Administração", "limite": { "currency": "BRL", "value": 1, "expression": "R$ 1" }, "modoAssinatura": { "tipo": "isolada" }, "sourceTrace": { "page": 1, "offsetStart": 0, "offsetEnd": 10, "snippet": "trecho" } }]
            }
            """;

        var mapping = CanonicalMapper.TryMap(json, "doc_case");

        Assert.True(mapping.Structured);
        Assert.Equal(cnpj, mapping.Cnpj);
        Assert.Equal(razao, mapping.RazaoSocial);
        Assert.Equal(tipo, mapping.TipoSocietario);
    }

    [Fact]
    public void InferTipoSocietario_FromRazaoAndExplicitField()
    {
        Assert.Equal("LTDA", CanonicalMapper.InferTipoSocietario("ACME Indústrias LTDA"));
        Assert.Equal("EIRELI", CanonicalMapper.InferTipoSocietario("Delta EIRELI"));
        Assert.Equal("S.A.", CanonicalMapper.InferTipoSocietario("Beta Comercial S.A."));
        Assert.Equal("S.A.", CanonicalMapper.InferTipoSocietario("Zeta Logística", "S.A."));
        Assert.Null(CanonicalMapper.InferTipoSocietario("Empresa Sem Tipo"));
        Assert.Null(CanonicalMapper.InferTipoSocietario(null, "$.payload.company.type"));
    }

    [Theory]
    [InlineData("e emissão de notas fiscais cujos valores ultrapassem", false)]
    [InlineData("emissão de notas fiscais", false)]
    [InlineData("ACME Indústrias LTDA", true)]
    [InlineData("Theo e Heloise", true)]
    [InlineData("Th••••DA", true)]
    public void LooksLikeLegalName_RejectsClauseFragments(string value, bool expected)
    {
        Assert.Equal(expected, CanonicalMapper.LooksLikeLegalName(value));
    }

    [Fact]
    public void TryMap_ClauseAsRazaoSocial_IsIgnored()
    {
        var json =
            """
            {
              "cnpj": "12.345.678/0001-90",
              "razaoSocial": "e emissão de notas fiscais cujos valores ultrapassem R$ 10.000,00",
              "pessoas": [{ "nome": "Sócio Teste", "cpf": "111.222.333-44", "status": "ativo" }],
              "poderes": [{ "pessoa": "Diretor", "operacao": "Administração", "limite": { "currency": "BRL", "value": 1, "expression": "R$ 1" }, "modoAssinatura": { "tipo": "isolada" }, "sourceTrace": { "page": 1, "offsetStart": 0, "offsetEnd": 10, "snippet": "trecho" } }]
            }
            """;

        var mapping = CanonicalMapper.TryMap(json, "doc_clause");

        Assert.True(mapping.Structured);
        Assert.Null(mapping.RazaoSocial);
    }
}
