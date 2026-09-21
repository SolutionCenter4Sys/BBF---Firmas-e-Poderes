namespace BbfFirmasPoderes.Domain.Documents;

public sealed record StoredBlob(string Path, string Sha256Hex, long ByteCount);

public interface IDocumentBlobStore
{
    Task<StoredBlob> SaveAsync(string documentId, Stream content, CancellationToken cancellationToken = default);

    Task<byte[]> ReadAllBytesAsync(string storagePath, CancellationToken cancellationToken = default);
}
