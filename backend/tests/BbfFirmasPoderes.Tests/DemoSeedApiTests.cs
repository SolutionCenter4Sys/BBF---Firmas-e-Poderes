using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BbfFirmasPoderes.Domain.Auth;
using BbfFirmasPoderes.Infrastructure.Persistence.Seed;

namespace BbfFirmasPoderes.Tests;

public class DemoSeedApiTests : IClassFixture<DocumentsApiFactory>
{
    private readonly DocumentsApiFactory _factory;

    public DemoSeedApiTests(DocumentsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDocuments_IncludesAcmeApproved_DeltaReprovado_GamaManual()
    {
        var client = Authenticated(Roles.Operador);

        var all = await client.GetAsync("/v1/documents");
        var decidido = await client.GetAsync("/v1/documents?status=decidido");
        var revisao = await client.GetAsync("/v1/documents?status=revisao_humana");

        Assert.Equal(HttpStatusCode.OK, all.StatusCode);
        var allList = await all.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(allList.EnumerateArray(), item => item.GetProperty("documentId").GetString() == AcmeSeed.DocumentId);
        Assert.Contains(allList.EnumerateArray(), item => item.GetProperty("documentId").GetString() == DemoSeed.ReprovadoDocumentId);
        Assert.Contains(allList.EnumerateArray(), item => item.GetProperty("documentId").GetString() == DemoSeed.ManualDocumentId);

        var decididoList = await decidido.Content.ReadFromJsonAsync<JsonElement>();
        var acme = decididoList.EnumerateArray().First(item => item.GetProperty("documentId").GetString() == AcmeSeed.DocumentId);
        var delta = decididoList.EnumerateArray().First(item => item.GetProperty("documentId").GetString() == DemoSeed.ReprovadoDocumentId);
        Assert.Equal("ACME Indústrias LTDA", acme.GetProperty("razaoSocial").GetString());
        Assert.Equal("LTDA", acme.GetProperty("tipoSocietario").GetString());
        Assert.Equal("Delta EIRELI", delta.GetProperty("razaoSocial").GetString());
        Assert.Equal("EIRELI", delta.GetProperty("tipoSocietario").GetString());
        Assert.DoesNotContain(decididoList.EnumerateArray(), item => item.GetProperty("documentId").GetString() == DemoSeed.ManualDocumentId);

        var revisaoList = await revisao.Content.ReadFromJsonAsync<JsonElement>();
        var gama = Assert.Single(revisaoList.EnumerateArray());
        Assert.Equal(DemoSeed.ManualDocumentId, gama.GetProperty("documentId").GetString());
        Assert.Equal("Gama Investimentos LTDA", gama.GetProperty("razaoSocial").GetString());
        Assert.Equal("LTDA", gama.GetProperty("tipoSocietario").GetString());
        Assert.Equal("revisao_humana", gama.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Replay_SeedDec002_ReturnsReprovado_Dec003_ReturnsManual()
    {
        var client = Authenticated(Roles.Auditor);

        var reprovado = await client.PostAsync($"/v1/decision/{DemoSeed.ReprovadoDecisionId}/replay", content: null);
        var manual = await client.PostAsync($"/v1/decision/{DemoSeed.ManualDecisionId}/replay", content: null);

        Assert.Equal(HttpStatusCode.OK, reprovado.StatusCode);
        Assert.Equal(HttpStatusCode.OK, manual.StatusCode);

        var reprovadoJson = await reprovado.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("REPROVADO", reprovadoJson.GetProperty("status").GetString());
        Assert.Equal(DemoSeed.ReprovadoDocumentId, reprovadoJson.GetProperty("documentId").GetString());

        var manualJson = await manual.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("MANUAL", manualJson.GetProperty("status").GetString());
        Assert.Equal(DemoSeed.ManualDocumentId, manualJson.GetProperty("documentId").GetString());
    }

    private HttpClient Authenticated(string role)
    {
        var client = _factory.CreateClient();
        client.Bearer(role);
        return client;
    }
}

public class DemoAuthTests : IClassFixture<DocumentsApiFactory>
{
    private readonly DocumentsApiFactory _factory;

    public DemoAuthTests(DocumentsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_Operador_ReturnsJwt_AndListsDocuments()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/auth/login", new
        {
            email = TestAuth.DemoOperadorEmail,
            password = TestAuth.DemoOperadorPassword
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(Roles.Operador, json.GetProperty("role").GetString());
        var token = json.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var docs = await client.GetAsync("/v1/documents");
        Assert.Equal(HttpStatusCode.OK, docs.StatusCode);
    }

    [Fact]
    public async Task Login_Auditor_ReturnsJwt_AndReadsAuditTrail()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/auth/login", new
        {
            email = TestAuth.DemoAuditorEmail,
            password = TestAuth.DemoAuditorPassword
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(Roles.Auditor, json.GetProperty("role").GetString());
        var token = json.GetProperty("accessToken").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var trail = await client.GetAsync($"/v1/audit/trail?documentId={DemoSeed.ReprovadoDocumentId}");
        Assert.Equal(HttpStatusCode.OK, trail.StatusCode);
        var events = await trail.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(events.EnumerateArray(), item => item.GetProperty("eventId").GetString() == "ev_0008");
    }

    [Fact]
    public async Task Login_OperadorToken_CannotReadAuditTrail()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/v1/auth/login", new
        {
            email = TestAuth.DemoOperadorEmail,
            password = TestAuth.DemoOperadorPassword
        });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var trail = await client.GetAsync("/v1/audit/trail");
        Assert.Equal(HttpStatusCode.Forbidden, trail.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/auth/login", new
        {
            email = TestAuth.DemoOperadorEmail,
            password = "wrong-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_MissingBody_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/auth/login", new { email = TestAuth.DemoOperadorEmail });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
