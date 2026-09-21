using BbfFirmasPoderes.Domain.Entities;
using BbfFirmasPoderes.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BbfFirmasPoderes.Infrastructure.Persistence.Seed;

/// <summary>
/// Seed WF-20: espelha <c>src/lib/mocks.ts</c> docs 004 (REPROVADO) e 003 (MANUAL).
/// ACME (<c>doc_001</c>) permanece em <see cref="AcmeSeed"/>.
/// </summary>
internal static class DemoSeed
{
    public const string ReprovadoDocumentId = "doc_004";
    public const string ReprovadoDecisionId = "dec_002";
    public const string ReprovadoCorrelationId = "corr_d4e5f6";
    public const string ReprovadoPersonId = "p4";

    public const string ManualDocumentId = "doc_003";
    public const string ManualDecisionId = "dec_003";
    public const string ManualCorrelationId = "corr_g7h8i9";
    public const string ManualPersonId = "p5";

    private static readonly DateTimeOffset DeltaUploadedAt = new(2026, 4, 28, 17, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DeltaEvaluatedAt = new(2026, 4, 28, 17, 33, 14, TimeSpan.Zero);
    private static readonly DateTimeOffset GamaUploadedAt = new(2026, 4, 29, 9, 48, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset GamaEvaluatedAt = new(2026, 4, 29, 9, 51, 8, TimeSpan.Zero);
    private static readonly DateOnly DeltaVigenciaFrom = new(2024, 1, 1);
    private static readonly DateOnly DeltaVigenciaTo = new(2025, 8, 30);

    private const string DeltaEvidenciasJson =
        """
        [{"type":"fonte_oficial","fonte":"Junta Comercial SP","detalhe":"Status ATIVO=false desde 2025-09-12"},{"type":"documento","trace":{"page":2,"offsetStart":320,"offsetEnd":480,"snippet":"...procuração com validade até 30/08/2025..."},"detalhe":"Cláusula 3 da Procuração"}]
        """;

    private const string GamaEvidenciasJson =
        """
        [{"type":"documento","trace":{"page":3,"offsetStart":800,"offsetEnd":1100,"snippet":"...os procuradores poderão, em conjunto ou isoladamente conforme deliberação..."},"detalhe":"Cláusula ambígua — modo de assinatura não determinístico"}]
        """;

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>().HasData(
            new Document
            {
                DocumentId = ReprovadoDocumentId,
                FileName = "contrato-delta.pdf",
                Cnpj = "11.222.333/0001-44",
                RazaoSocial = "Delta EIRELI",
                TipoSocietario = "EIRELI",
                UploadedAt = DeltaUploadedAt,
                UploadedBy = "joao.t@bbf.com.br",
                Status = DocStatus.decidido,
                FileHash = "d4e5...0f1a",
                Paginas = 6,
                ConfiancaOcr = 0.96m,
                ConfiancaIagen = 0.93m,
                ConfiancaNer = 0.95m,
                CorrelationId = ReprovadoCorrelationId
            },
            new Document
            {
                DocumentId = ManualDocumentId,
                FileName = "procuracao-gama.pdf",
                Cnpj = "55.666.777/0001-22",
                RazaoSocial = "Gama Investimentos LTDA",
                TipoSocietario = "LTDA",
                UploadedAt = GamaUploadedAt,
                UploadedBy = "ana.silva@bbf.com.br",
                Status = DocStatus.revisao_humana,
                FileHash = "c1d2...8e9f",
                Paginas = 4,
                ConfiancaOcr = 0.78m,
                ConfiancaIagen = 0.71m,
                ConfiancaNer = 0.66m,
                CorrelationId = ManualCorrelationId
            });

        modelBuilder.Entity<Person>().HasData(
            new Person
            {
                PersonId = ReprovadoPersonId,
                DocumentId = ReprovadoDocumentId,
                Nome = "Carlos Pereira",
                Cpf = "333.444.***-66",
                Qualificacao = "Titular",
                Cargo = "Procurador",
                Status = PersonStatus.inativo
            },
            new Person
            {
                PersonId = ManualPersonId,
                DocumentId = ManualDocumentId,
                Nome = "Pedro Henrique",
                Cpf = "444.555.***-77",
                Qualificacao = "Procurador",
                Cargo = "Procurador",
                Status = PersonStatus.ativo
            });

        modelBuilder.Entity<Power>().HasData(new Power
        {
            PowerId = "pw_delta_1",
            DocumentId = ReprovadoDocumentId,
            Pessoa = "Titular",
            Operacao = "Contratação de crédito",
            LimiteCurrency = "BRL",
            LimiteValue = 200000m,
            LimiteExpression = "até R$ 200.000,00",
            ModoAssinaturaTipo = SignatureModeType.isolada,
            VigenciaFrom = DeltaVigenciaFrom,
            VigenciaTo = DeltaVigenciaTo,
            SourcePage = 2,
            SourceOffsetStart = 320,
            SourceOffsetEnd = 480,
            SourceSnippet = "...procuração com validade até 30/08/2025..."
        });

        modelBuilder.Entity<Decision>().HasData(
            new Decision
            {
                DecisionId = ReprovadoDecisionId,
                DocumentId = ReprovadoDocumentId,
                Cnpj = "11.222.333/0001-44",
                Operacao = "Contratação de crédito — R$ 200.000",
                SignatariosSolicitados = ["Carlos Pereira (Procurador)"],
                Status = DecisionStatus.REPROVADO,
                Motivos =
                [
                    "Sócio Carlos Pereira consta como INATIVO na Junta Comercial desde 2025-09-12 (Regra RN01 v1.2.0).",
                    "Procuração apresentada está revogada (validTo 2025-08-30)."
                ],
                EvidenciasJson = DeltaEvidenciasJson,
                VersionRules = "1.2.0",
                VersionCanonical = "1.0.0",
                VersionAiPrompt = "leitura-contrato-social@2.1.0",
                VersionAiModel = "gemini-1.5-pro",
                EvaluatedAt = DeltaEvaluatedAt,
                LatencyMs = 1542
            },
            new Decision
            {
                DecisionId = ManualDecisionId,
                DocumentId = ManualDocumentId,
                Cnpj = "55.666.777/0001-22",
                Operacao = "Abertura de conta",
                SignatariosSolicitados = ["Pedro Henrique (Procurador)"],
                Status = DecisionStatus.MANUAL,
                Motivos =
                [
                    "Confiança da extração NER abaixo do threshold (66% < 75%) — Regra de Threshold v1.0.0.",
                    "Cláusula de poderes ambígua na página 3 — recomenda revisão jurídica."
                ],
                EvidenciasJson = GamaEvidenciasJson,
                VersionRules = "1.2.0",
                VersionCanonical = "1.0.0",
                VersionAiPrompt = "leitura-contrato-social@2.1.0",
                VersionAiModel = "gemini-1.5-pro",
                EvaluatedAt = GamaEvaluatedAt,
                LatencyMs = 1102
            });

        modelBuilder.Entity<AuditEvent>().HasData(
            new AuditEvent
            {
                EventId = "ev_0008",
                CorrelationId = ReprovadoCorrelationId,
                DocumentId = ReprovadoDocumentId,
                DecisionId = ReprovadoDecisionId,
                Type = "decision.evaluated",
                Actor = "system",
                OccurredAt = DeltaEvaluatedAt,
                Details = "Decisão REPROVADO (sócio inativo)"
            },
            new AuditEvent
            {
                EventId = "ev_0009",
                CorrelationId = ManualCorrelationId,
                DocumentId = ManualDocumentId,
                DecisionId = ManualDecisionId,
                Type = "decision.evaluated",
                Actor = "system",
                OccurredAt = GamaEvaluatedAt,
                Details = "Decisão MANUAL (baixa confiança)"
            });
    }
}
