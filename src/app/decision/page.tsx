"use client";
import { useState } from "react";
import Link from "next/link";
import { decisions, documents, findDecisionByDocumentId } from "@/lib/mocks";
import { DecisionStatusBadge } from "@/components/StatusBadge";

const fmtDate = (iso: string) => new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" });

export default function DecisionPage({ searchParams }: { searchParams: { docId?: string } }) {
  const target = searchParams.docId ? findDecisionByDocumentId(searchParams.docId) : decisions[0];
  const [confirmReplay, setConfirmReplay] = useState(false);
  if (!target) {
    return (
      <>
        <div className="page-header"><div><h2>Decisão</h2></div></div>
        <div className="card">Nenhuma decisão encontrada.</div>
      </>
    );
  }
  const doc = documents.find((d) => d.documentId === target.documentId);

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Decisão</h2>
          <div className="subtitle">
            <code>{target.decisionId}</code> · documento <Link href={`/documents/${target.documentId}`}><code>{target.documentId}</code></Link>
            {doc && <> · {doc.razaoSocial} ({doc.cnpj})</>}
          </div>
        </div>
        <button
          className="btn btn--ghost"
          onClick={() => setConfirmReplay((v) => !v)}
          aria-expanded={confirmReplay}
        >
          {confirmReplay ? "Cancelar" : "Replay (determinístico)"}
        </button>
      </div>

      {confirmReplay && (
        <div className="banner banner--warn" role="alertdialog" aria-label="Confirmar replay">
          ⚠️ Replay reexecuta a decisão usando o snapshot original (regras, canônico, dados oficiais).
          <button className="btn btn--primary" style={{ marginLeft: "auto", marginRight: 8 }}>Confirmar Replay</button>
          <button className="btn btn--ghost" onClick={() => setConfirmReplay(false)}>Cancelar</button>
        </div>
      )}

      <div className={`banner ${target.status === "APROVADO" ? "banner--ok" : target.status === "REPROVADO" ? "banner--err" : "banner--warn"}`}>
        {target.status === "APROVADO" ? "✅" : target.status === "REPROVADO" ? "❌" : "⚠️"} Decisão: <strong style={{ marginLeft: 8 }}>{target.status}</strong>
        <span style={{ marginLeft: "auto", fontWeight: 400, fontSize: 13 }}>{fmtDate(target.evaluatedAt)} · {target.latencyMs}ms</span>
      </div>

      <div className="grid-2">
        <div className="card">
          <h3>Operação solicitada</h3>
          <p style={{ fontSize: 16, margin: "0 0 12px 0" }}>{target.operacao}</p>
          <h3>Signatários informados</h3>
          <ul style={{ margin: 0, paddingLeft: 20 }}>
            {target.signatariosSolicitados.map((s, i) => (<li key={i}>{s}</li>))}
          </ul>
        </div>
        <div className="card">
          <h3>Versões usadas (snapshot para replay)</h3>
          <table>
            <tbody>
              <tr><th>Regras</th><td><code>{target.versions.rules}</code></td></tr>
              <tr><th>Canônico</th><td><code>{target.versions.canonical}</code></td></tr>
              <tr><th>Prompt LLM</th><td><code>{target.versions.aiPrompt}</code></td></tr>
              <tr><th>Modelo LLM</th><td><code>{target.versions.aiModel}</code></td></tr>
            </tbody>
          </table>
        </div>
      </div>

      <div className="card">
        <h3>Por que essa decisão? — motivos ({target.motivos.length})</h3>
        <ol style={{ margin: 0, paddingLeft: 20, lineHeight: 1.8 }}>
          {target.motivos.map((m, i) => (<li key={i}>{m}</li>))}
        </ol>
      </div>

      <div className="card">
        <h3>Evidências ({target.evidencias.length})</h3>
        {target.evidencias.map((e, i) => (
          <div key={i} style={{ padding: 12, marginBottom: 8, borderLeft: `4px solid ${e.type === "documento" ? "var(--color-brand-secondary)" : "var(--color-status-approved)"}`, background: "var(--color-bg-emphasis)", borderRadius: 4 }}>
            <div style={{ fontSize: 12, color: "var(--color-text-secondary)", marginBottom: 4 }}>
              {e.type === "documento" ? `📄 Documento — pg ${e.trace?.page}, offset ${e.trace?.offsetStart}–${e.trace?.offsetEnd}` : `🏛️ ${e.fonte}`}
            </div>
            <div style={{ fontSize: 14 }}>{e.detalhe}</div>
            {e.trace?.snippet && (
              <div style={{ marginTop: 8, padding: 8, background: "white", fontSize: 13, fontStyle: "italic", color: "var(--color-text-secondary)" }}>"{e.trace.snippet}"</div>
            )}
          </div>
        ))}
      </div>

      <div className="card">
        <h3>Outras decisões recentes</h3>
        <table>
          <thead><tr><th>ID</th><th>CNPJ</th><th>Operação</th><th>Status</th><th>Avaliada em</th><th>Latência</th></tr></thead>
          <tbody>
            {decisions.map((d) => (
              <tr key={d.decisionId}>
                <td><Link href={`/decision?docId=${d.documentId}`}><code>{d.decisionId}</code></Link></td>
                <td>{d.cnpj}</td>
                <td>{d.operacao}</td>
                <td><DecisionStatusBadge status={d.status} /></td>
                <td>{fmtDate(d.evaluatedAt)}</td>
                <td>{d.latencyMs}ms</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}
