using System.Security.Cryptography;
using BbfFirmasPoderes.Domain.Documents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BbfFirmasPoderes.Infrastructure.Storage;

public sealed class FileDocumentBlobStore(IOptions<DocumentsOptions> options, IHostEnvironment environment)
    : IDocumentBlobStore
{
    public async Task<StoredBlob> SaveAsync(
        string documentId,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var root = ResolveRoot();
        Directory.CreateDirectory(root);

        var path = Path.Combine(root, documentId);
        await using var fs = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        using var sha = SHA256.Create();
        await using (var crypto = new CryptoStream(fs, sha, CryptoStreamMode.Write, leaveOpen: true))
        {
            await content.CopyToAsync(crypto, cancellationToken);
            await crypto.FlushFinalBlockAsync(cancellationToken);
        }

        await fs.FlushAsync(cancellationToken);
        var hash = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
        return new StoredBlob(path, hash, fs.Length);
    }

    private string ResolveRoot()
    {
        var configured = options.Value.StorageRoot;
        if (string.IsNullOrWhiteSpace(configured))
            configured = Path.Combine("data", "docs");

        return Path.IsPathRooted(configured)
            ? configured
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configured));
    }
}
