using System.Text;
using BbfFirmasPoderes.Domain.Documents;

namespace BbfFirmasPoderes.Tests;

public class UploadRulesTests
{
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.1\n%%EOF\n");
    private static readonly byte[] Png =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00
    ];

    [Fact]
    public void IsAllowed_PdfMimeAndExtAndMagic_Passes()
    {
        Assert.True(UploadRules.IsAllowed("application/pdf", "contrato.pdf", Pdf));
    }

    [Fact]
    public void IsAllowed_OctetStreamWithPdfExtension_Fails()
    {
        Assert.False(UploadRules.IsAllowed("application/octet-stream", "foo.pdf", Pdf));
    }

    [Fact]
    public void IsAllowed_PdfMimeWithTxtExtension_Fails()
    {
        Assert.False(UploadRules.IsAllowed("application/pdf", "foo.txt", Pdf));
    }

    [Fact]
    public void IsAllowed_PdfMimeAndExtWithoutMagic_Fails()
    {
        Assert.False(UploadRules.IsAllowed("application/pdf", "contrato.pdf", Encoding.UTF8.GetBytes("not a pdf")));
    }

    [Fact]
    public void IsAllowed_PngMagic_Passes()
    {
        Assert.True(UploadRules.IsAllowed("image/png", "scan.png", Png));
    }
}
