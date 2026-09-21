using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BbfFirmasPoderes.Domain.Auth;
using BbfFirmasPoderes.Domain.Verification;

namespace BbfFirmasPoderes.Tests;

public class VerificationHealthApiTests : IClassFixture<DocumentsApiFactory>
{
    private readonly DocumentsApiFactory _factory;

    public VerificationHealthApiTests(DocumentsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_Operador_Returns200WithCircuitBreakerStates()
    {
        var client = _factory.CreateClient();
        client.Bearer(Roles.Operador);

        var response = await client.GetAsync("/v1/verification/health");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(4, json.GetArrayLength());

        var juntaSp = Find(json, OfficialSourcesCatalog.JuntaSp);
        Assert.Equal("operacional", juntaSp.GetProperty("status").GetString());
        Assert.Equal(CircuitBreakerStates.Fechado, juntaSp.GetProperty("circuitBreaker").GetString());

        var juntaRj = Find(json, OfficialSourcesCatalog.JuntaRj);
        Assert.Equal(CircuitBreakerStates.MeioAberto, juntaRj.GetProperty("circuitBreaker").GetString());

        var pep = Find(json, OfficialSourcesCatalog.CompliancePep);
        Assert.Equal("indisponivel", pep.GetProperty("status").GetString());
        Assert.Equal(CircuitBreakerStates.Aberto, pep.GetProperty("circuitBreaker").GetString());
    }

    [Fact]
    public async Task GetHealth_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/v1/verification/health");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_Consumer_Returns403()
    {
        var client = _factory.CreateClient();
        client.Bearer(Roles.Consumer);

        var response = await client.GetAsync("/v1/verification/health");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static JsonElement Find(JsonElement array, string sourceId)
        => array.EnumerateArray().Single(e => e.GetProperty("sourceId").GetString() == sourceId);
}
