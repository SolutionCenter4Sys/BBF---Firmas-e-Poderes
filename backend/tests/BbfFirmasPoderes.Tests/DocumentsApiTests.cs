using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BbfFirmasPoderes.Domain.Auth;
using BbfFirmasPoderes.Domain.Documents;
using BbfFirmasPoderes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BbfFirmasPoderes.Tests;

public class DocumentsApiTests : IClassFixture<DocumentsApiFactory>
{
    private static readonly byte[] TinyPdf = Encoding.ASCII.GetBytes("%PDF-1.1\n1 0 obj<</Type/Catalog>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF\n");

    private readonly DocumentsApiFactory _factory;

    public DocumentsApiTests(DocumentsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostDocuments_UnsupportedType_Returns415()
    {
        var client = AuthenticatedClient();
        using var content = Multipart("notes.txt", "text/plain", Encoding.UTF8.GetBytes("hello"));

        var response = await client.PostAsync("/v1/documents", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("não suportado", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostDocuments_OctetStreamWithPdfName_Returns415()
    {
        var client = AuthenticatedClient();
        using var content = Multipart("foo.pdf", "application/octet-stream", TinyPdf);

        var response = await client.PostAsync("/v1/documents", content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task PostDocuments_Oversize_Returns422()
    {
        var client = AuthenticatedClient();
        var oversized = new byte[2048];
        oversized[0] = (byte)'%';
        Encoding.ASCII.GetBytes("PDF-1.1").CopyTo(oversized, 1);
        using var content = Multipart("grande.pdf", "application/pdf", oversized);

        var response = await client.PostAsync("/v1/documents", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("excede", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostDocuments_HappyPath_Returns202_StatusPendente_OutboxUnprocessed()
    {
        var client = AuthenticatedClient();
        using var content = Multipart("contrato-social.pdf", "application/pdf", TinyPdf);

        var response = await client.PostAsync("/v1/documents", content);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var documentId = json.GetProperty("documentId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(documentId));
        Assert.Equal("pendente", json.GetProperty("status").GetString());
        Assert.StartsWith("corr_", json.GetProperty("correlationId").GetString());
        Assert.True(response.Headers.TryGetValues("Location", out var locations));
        Assert.Contains($"/v1/documents/{documentId}/status", Assert.Single(locations));

        var statusResponse = await client.GetAsync($"/v1/documents/{documentId}/status");
        var statusJson = await statusResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        Assert.Equal("pendente", statusJson.GetProperty("status").GetString());
        Assert.Equal(documentId, statusJson.GetProperty("documentId").GetString());
        Assert.Equal("contrato-social.pdf", statusJson.GetProperty("fileName").GetString());

        var listResponse = await client.GetAsync("/v1/documents");
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Contains(list.EnumerateArray(), item => item.GetProperty("documentId").GetString() == documentId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await db.Documents.FindAsync(documentId);
        Assert.NotNull(persisted);
        Assert.Equal(BbfFirmasPoderes.Domain.Enums.DocStatus.pendente, persisted!.Status);
        Assert.False(string.IsNullOrWhiteSpace(persisted.StoragePath));
        Assert.True(File.Exists(persisted.StoragePath));

        var outbox = Assert.Single(await db.OutboxMessages.Where(o => o.DocumentId == documentId).ToListAsync());
        Assert.Equal(OutboxTypes.DocumentUploaded, outbox.Type);
        Assert.Null(outbox.ProcessedAt);
        Assert.Contains(documentId, outbox.PayloadJson);
        Assert.DoesNotContain("kaas", outbox.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetDocuments_StatusDecidido_IncludesAcmeSeed()
    {
        var client = AuthenticatedClient();

        var response = await client.GetAsync("/v1/documents?status=decidido");
        var list = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(list.EnumerateArray(), item => item.GetProperty("documentId").GetString() == "doc_001");
        Assert.All(list.EnumerateArray(), item => Assert.Equal("decidido", item.GetProperty("status").GetString()));
        var acme = list.EnumerateArray().First(item => item.GetProperty("documentId").GetString() == "doc_001");
        Assert.Equal("12.345.678/0001-90", acme.GetProperty("cnpj").GetString());
        Assert.Equal("ACME Indústrias LTDA", acme.GetProperty("razaoSocial").GetString());
        Assert.Equal("LTDA", acme.GetProperty("tipoSocietario").GetString());
    }

    [Fact]
    public async Task GetDocuments_StatusRevisaoHumana_ExcludesAcmeSeed()
    {
        var client = AuthenticatedClient();

        var response = await client.GetAsync("/v1/documents?status=revisao_humana");
        var list = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(list.EnumerateArray(), item => item.GetProperty("documentId").GetString() == "doc_001");
    }

    [Fact]
    public async Task GetDocuments_InvalidStatus_Returns400()
    {
        var client = AuthenticatedClient();

        var response = await client.GetAsync("/v1/documents?status=nao_existe");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("status inválido", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetDocumentStatus_UnknownId_Returns404()
    {
        var client = AuthenticatedClient();

        var response = await client.GetAsync("/v1/documents/doc_nao_existe/status");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetCanonical_AcmeSeed_Returns3SociosAnd2Poderes_WithSourceTrace()
    {
        var client = AuthenticatedClient();

        var response = await client.GetAsync("/v1/documents/doc_001/canonical");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("doc_001", json.GetProperty("documentId").GetString());
        Assert.Equal("12.345.678/0001-90", json.GetProperty("cnpj").GetString());

        var pessoas = json.GetProperty("pessoas").EnumerateArray().ToArray();
        Assert.Equal(3, pessoas.Length);
        Assert.Equal(["p1", "p2", "p3"], pessoas.Select(p => p.GetProperty("personId").GetString()!).ToArray());
        Assert.Equal("João da Silva", pessoas[0].GetProperty("nome").GetString());
        Assert.Equal("Maria Souza", pessoas[1].GetProperty("nome").GetString());
        Assert.Equal("Carlos Pereira", pessoas[2].GetProperty("nome").GetString());
        Assert.Equal("inativo", pessoas[2].GetProperty("status").GetString());

        var poderes = json.GetProperty("poderes").EnumerateArray().ToArray();
        Assert.Equal(2, poderes.Length);
        Assert.Equal("pw1", poderes[0].GetProperty("powerId").GetString());
        Assert.Equal("pw2", poderes[1].GetProperty("powerId").GetString());
        Assert.Equal("isolada", poderes[0].GetProperty("modoAssinatura").GetProperty("tipo").GetString());
        Assert.Equal("conjunta", poderes[1].GetProperty("modoAssinatura").GetProperty("tipo").GetString());
        Assert.Equal(500000, poderes[0].GetProperty("limite").GetProperty("value").GetDecimal());

        var trace1 = poderes[0].GetProperty("sourceTrace");
        Assert.Equal(4, trace1.GetProperty("page").GetInt32());
        Assert.Equal(1280, trace1.GetProperty("offsetStart").GetInt32());
        Assert.Equal(1480, trace1.GetProperty("offsetEnd").GetInt32());
        Assert.Contains("Diretor poderá assinar isoladamente", trace1.GetProperty("snippet").GetString());

        var trace2 = poderes[1].GetProperty("sourceTrace");
        Assert.Equal(4, trace2.GetProperty("page").GetInt32());
        Assert.Equal(1500, trace2.GetProperty("offsetStart").GetInt32());
        Assert.Contains("assinatura conjunta", trace2.GetProperty("snippet").GetString());
    }

    [Fact]
    public async Task GetCanonical_UnknownId_Returns404()
    {
        var client = AuthenticatedClient();

        var response = await client.GetAsync("/v1/documents/doc_nao_existe/canonical");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private HttpClient AuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.Bearer(Roles.Operador);
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
