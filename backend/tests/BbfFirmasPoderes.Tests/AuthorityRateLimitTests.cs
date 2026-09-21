using System.Net;
using BbfFirmasPoderes.Domain.Auth;
using Microsoft.AspNetCore.Hosting;

namespace BbfFirmasPoderes.Tests;

public class AuthorityRateLimitTests : IClassFixture<RateLimitedAuthorityFactory>
{
    private readonly RateLimitedAuthorityFactory _factory;

    public AuthorityRateLimitTests(RateLimitedAuthorityFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDecision_ExceedsPermitLimit_Returns429()
    {
        var client = _factory.CreateClient();
        client.Bearer(Roles.Consumer);
        const string path =
            "/v1/authority/decision?cnpj=12.345.678/0001-90&operation=movimentacao_financeira&signers=p1";

        var first = await client.GetAsync(path);
        var second = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal((HttpStatusCode)429, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);
        Assert.True(second.Headers.RetryAfter is not null || second.Headers.Contains("Retry-After"));
    }
}

public sealed class RateLimitedAuthorityFactory : DocumentsApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("RateLimiting:PermitLimit", "1");
        builder.UseSetting("RateLimiting:WindowSeconds", "60");
    }
}
