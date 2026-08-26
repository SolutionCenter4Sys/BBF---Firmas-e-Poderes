using BbfFirmasPoderes.Domain.Auth;
using BbfFirmasPoderes.Domain.Correlation;
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
            .WithSummary("Listar documentos");

        group.MapGet("/{documentId}/status", GetStatus)
            .RequireAuthorization(Policies.DocumentsRead)
            .WithSummary("Consultar status do pipeline");

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

    private static async Task<IResult> List(DocumentIngestService ingest, CancellationToken cancellationToken)
    {
        var docs = await ingest.ListAsync(cancellationToken);
        var items = docs.Select(d => new DocumentListItem(
            d.DocumentId,
            d.FileName,
            d.Status.ToString(),
            d.UploadedAt,
            d.CorrelationId)).ToArray();
        return Results.Json(items);
    }

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
            new ConfidenceDto(doc.ConfiancaOcr, doc.ConfiancaIagen, doc.ConfiancaNer)));
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
    string? CorrelationId);

public sealed record ConfidenceDto(decimal Ocr, decimal Iagen, decimal Ner);

public sealed record DocumentStatusResponse(
    string DocumentId,
    string Status,
    string? FileName,
    DateTimeOffset UploadedAt,
    string? CorrelationId,
    int Paginas,
    string? Hash,
    ConfidenceDto? Confianca);
