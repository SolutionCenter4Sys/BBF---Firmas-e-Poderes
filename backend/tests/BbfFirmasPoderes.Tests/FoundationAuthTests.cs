using System.Net;
using BbfFirmasPoderes.Domain.Auth;
using BbfFirmasPoderes.Domain.Correlation;

namespace BbfFirmasPoderes.Tests;

public class FoundationAuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public FoundationAuthTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealthLive_WithoutToken_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetHealth_WithAuditorToken_Returns200()
    {
        var client = _factory.CreateClient();
        client.Bearer(Roles.Auditor);

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_WithOperadorToken_Returns403()
    {
        var client = _factory.CreateClient();
        client.Bearer(Roles.Operador);

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_WithAdminToken_Returns200()
    {
        var client = _factory.CreateClient();
        client.Bearer(Roles.Admin);

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSwagger_WithoutToken_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetSwaggerJson_WithoutToken_Returns200_AndListsDocumentsRoutes()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, body);
        Assert.Contains("openapi", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/v1/documents", body);
        Assert.DoesNotContain("/v1/authority", body);
    }

    [Fact]
    public async Task MissingCorrelationHeader_GeneratesCorrPrefixedId()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.True(response.Headers.TryGetValues(CorrelationIds.HeaderName, out var values));
        var correlationId = Assert.Single(values);
        Assert.StartsWith("corr_", correlationId);
    }

    [Fact]
    public async Task IncomingCorrelationHeader_IsEchoed()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(CorrelationIds.HeaderName, "corr_client_fixed");

        var response = await client.GetAsync("/health/live");

        Assert.True(response.Headers.TryGetValues(CorrelationIds.HeaderName, out var values));
        Assert.Equal("corr_client_fixed", Assert.Single(values));
    }
}
