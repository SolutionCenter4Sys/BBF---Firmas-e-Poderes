namespace BbfFirmasPoderes.Infrastructure.Storage;

public sealed class DocumentsOptions
{
    public const string SectionName = "Documents";

    /// <summary>Raiz do volume. Relativo ao ContentRoot se não for absoluto. Default <c>data/docs</c>.</summary>
    public string StorageRoot { get; set; } = Path.Combine("data", "docs");

    public long MaxUploadBytes { get; set; } = Domain.Documents.UploadRules.DefaultMaxBytes;
}
