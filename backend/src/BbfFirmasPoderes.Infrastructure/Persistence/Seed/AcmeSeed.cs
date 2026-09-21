using BbfFirmasPoderes.Domain.Entities;
using BbfFirmasPoderes.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BbfFirmasPoderes.Infrastructure.Persistence.Seed;

/// <summary>
/// Seed WF-04: documento ACME = <c>doc_001</c> de <c>src/lib/mocks.ts</c> (APROVADO).
/// Docs 003/004 (MANUAL/REPROVADO) em <see cref="DemoSeed"/>.
/// </summary>
internal static class AcmeSeed
{
    public const string DocumentId = "doc_001";
    public const string DecisionId = "dec_001";
    public const string CorrelationId = "corr_a1b2c3";
    public static readonly Guid KasRunId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-000000000001");

    private static readonly DateTimeOffset UploadedAt = new(2026, 4, 29, 14, 22, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset EvaluatedAt = new(2026, 4, 29, 14, 25, 32, TimeSpan.Zero);
    private static readonly DateOnly VigenciaFrom = new(2024, 1, 1);

    private const string EvidenciasJson =
        """
        [{"type":"documento","trace":{"page":4,"offsetStart":1500,"offsetEnd":1750,"snippet":"...acima desse valor, exigir-se-á assinatura conjunta de Diretor e Procurador..."},"detalhe":"Cláusula 8 do Contrato Social"},{"type":"fonte_oficial","fonte":"Junta Comercial SP","detalhe":"Quadro societário consultado em 2026-04-29; ambos os sócios constam como ATIVOS."}]
        """;

    private const string KasPayloadJson =
        """
        {"ok":true,"kasStatus":200,"action":"ingest","correlationId":"corr_a1b2c3","executionId":"exec_acme_001","fileName":"contrato-social-acme.pdf","journey":"testes-firmas-e-poderes","mode":"sync","body":{"status":"completed","message":"seed ACME doc_001"}}
        """;

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>().HasData(new Document
        {
            DocumentId = DocumentId,
            FileName = "contrato-social-acme.pdf",
            Cnpj = "12.345.678/0001-90",
            RazaoSocial = "ACME Indústrias LTDA",
            TipoSocietario = "LTDA",
            UploadedAt = UploadedAt,
            UploadedBy = "ana.silva@bbf.com.br",
            Status = DocStatus.decidido,
            FileHash = "a3f2c4...e9d1",
            Paginas = 12,
            ConfiancaOcr = 0.97m,
            ConfiancaIagen = 0.92m,
            ConfiancaNer = 0.94m,
            CorrelationId = CorrelationId
        });

        modelBuilder.Entity<Person>().HasData(
            new Person
            {
                PersonId = "p1",
                DocumentId = DocumentId,
                Nome = "João da Silva",
                Cpf = "111.222.***-44",
                Qualificacao = "Sócio-administrador",
                Cargo = "Diretor",
                Status = PersonStatus.ativo
            },
            new Person
            {
                PersonId = "p2",
                DocumentId = DocumentId,
                Nome = "Maria Souza",
                Cpf = "222.333.***-55",
                Qualificacao = "Sócia",
                Cargo = "Procuradora",
                Status = PersonStatus.ativo
            },
            new Person
            {
                PersonId = "p3",
                DocumentId = DocumentId,
                Nome = "Carlos Pereira",
                Cpf = "333.444.***-66",
                Qualificacao = "Sócio",
                Cargo = "Conselheiro",
                Status = PersonStatus.inativo
            });

        modelBuilder.Entity<Power>().HasData(
            new Power
            {
                PowerId = "pw1",
                DocumentId = DocumentId,
                Pessoa = "Diretor",
                Operacao = "Movimentação financeira",
                LimiteCurrency = "BRL",
                LimiteValue = 500000m,
                LimiteExpression = "até R$ 500.000,00",
                ModoAssinaturaTipo = SignatureModeType.isolada,
                VigenciaFrom = VigenciaFrom,
                SourcePage = 4,
                SourceOffsetStart = 1280,
                SourceOffsetEnd = 1480,
                SourceSnippet = "...o Diretor poderá assinar isoladamente movimentações até R$ 500.000,00..."
            },
            new Power
            {
                PowerId = "pw2",
                DocumentId = DocumentId,
                Pessoa = "Diretor + Procurador",
                Operacao = "Movimentação financeira",
                LimiteCurrency = "BRL",
                LimiteValue = 5000000m,
                LimiteExpression = "acima de R$ 500.000,00",
                ModoAssinaturaTipo = SignatureModeType.conjunta,
                ModoAssinaturaN = 2,
                ModoAssinaturaM = 2,
                ModoAssinaturaQualificacoes = ["Diretor", "Procurador"],
                VigenciaFrom = VigenciaFrom,
                SourcePage = 4,
                SourceOffsetStart = 1500,
                SourceOffsetEnd = 1750,
                SourceSnippet = "...acima desse valor, exigir-se-á assinatura conjunta de Diretor e Procurador..."
            });

        modelBuilder.Entity<Decision>().HasData(new Decision
        {
            DecisionId = DecisionId,
            DocumentId = DocumentId,
            Cnpj = "12.345.678/0001-90",
            Operacao = "Movimentação financeira — R$ 500.000",
            SignatariosSolicitados = ["João da Silva (Diretor)", "Maria Souza (Procuradora)"],
            Status = DecisionStatus.APROVADO,
            Motivos =
            [
                "Operação dentro do limite de R$ 500.000 com modo conjunto Diretor + Procurador (Regra RN02 + RN03 v1.2.0).",
                "Sócios João da Silva e Maria Souza confirmados como ATIVOS na Junta Comercial em 2026-04-29 14:22 UTC.",
                "Procuração vigente (validFrom 2024-01-01, sem revogação)."
            ],
            EvidenciasJson = EvidenciasJson,
            VersionRules = "1.2.0",
            VersionCanonical = "1.0.0",
            VersionAiPrompt = "leitura-contrato-social@2.1.0",
            VersionAiModel = "gemini-1.5-pro",
            EvaluatedAt = EvaluatedAt,
            LatencyMs = 1287
        });

        modelBuilder.Entity<AuditEvent>().HasData(new AuditEvent
        {
            EventId = "ev_0001",
            CorrelationId = CorrelationId,
            DocumentId = DocumentId,
            Type = "document.uploaded",
            Actor = "ana.silva@bbf.com.br",
            OccurredAt = UploadedAt,
            Details = "Upload de contrato-social-acme.pdf (12 páginas, 2.3MB)"
        });

        modelBuilder.Entity<KasRun>().HasData(new KasRun
        {
            KasRunId = KasRunId,
            CorrelationId = CorrelationId,
            DocumentId = DocumentId,
            ExecutionId = "exec_acme_001",
            Action = KasRunAction.ingest,
            FileName = "contrato-social-acme.pdf",
            HttpStatus = 200,
            Ok = true,
            OccurredAt = UploadedAt,
            DurationMs = 14200,
            PayloadJson = KasPayloadJson
        });
    }
}
