using BbfFirmasPoderes.Domain.Auth;
using BbfFirmasPoderes.Domain.Correlation;
using BbfFirmasPoderes.Domain.Enums;
using BbfFirmasPoderes.Infrastructure.Documents;
using Microsoft.AspNetCore.Mvc;

namespace BbfFirmasPoderes.Api.Documents;

public static class DocumentsEndpoints
{
    public static WebApplication MapDocuments(this WebApplication app)
    {
        var group = app.MapGroup("/v1/documents").WithTags("Documentos");

        group.MapPost("/", Upload)
            .RequireAuthorization(Policies.DocumentsUpload)
            .DisableAntiforgery()
            .WithSummary("Upload de documento societário")
            .WithDescription("Aceita multipart campo file e enfileira no outbox. Não processa OCR. Resposta 202 com status pendente.");

        group.MapGet("/", List)
            .RequireAuthorization(Policies.DocumentsRead)
            .WithSummary("Listar documentos")
            .WithDescription("Query opcional status (DocStatus). Filas de revisão/manual usam revisao_humana.");

        group.MapGet("/{documentId}/status", GetStatus)
            .RequireAuthorization(Policies.DocumentsRead)
            .WithSummary("Consultar status do pipeline");

        group.MapGet("/{documentId}/canonical", GetCanonical)
            .RequireAuthorization(Policies.DocumentsRead)
            .WithSummary("Modelo canônico (pessoas e poderes)")
            .WithDescription("Devolve Person[] e Power[] persistidos, com sourceTrace. Schema canonicalSchemaPreview.");

        return app;
    }

    private static async Task<IResult> Upload(
        HttpContext http,
        DocumentIngestService ingest,
        CancellationToken cancellationToken)
    {
        var correlationId = CorrelationContext.Current
            ?? http.Items[CorrelationIds.HeaderName]?.ToString()
            ?? CorrelationIds.New();

        if (!http.Request.HasFormContentType)
        {
            return Problem(
                http,
                StatusCodes.Status415UnsupportedMediaType,
                "Unsupported Media Type",
                "https://tools.ietf.org/html/rfc9110#section-15.5.16",
                "Content-Type deve ser multipart/form-data.");
        }

        var form = await http.Request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");

        Stream? stream = null;
        try
        {
            UploadInput? input = null;
            if (file is not null)
            {
                stream = file.OpenReadStream();
                input = new UploadInput(file.FileName, file.ContentType, file.Length, stream);
            }

            var actor = http.User.Identity?.Name ?? "anonymous";
            var outcome = await ingest.IngestAsync(input, actor, correlationId, cancellationToken);

            return outcome switch
            {
                IngestOutcome.Accepted accepted => Accepted(http, accepted),
                IngestOutcome.UnsupportedType => Problem(
                    http,
                    StatusCodes.Status415UnsupportedMediaType,
                    "Unsupported Media Type",
                    "https://tools.ietf.org/html/rfc9110#section-15.5.16",
                    "Tipo de arquivo não suportado. Envie PDF ou imagem."),
                IngestOutcome.InvalidSize invalid => Problem(
                    http,
                    StatusCodes.Status422UnprocessableEntity,
                    "Unprocessable Entity",
                    "https://tools.ietf.org/html/rfc4918#section-11.2",
                    invalid.Detail),
                IngestOutcome.MissingFile => Problem(
                    http,
                    StatusCodes.Status422UnprocessableEntity,
                    "Unprocessable Entity",
                    "https://tools.ietf.org/html/rfc4918#section-11.2",
                    "Arquivo obrigatório no campo multipart 'file'."),
                _ => Problem(
                    http,
                    StatusCodes.Status422UnprocessableEntity,
                    "Unprocessable Entity",
                    "https://tools.ietf.org/html/rfc4918#section-11.2",
                    "Upload inválido.")
            };
        }
        finally
        {
            if (stream is not null)
                await stream.DisposeAsync();
        }
    }

    private static IResult Accepted(HttpContext http, IngestOutcome.Accepted accepted)
    {
        http.Response.Headers.Location = $"/v1/documents/{accepted.DocumentId}/status";
        return Results.Json(
            new UploadAcceptedResponse(
                accepted.DocumentId,
                accepted.Status,
                accepted.CorrelationId,
                accepted.UploadedAt),
            statusCode: StatusCodes.Status202Accepted);
    }

    private static async Task<IResult> List(
        HttpContext http,
        DocumentIngestService ingest,
        string? status,
        CancellationToken cancellationToken)
    {
        DocStatus? filter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TryParseDocStatus(status, out var parsed))
            {
                return Problem(
                    http,
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    "status inválido. Use um valor de DocStatus.");
            }

