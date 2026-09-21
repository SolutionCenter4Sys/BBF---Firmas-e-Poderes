namespace BbfFirmasPoderes.Domain.Enums;

/// <summary>
/// Espelha <c>DocStatus</c> de <c>src/lib/mocks.ts</c>. Persistido como varchar (nome do membro).
/// </summary>
public enum DocStatus
{
    pendente,
    processando_ocr,
    processando_iagen,
    processando_ner,
    canonico_pronto,
    validacao_oficial,
    decidido,
    revisao_humana,
    falha
}
