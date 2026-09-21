using BbfFirmasPoderes.Domain.Enums;
using BbfFirmasPoderes.Domain.Kaas;

namespace BbfFirmasPoderes.Tests;

public class KasStatusMapperTests
{
    [Fact]
    public void FromPayload_NotOk_IsFalha()
    {
        Assert.Equal(DocStatus.falha, KasStatusMapper.FromPayload("""{"status":"canonico_pronto"}""", KasRunAction.ingest, ok: false));
    }

    [Fact]
    public void FromPayload_IngestWithoutStatus_IsProcessandoIagen()
    {
        Assert.Equal(
            DocStatus.processando_iagen,
            KasStatusMapper.FromPayload("""{"executionId":"exec_1","ok":true}""", KasRunAction.ingest, ok: true));
    }

    [Fact]
    public void FromPayload_ResultWithoutStatus_IsCanonicoPronto()
    {
        Assert.Equal(
            DocStatus.canonico_pronto,
            KasStatusMapper.FromPayload("""{"ok":true}""", KasRunAction.result, ok: true));
    }

    [Fact]
    public void FromPayload_ExplicitDocStatus_Wins()
    {
        Assert.Equal(
            DocStatus.processando_ner,
            KasStatusMapper.FromPayload("""{"body":{"status":"processando_ner"}}""", KasRunAction.ingest, ok: true));
    }

    [Fact]
    public void FromPayload_NestedSourceFailed_DoesNotMarkFalha()
    {
        var json =
            """
            {"mode":"sync","result":{"error":null,"output":{"verdict":"approved","sources":[{"status":"failed"},{"status":"skipped"}]}}}
            """;
        Assert.Equal(
            DocStatus.canonico_pronto,
            KasStatusMapper.FromPayload(json, KasRunAction.ingest, ok: true));
    }

    [Fact]
    public void FromSync_ManualAnalysis_IsRevisaoHumana()
    {
        Assert.Equal(
            DocStatus.revisao_humana,
            KasStatusMapper.FromSync("""{"status":"done","verdict":"manual_analysis"}""", ok: true));
    }

    [Fact]
    public void FromHttp_NestValidationArray_JoinsMessages()
    {
        var raw = """{"error":"Bad Request","message":["property document_url should not exist"],"statusCode":400}""";
        Assert.Equal(
            "KAAS HTTP 400: property document_url should not exist",
            KasErrorText.FromHttp(400, raw, "Bad Request"));
        Assert.Equal("property document_url should not exist", KasErrorText.FromBody(raw));
    }

    [Fact]
    public void FromHttp_EntityTooLarge_PrefersErrorOverGenericMessage()
    {
        var raw = """{"error":"request entity too large","message":"Internal server error","statusCode":500}""";
        Assert.Equal("request entity too large", KasErrorText.FromBody(raw));
        Assert.Equal(
            "KAAS HTTP 500: request entity too large",
            KasErrorText.FromHttp(500, raw, "Internal Server Error"));
    }

    [Fact]
    public void ExecutionId_FindsSnakeCase()
    {
        Assert.Equal("e27be89e", KasExecutionIds.ExtractFromJson("""{"execution_id":"e27be89e"}"""));
    }

    [Fact]
    public void FromPayload_CompletedAlias_FallsBackToAction()
    {
        Assert.Equal(
            DocStatus.processando_iagen,
            KasStatusMapper.FromPayload("""{"status":"completed"}""", KasRunAction.ingest, ok: true));
    }

    [Fact]
    public void ExecutionId_FindsNestedRunId()
    {
        var json = """{"data":{"run_id":"exec_nested"}}""";
        Assert.Equal("exec_nested", KasExecutionIds.ExtractFromJson(json));
    }
}