            filter = parsed;
        }

        var docs = await ingest.ListAsync(filter, cancellationToken);
        var items = docs.Select(d => new DocumentListItem(
            d.DocumentId,
            d.FileName,
            d.Status.ToString(),
            d.UploadedAt,
            d.CorrelationId,
            EmptyToNull(d.Cnpj),
            EmptyToNull(d.RazaoSocial),
            EmptyToNull(d.TipoSocietario),
            new ConfidenceDto(d.ConfiancaOcr, d.ConfiancaIagen, d.ConfiancaNer))).ToArray();
        return Results.Json(items);
    }

    private static bool TryParseDocStatus(string raw, out DocStatus status)
    {
        status = default;
        var trimmed = raw.Trim();
        if (int.TryParse(trimmed, out _))
            return false;

        return Enum.TryParse(trimmed, ignoreCase: true, out status) && Enum.IsDefined(status);
    }

    private static string? EmptyToNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static async Task<IResult> GetStatus(
        HttpContext http,
        string documentId,
        DocumentIngestService ingest,
        CancellationToken cancellationToken)
    {
        var doc = await ingest.GetAsync(documentId, cancellationToken);
        if (doc is null)
        {
            return Problem(
                http,
                StatusCodes.Status404NotFound,
                "Not Found",
                "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                "Documento não encontrado.");
        }

        return Results.Json(new DocumentStatusResponse(
            doc.DocumentId,
            doc.Status.ToString(),
            doc.FileName,
            doc.UploadedAt,
            doc.CorrelationId,
            doc.Paginas,
            doc.FileHash,
            new ConfidenceDto(doc.ConfiancaOcr, doc.ConfiancaIagen, doc.ConfiancaNer),
            doc.Status == DocStatus.falha
                ? await ingest.GetLastKasErrorAsync(documentId, cancellationToken)
                : null));
    }

    private static async Task<IResult> GetCanonical(
        HttpContext http,
        string documentId,
        DocumentIngestService ingest,
        CancellationToken cancellationToken)
    {
        var doc = await ingest.GetCanonicalAsync(documentId, cancellationToken);
        if (doc is null)
        {
            return Problem(
                http,
                StatusCodes.Status404NotFound,
                "Not Found",
                "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                "Documento não encontrado.");
        }

        var pessoas = doc.Socios
            .OrderBy(p => p.PersonId)
            .Select(p => new CanonicalPersonDto(
                p.PersonId,
                p.Nome,
                p.Cpf,
                p.Documento,
                p.Rg,
                p.PersonType,
                p.Quotas,
                p.MandateStart,
                p.MandateEnd,
                p.Qualificacao,
                p.Cargo,
                p.Status.ToString()))
            .ToArray();

        var poderes = doc.Poderes
            .OrderBy(p => p.PowerId)
            .Select(p => new CanonicalPowerDto(
                p.PowerId,
                p.Pessoa,
                p.Operacao,
                new CanonicalLimiteDto(p.LimiteCurrency, p.LimiteValue, p.LimiteExpression),
                new CanonicalModoAssinaturaDto(
                    p.ModoAssinaturaTipo.ToString(),
                    p.ModoAssinaturaN,
                    p.ModoAssinaturaM,
                    p.ModoAssinaturaQualificacoes),
                new CanonicalVigenciaDto(p.VigenciaFrom, p.VigenciaTo),
                new CanonicalSourceTraceDto(p.SourcePage, p.SourceOffsetStart, p.SourceOffsetEnd, p.SourceSnippet)))
            .ToArray();

        return Results.Json(new CanonicalDocumentResponse(doc.DocumentId, doc.Cnpj, pessoas, poderes));
    }

    private static IResult Problem(HttpContext http, int status, string title, string type, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = type,
            Detail = detail,
            Instance = http.Request.Path
        };
        problem.Extensions["correlationId"] = CorrelationContext.Current
            ?? http.Items[CorrelationIds.HeaderName]?.ToString();

        return Results.Json(problem, statusCode: status, contentType: "application/problem+json");
    }
}

public sealed record UploadAcceptedResponse(
    string DocumentId,
    string Status,
    string CorrelationId,
    DateTimeOffset UploadedAt);

public sealed record DocumentListItem(
    string DocumentId,
    string FileName,
    string Status,
    DateTimeOffset UploadedAt,
    string? CorrelationId,
    string? Cnpj,
    string? RazaoSocial,
    string? TipoSocietario,
    ConfidenceDto? Confianca);

public sealed record ConfidenceDto(decimal Ocr, decimal Iagen, decimal Ner);

public sealed record DocumentStatusResponse(
    string DocumentId,
    string Status,
    string? FileName,
    DateTimeOffset UploadedAt,
    string? CorrelationId,
    int Paginas,
    string? Hash,
    ConfidenceDto? Confianca,
    string? LastError);

public sealed record CanonicalDocumentResponse(
    string DocumentId,
    string Cnpj,
    IReadOnlyList<CanonicalPersonDto> Pessoas,
    IReadOnlyList<CanonicalPowerDto> Poderes);

public sealed record CanonicalPersonDto(
    string PersonId,
    string Nome,
    string Cpf,
    string Documento,
    string Rg,
    string PersonType,
    decimal? Quotas,
    DateOnly? MandateStart,
    DateOnly? MandateEnd,
    string Qualificacao,
    string Cargo,
    string Status);

public sealed record CanonicalPowerDto(
    string PowerId,
    string Pessoa,
    string Operacao,
    CanonicalLimiteDto Limite,
    CanonicalModoAssinaturaDto ModoAssinatura,
    CanonicalVigenciaDto Vigencia,
    CanonicalSourceTraceDto SourceTrace);

public sealed record CanonicalLimiteDto(string Currency, decimal Value, string Expression);

public sealed record CanonicalModoAssinaturaDto(
    string Tipo,
    int? N,
    int? M,
    string[]? Qualificacoes);

public sealed record CanonicalVigenciaDto(DateOnly ValidFrom, DateOnly? ValidTo);

public sealed record CanonicalSourceTraceDto(int Page, int OffsetStart, int OffsetEnd, string Snippet);
