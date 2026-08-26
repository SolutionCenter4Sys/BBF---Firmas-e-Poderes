using BbfFirmasPoderes.Domain.Documents;
using BbfFirmasPoderes.Domain.Entities;
using BbfFirmasPoderes.Domain.Enums;
using BbfFirmasPoderes.Infrastructure.Persistence;
using BbfFirmasPoderes.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BbfFirmasPoderes.Infrastructure.Documents;

public sealed record UploadInput(string FileName, string? ContentType, long Length, Stream Content);

public abstract record IngestOutcome
{
    public sealed record Accepted(
        string DocumentId,
        string Status,
        string CorrelationId,
        DateTimeOffset UploadedAt) : IngestOutcome;

    public sealed record MissingFile : IngestOutcome;
    public sealed record UnsupportedType : IngestOutcome;
    public sealed record InvalidSize(string Detail) : IngestOutcome;
}

public sealed class DocumentIngestService(
    AppDbContext db,
    IDocumentBlobStore blobStore,
    IOptions<DocumentsOptions> options)
{
    public async Task<IngestOutcome> IngestAsync(
        UploadInput? file,
        string actor,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        if (file is null)
            return new IngestOutcome.MissingFile();

        if (file.Length <= 0)
            return new IngestOutcome.InvalidSize("Arquivo vazio.");

        var maxBytes = options.Value.MaxUploadBytes > 0
            ? options.Value.MaxUploadBytes
            : UploadRules.DefaultMaxBytes;

        if (file.Length > maxBytes)
            return new IngestOutcome.InvalidSize($"Arquivo excede o limite de {maxBytes} bytes.");

        var fileName = UploadRules.SanitizeFileName(file.FileName);
        if (!UploadRules.IsAllowed(file.ContentType, fileName))
            return new IngestOutcome.UnsupportedType();

        var documentId = $"doc_{Guid.NewGuid():N}";
        var uploadedAt = DateTimeOffset.UtcNow;
        StoredBlob? stored = null;

        try
        {
            stored = await blobStore.SaveAsync(documentId, file.Content, cancellationToken);

            db.Documents.Add(new Document
            {
                DocumentId = documentId,
                FileName = fileName,
                Cnpj = string.Empty,
                RazaoSocial = string.Empty,
                TipoSocietario = string.Empty,
                UploadedAt = uploadedAt,
                UploadedBy = string.IsNullOrWhiteSpace(actor) ? "anonymous" : actor,
                Status = DocStatus.pendente,
                FileHash = stored.Sha256Hex,
                ContentType = file.ContentType,
                StoragePath = stored.Path,
                Paginas = 0,
                CorrelationId = correlationId
            });

            db.OutboxMessages.Add(new OutboxMessage
            {
                OutboxId = Guid.NewGuid(),
                Type = OutboxTypes.DocumentUploaded,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    documentId,
                    storagePath = stored.Path,
                    fileName,
                    contentType = file.ContentType,
                    fileHash = stored.Sha256Hex,
                    correlationId
                }),
                CreatedAt = uploadedAt,
                ProcessedAt = null,
                DocumentId = documentId,
                CorrelationId = correlationId
            });

            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (stored is not null && File.Exists(stored.Path))
                File.Delete(stored.Path);
            throw;
        }

        return new IngestOutcome.Accepted(documentId, nameof(DocStatus.pendente), correlationId, uploadedAt);
    }

    public Task<List<Document>> ListAsync(CancellationToken cancellationToken = default)
        => db.Documents
            .AsNoTracking()
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(cancellationToken);

    public Task<Document?> GetAsync(string documentId, CancellationToken cancellationToken = default)
        => db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
}
