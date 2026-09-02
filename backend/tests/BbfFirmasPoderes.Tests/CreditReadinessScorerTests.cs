using System.Text.Json;
using BbfFirmasPoderes.Domain.Canonical;

namespace BbfFirmasPoderes.Tests;

public class CreditReadinessScorerTests
{
    [Fact]
    public void Calculate_CompleteConsistentAuthority_ReturnsHighScore()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "document": { "truncated": false, "warnings": [], "skipped_pages": [] },
              "company": { "cnpj": "12.345.678/0001-90", "legal_name": "ACME LTDA" },
              "people": [{ "name": "Ana", "citations": [{ "page": 1 }] }],
              "powers": [{ "text": "Assinar", "citations": [{ "page": 2 }] }],
              "signatures": [{ "page": 3 }],
              "validations": [{ "status": "pass" }],
              "risks": [],
              "authority_decision": "allow"
            }
            """);

        var result = CreditReadinessScorer.Calculate(json.RootElement);

        Assert.Equal(100, result.Score);
        Assert.Equal("alto", result.Classification);
        Assert.Equal("aprovado", result.Recommendation);
        Assert.Empty(result.CriticalBlockers);
        Assert.Contains("Score 100/100", result.Justification);
    }

    [Fact]
    public void Calculate_CriticalAuthorityFailure_CapsAndRejects()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "company": { "cnpj": "12.345.678/0001-90", "legal_name": "ACME LTDA" },
              "people": [{ "name": "Ana" }],
              "powers": [{ "text": "Sem poderes para a operação" }],
              "authority_decision": "deny",
              "authority_reason": "Representação inválida e mandato vencido."
            }
            """);

        var result = CreditReadinessScorer.Calculate(json.RootElement);

        Assert.True(result.Score <= 49);
        Assert.Equal("baixo", result.Classification);
        Assert.Equal("reprovado", result.Recommendation);
        Assert.Contains("representacao_invalida", result.CriticalBlockers);
        Assert.Contains("mandato_vencido", result.CriticalBlockers);
        Assert.Contains("poderes_ausentes", result.CriticalBlockers);
    }

    [Fact]
    public void Normalize_CurrentKaasEnvelope_ProducesStableContractAndScore()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "result": {
                "output": {
                  "verdict": "manual_analysis",
                  "document_analysis": {
                    "corpus": { "mime_type": "application/pdf", "page_count": 4, "warnings": [] },
                    "signatures": [{ "page": 4 }]
                  },
                  "powers_extraction": {
                    "envelope": {
                      "grantor": { "cnpj": "12.345.678/0001-90", "legal_name": "ACME LTDA" },
                      "representatives": [{ "name": "Ana", "citations": [{ "page": 1 }] }],
                      "powers": [{ "text": "Administração", "citations": [{ "page": 2 }] }]
                    }
                  },
                  "authority_validation": {
                    "decision": "review",
                    "reason": "Revisão cadastral necessária.",
                    "checks": [{ "status": "pass" }],
                    "conflicts": []
                  }
                }
              }
            }
            """);

        var normalized = KaasAnalysisNormalizer.Normalize(json.RootElement);
        using var result = JsonDocument.Parse(normalized.AnalysisJson);

        Assert.Equal("12.345.678/0001-90", result.RootElement.GetProperty("company").GetProperty("cnpj").GetString());
        Assert.Single(result.RootElement.GetProperty("people").EnumerateArray());
        Assert.Single(result.RootElement.GetProperty("powers").EnumerateArray());
        Assert.True(result.RootElement.GetProperty("score").GetInt32() > 0);
        Assert.Equal("revisao_manual", result.RootElement.GetProperty("recommendation").GetString());
        Assert.False(string.IsNullOrWhiteSpace(result.RootElement.GetProperty("score_justification").GetString()));
    }
}
