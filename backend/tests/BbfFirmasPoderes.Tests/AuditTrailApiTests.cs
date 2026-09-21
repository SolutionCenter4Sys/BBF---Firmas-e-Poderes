using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BbfFirmasPoderes.Domain.Audit;
using BbfFirmasPoderes.Domain.Auth;
using BbfFirmasPoderes.Infrastructure.Persistence.Seed;

namespace BbfFirmasPoderes.Tests;

public class AuditTrailApiTests : IClassFixture<DocumentsApiFactory>
{
    private static readonly byte[] TinyPdf =
        Encoding.ASCII.GetBytes("%PDF-1.1\n1 0 obj<</Type/Catalog>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF\n");

    private readonly DocumentsApiFactory _factory;

    public AuditTrailApiTests(DocumentsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetTrail_Operador_Returns403()
    {
        var client = Client(Roles.Operador);

        var response = await client.GetAsync("/v1/audit/trail");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Perfil sem permissão", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTrail_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/v1/audit/trail");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetTrail_AuditorAfterUpload_Returns200_WithDocumentUploaded()
    {
        var operador = Client(Roles.Operador);
        using var content = Multipart("contrato-social.pdf", "application/pdf", TinyPdf);
        var upload = await operador.PostAsync("/v1/documents", content);
        var uploaded = await upload.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Accepted, upload.StatusCode);
        var documentId = uploaded.GetProperty("documentId").GetString();
        var correlationId = uploaded.GetProperty("correlationId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(documentId));
        Assert.False(string.IsNullOrWhiteSpace(correlationId));

        var auditor = Client(Roles.Auditor);
        var response = await auditor.GetAsync($"/v1/audit/trail?documentId={documentId}");
        var trail = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(trail.ValueKind == JsonValueKind.Array);
        Assert.Contains(trail.EnumerateArray(), e =>
            e.GetProperty("type").GetString() == AuditEventTypes.DocumentUploaded
            && e.GetProperty("documentId").GetString() == documentId
            && e.GetProperty("correlationId").GetString() == correlationId
            && e.GetProperty("timestamp").GetDateTimeOffset() != default
            && e.GetProperty("eventId").GetString()!.StartsWith("ev_", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetTrail_FiltersByCorrelationIdAndDateRange()
    {
        var auditor = Client(Roles.Auditor);

        var seed = await auditor.GetAsync($"/v1/audit/trail?documentId={AcmeSeed.DocumentId}&correlationId={AcmeSeed.CorrelationId}");
        var seedTrail = await seed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.OK, seed.StatusCode);
        Assert.Contains(seedTrail.EnumerateArray(), e => e.GetProperty("eventId").GetString() == "ev_0001");

        var after = await auditor.GetAsync($"/v1/audit/trail?documentId={AcmeSeed.DocumentId}&from=2026-04-30T00:00:00Z");
        var afterTrail = await after.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
        Assert.DoesNotContain(afterTrail.EnumerateArray(), e => e.GetProperty("eventId").GetString() == "ev_0001");

        var window = await auditor.GetAsync(
            $"/v1/audit/trail?documentId={AcmeSeed.DocumentId}&from=2026-04-29T14:00:00Z&to=2026-04-29T14:30:00Z");
        var windowTrail = await window.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(windowTrail.EnumerateArray(), e => e.GetProperty("eventId").GetString() == "ev_0001");
    }

    [Fact]
    public async Task GetTrail_FromAfterTo_Returns400()
    {
        var auditor = Client(Roles.Auditor);

        var response = await auditor.GetAsync("/v1/audit/trail?from=2026-04-30T00:00:00Z&to=2026-04-29T00:00:00Z");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("from", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTrail_AfterEvaluate_IncludesDecisionEvaluated()
    {
        var operador = Client(Roles.Operador);
        var payload = new
        {
            documentId = AcmeSeed.DocumentId,
            cnpj = "12.345.678/0001-90",
            operacao = "movimentacao_financeira",
            valorOperacao = 500000,
            currency = "BRL",
            signatariosSolicitados = new[] { "João da Silva (Diretor)" },
            asOf = "2026-04-29T14:25:00Z"
        };

        var evaluate = await operador.PostAsJsonAsync("/v1/decision/evaluate", payload);
        Assert.Equal(HttpStatusCode.OK, evaluate.StatusCode);

        var auditor = Client(Roles.Auditor);
        var response = await auditor.GetAsync($"/v1/audit/trail?documentId={AcmeSeed.DocumentId}");
        var trail = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(trail.EnumerateArray(), e =>
            e.GetProperty("type").GetString() == AuditEventTypes.DecisionEvaluated
            && !string.IsNullOrWhiteSpace(e.GetProperty("decisionId").GetString()));
    }

    [Fact]
    public async Task Swagger_ListsAuditTrailRoute()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, body);
        Assert.Contains("/v1/audit/trail", body);
        Assert.DoesNotContain("\"/audit\"", body);
    }

    private HttpClient Client(string role)
    {
        var client = _factory.CreateClient();
        client.Bearer(role);
        return client;
    }

    private static MultipartFormDataContent Multipart(string fileName, string contentType, byte[] bytes)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var content = new MultipartFormDataContent();
        content.Add(file, "file", fileName);
        return content;
    }
}
