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
    private static readonly byte[] Zip = [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00];
    private static readonly byte[] Ole =
        [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

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

    [Theory]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "contrato.docx")]
    [InlineData("application/vnd.oasis.opendocument.text", "contrato.odt")]
    public void IsAllowed_ZipBasedOfficeDocument_Passes(string mime, string fileName)
    {
        Assert.True(UploadRules.IsAllowed(mime, fileName, Zip));
    }

    [Fact]
    public void IsAllowed_LegacyWordDocument_Passes()
    {
        Assert.True(UploadRules.IsAllowed("application/msword", "contrato.doc", Ole));
    }

    [Theory]
    [InlineData("application/rtf")]
    [InlineData("text/rtf")]
    public void IsAllowed_RtfDocument_Passes(string mime)
    {
        Assert.True(UploadRules.IsAllowed(mime, "contrato.rtf", Encoding.ASCII.GetBytes(@"{\rtf1\ansi")));
    }

    [Fact]
    public void IsAllowed_DocxWithWrongMagic_Fails()
    {
        Assert.False(UploadRules.IsAllowed(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "contrato.docx",
            Pdf));
    }
}
