"use client";

import { FormEvent, useState } from "react";
import { JwtTokenForm } from "@/components/JwtTokenForm";
import type { DecisionRecord } from "@/domain";
import { getAuthorityDecision } from "@/hooks/useDecision";
import { ApiError, getApiBaseUrl } from "@/lib/api";

export default function ApiHelperPage() {
  const [cnpj, setCnpj] = useState("12.345.678/0001-90");
  const [operation, setOperation] = useState("movimentacao_financeira");
  const [signers, setSigners] = useState("p1");
  const [valor, setValor] = useState("500000");
  const [idempotencyKey, setIdempotencyKey] = useState("");
  const [busy, setBusy] = useState(false);
  const [unauthorized, setUnauthorized] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<DecisionRecord | null>(null);
  const [httpStatus, setHttpStatus] = useState<number | null>(null);

  const apiBase = getApiBaseUrl();
  const swaggerHref = `${apiBase}/swagger`;

  const curlSnippet = `curl -X GET '${apiBase}/v1/authority/decision?cnpj=${encodeURIComponent(cnpj)}&operation=${operation}&signers=${encodeURIComponent(signers)}${valor.trim() ? `&valor=${encodeURIComponent(valor.trim())}` : ""}' \\
  -H 'Authorization: Bearer <token-oauth2>' \\
  -H 'X-Correlation-Id: corr_<uuid>'${idempotencyKey.trim() ? ` \\\n  -H 'Idempotency-Key: ${idempotencyKey.trim()}'` : ""}`;

  const jsSnippet = `const params = new URLSearchParams({
  cnpj: '${cnpj}',
  operation: '${operation}',
  signers: '${signers}'
});

const res = await fetch('${apiBase}/v1/authority/decision?' + params, {
  headers: { Authorization: 'Bearer <token>' }
});
const result = await res.json();
console.log(result.status); // APROVADO | REPROVADO | MANUAL`;

  const javaSnippet = `DecisionResult result = client.authority().decide(
    DecisionRequest.builder()
        .cnpj("${cnpj}")
        .operation("${operation}")
        .signers(List.of(${signers.split(",").map((s) => `"${s.trim()}"`).filter(Boolean).join(", ")}))
        .build()
);

System.out.println(result.getStatus());`;

  const run = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setHttpStatus(null);
    try {
      const parsedValor = Number(valor.replace(",", "."));
      const record = await getAuthorityDecision({
        cnpj: cnpj.trim(),
        operation,
        signers: signers.trim(),
        valor: Number.isFinite(parsedValor) ? parsedValor : undefined,
        idempotencyKey: idempotencyKey.trim() || undefined
      });
      setResult(record);
      setHttpStatus(200);
      setUnauthorized(false);
    } catch (err) {
      setResult(null);
      if (err instanceof ApiError) {
        setHttpStatus(err.status);
        if (err.status === 401) setUnauthorized(true);
        setError(err.message);
      } else {
        setError(err instanceof Error ? err.message : "Falha na chamada.");
      }
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      <div className="page-header">
        <div>
          <h2>API Helper</h2>
          <div className="subtitle">Teste o endpoint <code>GET /v1/authority/decision</code> contra a API .NET</div>
        </div>
        <a href={swaggerHref} className="btn btn--secondary" target="_blank" rel="noreferrer">Abrir Swagger</a>
      </div>

      {unauthorized && (
        <JwtTokenForm
          roleLabel="consumer / operador"
          hint="GET /v1/authority/decision exige Bearer consumer, operador ou admin."
          onSaved={() => {
            setUnauthorized(false);
            setError(null);
          }}
        />
      )}

      {error && (
        <div className="banner banner--err" role="alert">
          ✕ {httpStatus ? `${httpStatus} · ` : ""}{error}
        </div>
      )}

      <div className="grid-2">
        <form className="card" onSubmit={run}>
          <h3>Parâmetros</h3>
          <div style={{ display: "grid", gap: 12 }}>
            <div><label htmlFor="helper-cnpj">CNPJ</label><input id="helper-cnpj" className="input" value={cnpj} onChange={(e) => setCnpj(e.target.value)} /></div>
            <div>
              <label htmlFor="helper-op">Operação</label>
              <select id="helper-op" className="input" value={operation} onChange={(e) => setOperation(e.target.value)}>
                <option value="movimentacao_financeira">movimentacao_financeira</option>
                <option value="contratacao_credito">contratacao_credito</option>
                <option value="abertura_conta">abertura_conta</option>
                <option value="emissao_procuracao">emissao_procuracao</option>
              </select>
            </div>
            <div><label htmlFor="helper-signers">Signatários (IDs separados por vírgula)</label><input id="helper-signers" className="input" value={signers} onChange={(e) => setSigners(e.target.value)} /></div>
            <div><label htmlFor="helper-valor">valor (opcional)</label><input id="helper-valor" className="input" type="number" min={0} value={valor} onChange={(e) => setValor(e.target.value)} /></div>
            <div><label htmlFor="helper-idem">Idempotency-Key (opcional, 24 h)</label><input id="helper-idem" className="input" value={idempotencyKey} onChange={(e) => setIdempotencyKey(e.target.value)} placeholder="chave-unica" /></div>
            <button type="submit" className="btn btn--primary" style={{ marginTop: 8 }} disabled={busy}>
              {busy ? "Executando…" : "▶ Executar GET /v1/authority/decision"}
            </button>
          </div>
        </form>

        <div className="card">
          <h3>Resposta {httpStatus ? `(${httpStatus})` : ""}</h3>
          <pre>{result ? JSON.stringify(result, null, 2) : "Ainda sem chamada. ACME: cnpj 12.345.678/0001-90, signers p1 → APROVADO."}</pre>
        </div>
      </div>

      <div className="card">
        <h3>cURL</h3>
        <pre>{curlSnippet}</pre>
      </div>

      <div className="grid-2">
        <div className="card">
          <h3>JavaScript</h3>
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
          <li><strong>Autenticação:</strong> OAuth2 client credentials para parceiros, mTLS para canais corporativos internos. Dev: JWT HS256 em <code>bbf.access_token</code>.</li>
          <li><strong>Rate limit:</strong> 429 + <code>Retry-After</code> em <code>/v1/authority/decision</code> (<code>RateLimiting:PermitLimit</code>).</li>
          <li><strong>Idempotência:</strong> header <code>Idempotency-Key</code> (janela 24 h, mesmo <code>decisionId</code>).</li>
          <li><strong>Auditoria:</strong> cada chamada registra <code>correlationId</code> na trilha <code>GET /v1/audit/trail</code>.</li>
        </ul>
      </div>
    </>
  );
}
