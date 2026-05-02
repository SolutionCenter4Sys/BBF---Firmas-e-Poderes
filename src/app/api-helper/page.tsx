"use client";
import { useState } from "react";

const samplePayload = {
  status: "APROVADO",
  motivos: [
    "Operação dentro do limite de R$ 500.000 com modo conjunto Diretor + Procurador (RN02 + RN03 v1.2.0).",
    "Sócios João da Silva e Maria Souza confirmados como ATIVOS na Junta Comercial em 2026-04-29 14:22 UTC.",
    "Procuração vigente (validFrom 2024-01-01)."
  ],
  limite: 500000,
  evidencias: [
    { type: "documento", page: 4, snippet: "...assinatura conjunta de Diretor e Procurador..." },
    { type: "fonte_oficial", fonte: "Junta Comercial SP", detalhe: "Sócios ativos" }
  ],
  versions: { rules: "1.2.0", canonical: "1.0.0", aiPrompt: "leitura-contrato-social@2.1.0", aiModel: "gemini-1.5-pro" },
  decisionId: "dec_001",
  evaluatedAt: "2026-04-29T14:25:32Z",
  correlationId: "corr_a1b2c3"
};

export default function ApiHelperPage() {
  const [cnpj, setCnpj] = useState("12.345.678/0001-90");
  const [operation, setOperation] = useState("movimentacao_financeira");
  const [signers, setSigners] = useState("p1,p2");

  const curlSnippet = `curl -X GET 'https://api.bbf.bradesco.com.br/v1/authority/decision?cnpj=${encodeURIComponent(cnpj)}&operation=${operation}&signers=${signers}' \\
  -H 'Authorization: Bearer <token-oauth2>' \\
  -H 'X-Correlation-Id: corr_<uuid>' \\
  -H 'Idempotency-Key: <chave-unica>'`;

  const jsSnippet = `import { decision } from '@bbf/firmas-poderes-sdk';

const result = await decision.evaluate({
  cnpj: '${cnpj}',
  operation: '${operation}',
  signers: ['${signers.split(",").join("', '")}']
});

console.log(result.status); // APROVADO | REPROVADO | MANUAL`;

  const javaSnippet = `import br.com.bradesco.bbf.firmaspoderes.client.*;

DecisionResult result = client.decisions().evaluate(
    DecisionRequest.builder()
        .cnpj("${cnpj}")
        .operation("${operation}")
        .signers(List.of(${signers.split(",").map((s) => `"${s.trim()}"`).join(", ")}))
        .build()
);

System.out.println(result.getStatus());`;

  return (
    <>
      <div className="page-header">
        <div>
          <h2>API Helper</h2>
          <div className="subtitle">Teste o endpoint <code>GET /v1/authority/decision</code> sem sair da plataforma</div>
        </div>
        <a href="#" className="btn btn--secondary">Abrir Swagger</a>
      </div>

      <div className="grid-2">
        <div className="card">
          <h3>Parâmetros</h3>
          <div style={{ display: "grid", gap: 12 }}>
            <div><label>CNPJ</label><input className="input" value={cnpj} onChange={(e) => setCnpj(e.target.value)} /></div>
            <div>
              <label>Operação</label>
              <select className="input" value={operation} onChange={(e) => setOperation(e.target.value)}>
                <option value="movimentacao_financeira">movimentacao_financeira</option>
                <option value="contratacao_credito">contratacao_credito</option>
                <option value="abertura_conta">abertura_conta</option>
                <option value="emissao_procuracao">emissao_procuracao</option>
              </select>
            </div>
            <div><label>Signatários (IDs separados por vírgula)</label><input className="input" value={signers} onChange={(e) => setSigners(e.target.value)} /></div>
            <button className="btn btn--primary" style={{ marginTop: 8 }}>▶ Executar (mock)</button>
          </div>
        </div>

        <div className="card">
          <h3>Resposta esperada</h3>
          <pre>{JSON.stringify(samplePayload, null, 2)}</pre>
        </div>
      </div>

      <div className="card">
        <h3>cURL</h3>
        <pre>{curlSnippet}</pre>
      </div>

      <div className="grid-2">
        <div className="card">
          <h3>JavaScript (SDK)</h3>
          <pre>{jsSnippet}</pre>
        </div>
        <div className="card">
          <h3>Java (SDK)</h3>
          <pre>{javaSnippet}</pre>
        </div>
      </div>

      <div className="card">
        <h3>Notas de adoção</h3>
        <ul style={{ lineHeight: 1.8 }}>
          <li><strong>Versionamento:</strong> URI <code>/v1/...</code> — política SemVer com janela de deprecation de 6 meses.</li>
          <li><strong>Autenticação:</strong> OAuth2 client credentials para parceiros, mTLS para canais corporativos internos.</li>
          <li><strong>Rate limit:</strong> 100 req/min por consumidor (default). Solicitar aumento via portal interno.</li>
          <li><strong>Idempotência:</strong> use o header <code>Idempotency-Key</code> para retries seguros.</li>
          <li><strong>Auditoria:</strong> cada chamada é registrada com correlationId para rastreabilidade ponta-a-ponta.</li>
        </ul>
      </div>
    </>
  );
}
