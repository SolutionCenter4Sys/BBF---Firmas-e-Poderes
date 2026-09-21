using BbfFirmasPoderes.Domain.Enums;
using BbfFirmasPoderes.Domain.Pii;

namespace BbfFirmasPoderes.Tests;

public class DomainModelTests
{
    [Fact]
    public void DocStatus_Names_MatchFrontMocks()
    {
        string[] expected =
        [
            "pendente",
            "processando_ocr",
            "processando_iagen",
            "processando_ner",
            "canonico_pronto",
            "validacao_oficial",
            "decidido",
            "revisao_humana",
            "falha"
        ];

        var actual = Enum.GetNames<DocStatus>();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DecisionStatus_Names_MatchFrontMocks()
    {
        Assert.Equal(["APROVADO", "REPROVADO", "MANUAL"], Enum.GetNames<DecisionStatus>());
    }

    [Fact]
    public void PiiMask_Cpf_HidesMiddleDigits()
    {
        Assert.Equal("111.222.***-44", PiiMask.Cpf("111.222.333-44"));
    }

    [Fact]
    public void PiiMask_Cnpj_KeepsPrefixAndCheckDigits()
    {
        Assert.Equal("12.***.***/****-90", PiiMask.Cnpj("12.345.678/0001-90"));
    }

    [Fact]
    public void PiiMask_Cpf_AlreadyMasked_PassesThrough()
    {
        Assert.Equal("111.222.***-44", PiiMask.Cpf("111.222.***-44"));
    }

    [Fact]
    public void PiiMask_InText_MasksCpfAndCnpj()
    {
        var raw = "CPF 111.222.333-44 CNPJ 12.345.678/0001-90";
        var masked = PiiMask.InText(raw);

        Assert.Contains("111.222.***-44", masked);
        Assert.Contains("12.***.***/****-90", masked);
        Assert.DoesNotContain("111.222.333-44", masked);
        Assert.DoesNotContain("12.345.678/0001-90", masked);
    }
}
