using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BbfFirmasPoderes.Domain.Auth;
using BbfFirmasPoderes.Infrastructure.Persistence.Seed;

namespace BbfFirmasPoderes.Tests;

public class DecisionApiTests : IClassFixture<DocumentsApiFactory>
{
    private readonly DocumentsApiFactory _factory;

    public DecisionApiTests(DocumentsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Evaluate_AcmeDiretorMovimentacao_Aprovado_ReplayIdentico()
    {
        var operador = Authenticated(Roles.Operador);
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
        var evaluateBody = await evaluate.Content.ReadAsStringAsync();
        var evaluateJson = JsonDocument.Parse(evaluateBody).RootElement;

        Assert.Equal(HttpStatusCode.OK, evaluate.StatusCode);
        Assert.Equal("APROVADO", evaluateJson.GetProperty("status").GetString());
        Assert.Equal(AcmeSeed.DocumentId, evaluateJson.GetProperty("documentId").GetString());
        Assert.Equal("1.2.0", evaluateJson.GetProperty("versions").GetProperty("rules").GetString());
        Assert.Equal("1.0.0", evaluateJson.GetProperty("versions").GetProperty("canonical").GetString());
        Assert.Equal("leitura-contrato-social@2.1.0", evaluateJson.GetProperty("versions").GetProperty("aiPrompt").GetString());
        Assert.Equal("gemini-1.5-pro", evaluateJson.GetProperty("versions").GetProperty("aiModel").GetString());
        Assert.Contains("RN02", evaluateJson.GetProperty("motivos").EnumerateArray().First().GetString());
        var evidence = evaluateJson.GetProperty("evidencias").EnumerateArray().First();
        Assert.Equal("documento", evidence.GetProperty("type").GetString());
        Assert.Equal(4, evidence.GetProperty("trace").GetProperty("page").GetInt32());
        Assert.Contains("isoladamente", evidence.GetProperty("trace").GetProperty("snippet").GetString());

        var decisionId = evaluateJson.GetProperty("decisionId").GetString();
        Assert.StartsWith("dec_", decisionId);

        var auditor = Authenticated(Roles.Auditor);
        var replay = await auditor.PostAsync($"/v1/decision/{decisionId}/replay", content: null);
        var replayBody = await replay.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        Assert.Equal(evaluateBody, replayBody);
    }

    [Fact]
    public async Task Replay_SeedDec001_ReturnsSnapshot()
    {
        var client = Authenticated(Roles.Auditor);

        var response = await client.PostAsync($"/v1/decision/{AcmeSeed.DecisionId}/replay", content: null);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(AcmeSeed.DecisionId, json.GetProperty("decisionId").GetString());
        Assert.Equal("APROVADO", json.GetProperty("status").GetString());
        Assert.Equal(1287, json.GetProperty("latencyMs").GetInt32());
        Assert.Equal("1.2.0", json.GetProperty("versions").GetProperty("rules").GetString());
        Assert.Equal(2, json.GetProperty("evidencias").GetArrayLength());
    }

    [Fact]
    public async Task Replay_Unknown_Returns404()
    {
        var client = Authenticated(Roles.Auditor);

        var response = await client.PostAsync("/v1/decision/dec_nao_existe/replay", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Evaluate_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/decision/evaluate", new { documentId = AcmeSeed.DocumentId });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Evaluate_Auditor_Returns403()
    {
        var client = Authenticated(Roles.Auditor);

        var response = await client.PostAsJsonAsync("/v1/decision/evaluate", new
        {
            documentId = AcmeSeed.DocumentId,
            operacao = "movimentacao_financeira",
            signatariosSolicitados = new[] { "Diretor" }
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Replay_Operador_Returns403()
    {
        var client = Authenticated(Roles.Operador);

        var response = await client.PostAsync($"/v1/decision/{AcmeSeed.DecisionId}/replay", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Evaluate_MissingOperacao_Returns400()
    {
        var client = Authenticated(Roles.Operador);

        var response = await client.PostAsJsonAsync("/v1/decision/evaluate", new
        {
            documentId = AcmeSeed.DocumentId,
            signatariosSolicitados = new[] { "Diretor" }
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("operacao", body, StringComparison.OrdinalIgnoreCase);
    }

    private HttpClient Authenticated(string role)
    {
        var client = _factory.CreateClient();
        client.Bearer(role);
        return client;
    }
}
