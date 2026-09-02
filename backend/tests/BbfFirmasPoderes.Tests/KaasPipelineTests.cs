using System.Text;
using System.Text.Json;
using BbfFirmasPoderes.Domain.Audit;
using BbfFirmasPoderes.Domain.Correlation;
using BbfFirmasPoderes.Domain.Documents;
using BbfFirmasPoderes.Domain.Entities;
using BbfFirmasPoderes.Domain.Enums;
using BbfFirmasPoderes.Domain.Kaas;
using BbfFirmasPoderes.Infrastructure;
using BbfFirmasPoderes.Infrastructure.Kaas;
using BbfFirmasPoderes.Infrastructure.Persistence;
using BbfFirmasPoderes.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace BbfFirmasPoderes.Tests;

public sealed class KaasPipelineTests : IDisposable
{
    public const string FakeApiKey = "test-kaas-key-not-real";

    private static readonly byte[] TinyPdf =
        Encoding.ASCII.GetBytes("%PDF-1.1\n1 0 obj<</Type/Catalog>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF\n");

    private readonly WireMockServer _kaas;
    private readonly string _storageRoot;

    public KaasPipelineTests()
    {
        _storageRoot = Path.Combine(Path.GetTempPath(), "bbf-kaas-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_storageRoot);
        _kaas = WireMockServer.Start();
    }

    [Fact]
    public async Task ProcessNext_SyncJourney_PersistsKasRun_AdvancesStatus()
    {
        var path = "/kas/triggers/journeys/testes-firmas-e-poderes/run";

        _kaas.Given(Request.Create().WithPath(path).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"execution_id":"exec_wire_001","status":"done","verdict":"manual_analysis","ok":true}"""));

        await using var provider = BuildProvider();
        var (documentId, correlationId) = await SeedPendingDocumentAsync(provider);

        var processor = provider.GetRequiredService<OutboxKaasProcessor>();
        var processed = await processor.ProcessNextAsync();
        Assert.True(processed);

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var doc = await db.Documents.FindAsync(documentId);
        Assert.NotNull(doc);
        Assert.Equal(DocStatus.revisao_humana, doc!.Status);

        var run = Assert.Single(await db.KasRuns.Where(r => r.DocumentId == documentId).ToListAsync());
        Assert.Equal(KasRunAction.ingest, run.Action);
        Assert.True(run.Ok);
        Assert.Equal("exec_wire_001", run.ExecutionId);
        Assert.Contains("manual_analysis", run.PayloadJson, StringComparison.Ordinal);

        var outbox = Assert.Single(await db.OutboxMessages.Where(o => o.DocumentId == documentId).ToListAsync());
        Assert.NotNull(outbox.ProcessedAt);

        var entries = _kaas.LogEntries.ToList();
        Assert.Single(entries);

        var headers = entries[0].RequestMessage.Headers;
        Assert.NotNull(headers);
        var apiKeyHeader = headers.FirstOrDefault(h =>
            h.Key.Equals(KasDefaults.ApiKeyHeader, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(FakeApiKey, apiKeyHeader.Value);
        Assert.DoesNotContain("railway.app", entries[0].RequestMessage.AbsoluteUrl, StringComparison.OrdinalIgnoreCase);

        var contentType = headers.FirstOrDefault(h =>
            h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            "multipart/form-data",
            string.Join(";", contentType.Value),
            StringComparison.OrdinalIgnoreCase);

        var rawBody = entries[0].RequestMessage.Body ?? string.Empty;
        Assert.Contains("name=mode", rawBody, StringComparison.Ordinal);
        Assert.Contains("sync", rawBody, StringComparison.Ordinal);
        Assert.Contains("name=payload", rawBody, StringComparison.Ordinal);
        Assert.Contains("application/json", rawBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name=document_url", rawBody, StringComparison.Ordinal);
        Assert.DoesNotContain("payload.document_url", rawBody, StringComparison.Ordinal);
        Assert.Contains("filename=contrato-social.pdf", rawBody, StringComparison.Ordinal);
        Assert.Contains("application/pdf", rawBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("base64", rawBody, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(correlationId, (await db.Documents.FindAsync(documentId))!.CorrelationId);

        Assert.Empty(await db.People.Where(p => p.DocumentId == documentId).ToListAsync());
        Assert.Empty(await db.Powers.Where(p => p.DocumentId == documentId).ToListAsync());

        var types = await db.AuditEvents.Where(e => e.DocumentId == documentId).Select(e => e.Type).ToListAsync();
        Assert.Contains(AuditEventTypes.KasIngest, types);
        Assert.Contains(AuditEventTypes.OcrCompleted, types);
        Assert.Contains(AuditEventTypes.KasResult, types);
        Assert.DoesNotContain(AuditEventTypes.CanonicalReady, types);
    }

    [Fact]
    public async Task ProcessNext_StructuredKaas_PersistsAcmePeopleAndPowers_CanonicoPronto()
    {
        var path = "/kas/triggers/journeys/testes-firmas-e-poderes/run";
        var resultBody =
            """
            {"executionId":"exec_wire_acme","status":"canonico_pronto","ok":true,"documentId":"ignored","cnpj":"12.345.678/0001-90","razaoSocial":"ACME Indústrias LTDA","pessoas":[{"personId":"p1","nome":"João da Silva","cpf":"111.222.***-44","qualificacao":"Sócio-administrador","cargo":"Diretor","status":"ativo"},{"personId":"p2","nome":"Maria Souza","cpf":"222.333.***-55","qualificacao":"Sócia","cargo":"Procuradora","status":"ativo"},{"personId":"p3","nome":"Carlos Pereira","cpf":"333.444.***-66","qualificacao":"Sócio","cargo":"Conselheiro","status":"inativo"}],"poderes":[{"powerId":"pw1","pessoa":"Diretor","operacao":"Movimentação financeira","limite":{"currency":"BRL","value":500000,"expression":"até R$ 500.000,00"},"modoAssinatura":{"tipo":"isolada"},"vigencia":{"validFrom":"2024-01-01"},"sourceTrace":{"page":4,"offsetStart":1280,"offsetEnd":1480,"snippet":"...o Diretor poderá assinar isoladamente movimentações até R$ 500.000,00..."}},{"powerId":"pw2","pessoa":"Diretor + Procurador","operacao":"Movimentação financeira","limite":{"currency":"BRL","value":5000000,"expression":"acima de R$ 500.000,00"},"modoAssinatura":{"tipo":"conjunta","n":2,"m":2,"qualificacoes":["Diretor","Procurador"]},"vigencia":{"validFrom":"2024-01-01"},"sourceTrace":{"page":4,"offsetStart":1500,"offsetEnd":1750,"snippet":"...acima desse valor, exigir-se-á assinatura conjunta de Diretor e Procurador..."}}]}
            """;

        _kaas.Given(Request.Create().WithPath(path).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(resultBody));

        await using var provider = BuildProvider();
        var (documentId, _) = await SeedPendingDocumentAsync(provider);

        var processor = provider.GetRequiredService<OutboxKaasProcessor>();
        Assert.True(await processor.ProcessNextAsync());

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var doc = await db.Documents.FindAsync(documentId);
        Assert.Equal(DocStatus.canonico_pronto, doc!.Status);
        Assert.Equal("12.345.678/0001-90", doc.Cnpj);
        Assert.Equal("ACME Indústrias LTDA", doc.RazaoSocial);
        Assert.Equal("LTDA", doc.TipoSocietario);
        Assert.NotNull(doc.CreditReadinessScore);
        Assert.InRange(doc.CreditReadinessScore.Value, 0, 100);
        Assert.False(string.IsNullOrWhiteSpace(doc.CreditReadinessClassification));
        Assert.False(string.IsNullOrWhiteSpace(doc.CreditReadinessRecommendation));
        Assert.False(string.IsNullOrWhiteSpace(doc.CreditReadinessJustification));
        Assert.Contains("\"score\"", doc.AnalysisJson);
        Assert.Contains("\"score_justification\"", doc.AnalysisJson);

        var people = await db.People.Where(p => p.DocumentId == documentId).OrderBy(p => p.PersonId).ToListAsync();
        var powers = await db.Powers.Where(p => p.DocumentId == documentId).OrderBy(p => p.PowerId).ToListAsync();
        Assert.Equal(3, people.Count);
        Assert.Equal(2, powers.Count);
        Assert.Equal(["João da Silva", "Maria Souza", "Carlos Pereira"], people.Select(p => p.Nome).ToArray());
        Assert.Equal(PersonStatus.inativo, people[2].Status);

        Assert.Equal(4, powers[0].SourcePage);
        Assert.Equal(1280, powers[0].SourceOffsetStart);
        Assert.Contains("Diretor poderá assinar isoladamente", powers[0].SourceSnippet);
        Assert.Equal(SignatureModeType.conjunta, powers[1].ModoAssinaturaTipo);
        Assert.Equal(["Diretor", "Procurador"], powers[1].ModoAssinaturaQualificacoes ?? []);
        Assert.Contains("assinatura conjunta", powers[1].SourceSnippet);

        var types = await db.AuditEvents.Where(e => e.DocumentId == documentId).Select(e => e.Type).ToListAsync();
        Assert.Contains(AuditEventTypes.KasIngest, types);
        Assert.Contains(AuditEventTypes.OcrCompleted, types);
        Assert.Contains(AuditEventTypes.KasResult, types);
        Assert.Contains(AuditEventTypes.CanonicalReady, types);
    }

    [Fact]
    public async Task ProcessNext_LiveEnvelope_PersistsPeople_KeepsRevisaoHumana()
    {
        var path = "/kas/triggers/journeys/testes-firmas-e-poderes/run";
        var body =
            """
            {"execution_id":"exec_live","status":"done","verdict":"manual_analysis","result":{"output":{"powers_extraction":{"envelope":{"grantor":{"cnpj":null,"legal_name":"Theo e Heloise Joalheria LTDA"},"representatives":[{"name":"Eduardo Henrique Figueiredo","role":"Sócio administrador","person_type":"pf"}],"powers":[{"text":"Administração geral","granted_to":["Eduardo Henrique Figueiredo"],"restrictions":[],"citations":[{"page":2,"excerpt":"administrador"}]}]}},"document_analysis":{"corpus":{"page_count":4,"raw_text":"CNPJ 12.631.063/0001-25"}}}}}
            """;

        _kaas.Given(Request.Create().WithPath(path).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));

        await using var provider = BuildProvider();
        var (documentId, _) = await SeedPendingDocumentAsync(provider);

        var processor = provider.GetRequiredService<OutboxKaasProcessor>();
        Assert.True(await processor.ProcessNextAsync());

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var doc = await db.Documents.FindAsync(documentId);
        Assert.Equal(DocStatus.revisao_humana, doc!.Status);
        Assert.Equal("12.631.063/0001-25", doc.Cnpj);
        Assert.Equal("Theo e Heloise Joalheria LTDA", doc.RazaoSocial);
        Assert.Equal(4, doc.Paginas);
        Assert.Single(await db.People.Where(p => p.DocumentId == documentId).ToListAsync());
        Assert.Single(await db.Powers.Where(p => p.DocumentId == documentId).ToListAsync());
        Assert.Contains(
            AuditEventTypes.CanonicalReady,
            await db.AuditEvents.Where(e => e.DocumentId == documentId).Select(e => e.Type).ToListAsync());
    }

    [Fact]
    public async Task ProcessNext_KaasHttpError_SetsFalha_MarksOutbox()
    {
        _kaas.Given(Request.Create().UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500).WithBody("""{"error":"kaas down"}"""));

        await using var provider = BuildProvider();
        var (documentId, _) = await SeedPendingDocumentAsync(provider);

        var processor = provider.GetRequiredService<OutboxKaasProcessor>();
        Assert.True(await processor.ProcessNextAsync());

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var doc = await db.Documents.FindAsync(documentId);
        Assert.Equal(DocStatus.falha, doc!.Status);
        Assert.NotNull((await db.OutboxMessages.SingleAsync(o => o.DocumentId == documentId)).ProcessedAt);
        Assert.NotEmpty(await db.KasRuns.Where(r => r.DocumentId == documentId).ToListAsync());
        var details = await db.AuditEvents.Where(e => e.DocumentId == documentId).Select(e => e.Details).ToListAsync();
        Assert.Contains(details, d => d.Contains("kaas down", StringComparison.OrdinalIgnoreCase)
            || d.Contains("KAAS HTTP 500", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProcessNext_Kaas400DocumentUrlRoot_PersistsExactMessage()
    {
        _kaas.Given(Request.Create().UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"error":"Bad Request","message":["property document_url should not exist"],"statusCode":400}"""));

