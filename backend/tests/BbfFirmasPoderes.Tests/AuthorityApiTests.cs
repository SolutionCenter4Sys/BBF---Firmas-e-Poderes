using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BbfFirmasPoderes.Domain.Auth;
using BbfFirmasPoderes.Domain.Correlation;
using BbfFirmasPoderes.Domain.Verification;
using BbfFirmasPoderes.Infrastructure.Persistence.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace BbfFirmasPoderes.Tests;

public class AuthorityApiTests : IClassFixture<DocumentsApiFactory>
{
    private const string AcmeCnpj = "12.345.678/0001-90";
    private const string AcmePath =
        "/v1/authority/decision?cnpj=12.345.678/0001-90&operation=movimentacao_financeira&signers=p1";

    private readonly DocumentsApiFactory _factory;

    public AuthorityApiTests(DocumentsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDecision_AcmeDiretor_Returns200Aprovado()
    {
        var client = Authenticated(Roles.Consumer);

        var response = await client.GetAsync(AcmePath);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("APROVADO", json.GetProperty("status").GetString());
        Assert.Equal(AcmeSeed.DocumentId, json.GetProperty("documentId").GetString());
        Assert.Equal(AcmeCnpj, json.GetProperty("cnpj").GetString());
        Assert.StartsWith("dec_", json.GetProperty("decisionId").GetString());
        Assert.Contains("RN02", json.GetProperty("motivos").EnumerateArray().First().GetString());
        var evidence = json.GetProperty("evidencias").EnumerateArray().First();
        Assert.Equal("documento", evidence.GetProperty("type").GetString());
        Assert.Equal(4, evidence.GetProperty("trace").GetProperty("page").GetInt32());
        Assert.True(response.Headers.Contains(CorrelationIds.HeaderName));
    }

    [Fact]
    public async Task GetDecision_MissingParams_Returns400()
    {
        var client = Authenticated(Roles.Consumer);

        var missingCnpj = await client.GetAsync("/v1/authority/decision?operation=movimentacao_financeira&signers=p1");
        var missingOp = await client.GetAsync($"/v1/authority/decision?cnpj={AcmeCnpj}&signers=p1");
        var missingSigners = await client.GetAsync($"/v1/authority/decision?cnpj={AcmeCnpj}&operation=movimentacao_financeira");
        var badCnpj = await client.GetAsync("/v1/authority/decision?cnpj=123&operation=movimentacao_financeira&signers=p1");

        Assert.Equal(HttpStatusCode.BadRequest, missingCnpj.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingOp.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingSigners.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badCnpj.StatusCode);
        Assert.Equal("application/problem+json", missingCnpj.Content.Headers.ContentType?.MediaType);
        Assert.Contains("cnpj", await missingCnpj.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("operation", await missingOp.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("signers", await missingSigners.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetDecision_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(AcmePath);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetDecision_Auditor_Returns403()
    {
        var client = Authenticated(Roles.Auditor);

        var response = await client.GetAsync(AcmePath);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDecision_IdempotencyKey_ReplaysSameDecisionId()
    {
        var client = Authenticated(Roles.Consumer);
        client.DefaultRequestHeaders.Add(IdempotencyKeys.HeaderName, "idem-acme-" + Guid.NewGuid().ToString("N"));

        var first = await client.GetAsync(AcmePath);
        var firstJson = await first.Content.ReadFromJsonAsync<JsonElement>();
        var second = await client.GetAsync(AcmePath);
        var secondJson = await second.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(
            firstJson.GetProperty("decisionId").GetString(),
            secondJson.GetProperty("decisionId").GetString());
    }

    [Fact]
    public async Task GetDecision_CircuitOpen_Returns200ManualFallback()
    {
        var store = _factory.Services.GetRequiredService<IOfficialSourceHealthStore>();
        store.SetCircuitBreaker(OfficialSourcesCatalog.JuntaSp, CircuitBreakerStates.Aberto, "teste WF-11");
        try
        {
            var client = Authenticated(Roles.Consumer);
            var response = await client.GetAsync(AcmePath);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("MANUAL", json.GetProperty("status").GetString());
            Assert.Contains("circuit breaker aberto", json.GetProperty("motivos").EnumerateArray().First().GetString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            store.SetCircuitBreaker(OfficialSourcesCatalog.JuntaSp, CircuitBreakerStates.Fechado);
        }
    }

    [Fact]
    public async Task GetDecision_EchoesCorrelationId()
    {
        var client = Authenticated(Roles.Consumer);
        client.DefaultRequestHeaders.Add(CorrelationIds.HeaderName, "corr_authority_test");

        var response = await client.GetAsync(AcmePath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(CorrelationIds.HeaderName, out var values));
        Assert.Equal("corr_authority_test", Assert.Single(values));
    }

    private HttpClient Authenticated(string role)
    {
        var client = _factory.CreateClient();
        client.Bearer(role);
        return client;
    }
}
