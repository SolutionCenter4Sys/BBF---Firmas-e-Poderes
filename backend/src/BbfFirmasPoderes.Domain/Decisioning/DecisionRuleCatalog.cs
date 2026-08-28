namespace BbfFirmasPoderes.Domain.Decisioning;

/// <summary>
/// Regras mínimas WF-09, alinhadas a <c>dmnRules</c> de <c>src/lib/mocks.ts</c>.
/// Motor puro (sem I/O). Junta Comercial real = fora de escopo. Circuit breaker stub = WF-11.
/// </summary>
/// <remarks>
/// <para><b>RN01</b> — cada signatário existe no canônico e está <c>ativo</c>.
/// Status vem de <c>Person.Status</c> (seed/KAAS), não de consulta oficial.</para>
/// <para><b>RN02</b> — modo de assinatura cobre a operação: <c>isolada</c> (um cargo basta)
/// vs <c>conjunta</c> (n de m qualificações). ACME: Diretor isolado até o limite;
/// Diretor + Procurador no poder conjunto.</para>
/// <para><b>RN03</b> — valor da operação ≤ <c>Power.LimiteValue</c> do poder que casou o modo.
/// ACME: isolada R$ 500.000; conjunta R$ 5.000.000.</para>
/// <para><b>RN04</b> — poder vigente em <c>asOf</c> (<c>VigenciaFrom</c> / <c>VigenciaTo</c>).</para>
/// <para><b>TH01</b> — min(OCR, IAGen, NER) &lt; 0,75 → <c>MANUAL</c> (não consulta fonte oficial).</para>
/// <para>RN05/RN06 do mock estão <c>em_revisao</c>/<c>proposta</c> — fora deste motor.</para>
/// </remarks>
public static class DecisionRuleCatalog
{
    public const string Rn01 = "RN01";
    public const string Rn02 = "RN02";
    public const string Rn03 = "RN03";
    public const string Rn04 = "RN04";
    public const string Th01 = "TH01";

    public const string RulesVersion = "1.2.0";
    public const string ThresholdVersion = "1.0.0";
    public const string CanonicalVersion = "1.0.0";
    public const string AiPrompt = "leitura-contrato-social@2.1.0";
    public const string AiModel = "gemini-1.5-pro";

    public const decimal ConfidenceThreshold = 0.75m;
    public const string DefaultCurrency = "BRL";
}
