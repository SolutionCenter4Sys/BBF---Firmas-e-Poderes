"use client";
import { useEffect, useState } from "react";
import Link from "next/link";
import { loadKasResult, type KasRunResult } from "@/lib/kas-result";

function JsonNode({ value, name }: { value: unknown; name?: string }) {
  if (value === null || typeof value !== "object") {
    const text = typeof value === "string" ? `"${value}"` : String(value);
    return (
      <div className="json-row">
        {name !== undefined && <span className="json-key">{name}: </span>}
        <span className="json-leaf">{text}</span>
      </div>
    );
  }

  const entries = Array.isArray(value)
    ? value.map((v, i) => [String(i), v] as const)
    : Object.entries(value);

  return (
    <details className="json-block" open>
      <summary>
        {name !== undefined ? <span className="json-key">{name}</span> : null}
        <span className="json-meta">{Array.isArray(value) ? `Array(${value.length})` : "Object"}</span>
      </summary>
      <div className="json-children">
        {entries.map(([k, v]) => (
          <JsonNode key={k} name={k} value={v} />
        ))}
      </div>
    </details>
  );
}

export default function KasResultPage() {
  const [result, setResult] = useState<KasRunResult | null>(null);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    setResult(loadKasResult());
  }, []);

  const copy = async () => {
    if (!result) return;
    const proxy = result.body as { body?: unknown } | null;
    const kasBody = proxy && typeof proxy === "object" && "body" in proxy ? proxy.body : result.body;
    await navigator.clipboard.writeText(JSON.stringify(kasBody, null, 2));
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  if (!result) {
    return (
      <>
        <div className="page-header">
          <div>
            <h2>Retorno KAAS</h2>
            <div className="subtitle">Nenhuma execução nesta sessão</div>
          </div>
          <Link href="/" className="btn btn--primary">Enviar documento</Link>
        </div>
        <div className="card">
          <p style={{ margin: 0, color: "var(--color-text-secondary)" }}>
            Faça upload no Dashboard. O JSON da jornada <code>testes-firmas-e-poderes</code> aparece aqui.
          </p>
        </div>
      </>
    );
  }

  const proxy = result.body as {
    ok?: boolean;
    kasStatus?: number;
    durationMs?: number;
    fileName?: string;
    fileSize?: number;
    error?: string;
    detail?: string;
    body?: unknown;
  } | null;
  const kasBody = proxy && typeof proxy === "object" && "body" in proxy ? proxy.body : result.body;
  const kasStatus = typeof proxy?.kasStatus === "number" ? proxy.kasStatus : result.httpStatus;
  const durationMs = typeof proxy?.durationMs === "number" ? proxy.durationMs : null;

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Retorno KAAS</h2>
          <div className="subtitle">
            Jornada <code>testes-firmas-e-poderes</code> · modo <code>sync</code>
          </div>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <Link href="/" className="btn btn--ghost">← Dashboard</Link>
          <button type="button" className="btn btn--secondary" onClick={copy}>
            {copied ? "Copiado" : "Copiar JSON"}
          </button>
        </div>
      </div>

      <div className={`banner ${result.ok ? "banner--ok" : "banner--err"}`} role="status">
        {result.ok ? "✓" : "✕"} HTTP {kasStatus}
        {result.ok ? " — jornada concluída (sync)" : ` — ${proxy?.error || proxy?.detail || "a API retornou erro"}`}
      </div>

      <div className="grid-3">
        <div className="metric-card">
          <div className="metric-label">Arquivo</div>
          <div className="metric-value" style={{ fontSize: 16 }}>{result.fileName}</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: result.ok ? "var(--color-status-approved)" : "var(--color-status-rejected)" }}>
          <div className="metric-label">Status KAAS</div>
          <div className="metric-value">{kasStatus}</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-info)" }}>
          <div className="metric-label">{durationMs != null ? "Duração" : "Quando"}</div>
          <div className="metric-value" style={{ fontSize: 16 }}>
            {durationMs != null ? `${(durationMs / 1000).toFixed(1)}s` : new Date(result.at).toLocaleString("pt-BR")}
          </div>
        </div>
      </div>

      <div className="card">
        <h3>Corpo da resposta</h3>
        {kasBody && typeof kasBody === "object" ? (
          <JsonNode value={kasBody} />
        ) : (
          <pre>{kasBody == null ? "(vazio)" : String(kasBody)}</pre>
        )}
      </div>

      <div className="card">
        <h3>JSON bruto</h3>
        <pre>{JSON.stringify(kasBody, null, 2)}</pre>
      </div>
    </>
  );
}