        await using var provider = BuildProvider();
        var (documentId, _) = await SeedPendingDocumentAsync(provider);

        var processor = provider.GetRequiredService<OutboxKaasProcessor>();
        Assert.True(await processor.ProcessNextAsync());

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var doc = await db.Documents.FindAsync(documentId);
        Assert.Equal(DocStatus.falha, doc!.Status);
        Assert.True(string.IsNullOrWhiteSpace(doc.Cnpj));
        Assert.True(string.IsNullOrWhiteSpace(doc.RazaoSocial));
        Assert.True(string.IsNullOrWhiteSpace(doc.TipoSocietario));

        var run = Assert.Single(await db.KasRuns.Where(r => r.DocumentId == documentId).ToListAsync());
        Assert.False(run.Ok);
        Assert.Equal(400, run.HttpStatus);
        Assert.Contains("property document_url should not exist", run.PayloadJson, StringComparison.Ordinal);

        var details = await db.AuditEvents.Where(e => e.DocumentId == documentId).Select(e => e.Details).ToListAsync();
        Assert.Contains(details, d => d.Contains("property document_url should not exist", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProcessNext_EmptyOutbox_ReturnsFalse()
    {
        await using var provider = BuildProvider();
        var processor = provider.GetRequiredService<OutboxKaasProcessor>();
        Assert.False(await processor.ProcessNextAsync());
        Assert.Empty(_kaas.LogEntries);
    }

    [Fact]
    public void ApiHost_DoesNotRegisterKasClient()
    {
        using var factory = new DocumentsApiFactory();
        using var scope = factory.Services.CreateScope();
        Assert.Null(scope.ServiceProvider.GetService<IKasClient>());
    }

    [Fact]
    public void Timeout_AllowsFiveMinuteJourneyWithSafetyMargin()
    {
        using var provider = BuildProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<KasOptions>>().Value;
        Assert.Equal(KasDefaults.TimeoutSeconds, options.TimeoutSeconds);
    }

    private ServiceProvider BuildProvider()
    {
        var runUrl = $"{_kaas.Url}/kas/triggers/journeys/testes-firmas-e-poderes/run";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=127.0.0.1;Port=5432;Database=unused;Username=bbf;Password=bbf",
                ["Documents:StorageRoot"] = _storageRoot,
                ["Kas:RunUrl"] = runUrl,
                ["Kas:ApiKey"] = FakeApiKey,
                ["Kas:TimeoutSeconds"] = "600",
                ["Kas:PollIntervalSeconds"] = "1"
            })
            .Build();

        var dbName = $"kaas-{Guid.NewGuid():N}";
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();
        services.AddSingleton<IAuditContext, WorkerTestAuditContext>();
        services.AddScoped<AppendOnlyAuditInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase(dbName);
            options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            options.AddInterceptors(sp.GetRequiredService<AppendOnlyAuditInterceptor>());
        });
        services.AddSingleton<IDocumentBlobStore>(new FileBackedTestBlobStore(_storageRoot));
        services.AddKaasPipeline(config);

        var provider = services.BuildServiceProvider();
        using (var scope = provider.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        }

        return provider;
    }

    private async Task<(string DocumentId, string CorrelationId)> SeedPendingDocumentAsync(ServiceProvider provider)
    {
        var documentId = $"doc_{Guid.NewGuid():N}";
        var correlationId = $"corr_{Guid.NewGuid():D}";
        var blobPath = Path.Combine(_storageRoot, documentId);
        await File.WriteAllBytesAsync(blobPath, TinyPdf);

        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Documents.Add(new Document
        {
            DocumentId = documentId,
            FileName = "contrato-social.pdf",
            Cnpj = string.Empty,
            RazaoSocial = string.Empty,
            TipoSocietario = string.Empty,
            UploadedAt = DateTimeOffset.UtcNow,
            UploadedBy = "operador-test",
            Status = DocStatus.pendente,
            FileHash = "abc123",
            ContentType = "application/pdf",
            StoragePath = blobPath,
            Paginas = 0,
            CorrelationId = correlationId
        });
        db.OutboxMessages.Add(new OutboxMessage
        {
            OutboxId = Guid.NewGuid(),
            Type = OutboxTypes.DocumentUploaded,
            PayloadJson = JsonSerializer.Serialize(new { documentId, correlationId }),
            CreatedAt = DateTimeOffset.UtcNow,
            ProcessedAt = null,
            DocumentId = documentId,
            CorrelationId = correlationId
        });
        await db.SaveChangesAsync();
        return (documentId, correlationId);
    }

    public void Dispose()
    {
        _kaas.Stop();
        _kaas.Dispose();
        try
        {
            if (Directory.Exists(_storageRoot))
                Directory.Delete(_storageRoot, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private sealed class WorkerTestAuditContext : IAuditContext
    {
        public string CorrelationId => CorrelationContext.Current ?? "corr_worker_test";
        public string Actor => "worker";
    }

    private sealed class FileBackedTestBlobStore(string root) : IDocumentBlobStore
    {
        public Task<StoredBlob> SaveAsync(string documentId, Stream content, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<byte[]> ReadAllBytesAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            var path = Path.IsPathRooted(storagePath) ? storagePath : Path.Combine(root, storagePath);
            return File.ReadAllBytesAsync(path, cancellationToken);
        }
    }
}
