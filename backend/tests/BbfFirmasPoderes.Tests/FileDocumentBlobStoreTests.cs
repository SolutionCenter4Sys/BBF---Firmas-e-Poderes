using BbfFirmasPoderes.Infrastructure.Storage;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BbfFirmasPoderes.Tests;

public class FileDocumentBlobStoreTests
{
    [Fact]
    public async Task ReadAllBytes_AbsolutePathOutsideRoot_ThrowsUnauthorized()
    {
        var root = Path.Combine(Path.GetTempPath(), "bbf-blob-jail-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new FileDocumentBlobStore(
                Options.Create(new DocumentsOptions { StorageRoot = root }),
                new StubHost(root));

            var outside = Path.Combine(Path.GetTempPath(), "bbf-outside-" + Guid.NewGuid().ToString("N") + ".bin");
            await File.WriteAllBytesAsync(outside, [1, 2, 3]);
            try
            {
                await Assert.ThrowsAsync<UnauthorizedAccessException>(
                    () => store.ReadAllBytesAsync(outside));
            }
            finally
            {
                File.Delete(outside);
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ReadAllBytes_RelativePathInsideRoot_ReturnsBytes()
    {
        var root = Path.Combine(Path.GetTempPath(), "bbf-blob-ok-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new FileDocumentBlobStore(
                Options.Create(new DocumentsOptions { StorageRoot = root }),
                new StubHost(root));

            await using (var input = new MemoryStream([9, 8, 7]))
            {
                var saved = await store.SaveAsync("doc_test", input);
                var bytes = await store.ReadAllBytesAsync(saved.Path);
                Assert.Equal(new byte[] { 9, 8, 7 }, bytes);
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class StubHost(string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = contentRoot;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
