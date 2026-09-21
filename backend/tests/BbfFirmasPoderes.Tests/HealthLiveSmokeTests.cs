using System.Net;

namespace BbfFirmasPoderes.Tests;

public class HealthLiveSmokeTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public HealthLiveSmokeTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealthLive_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
