"use client";

import { FormEvent, Suspense, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { JwtTokenForm } from "@/components/JwtTokenForm";
import type { DecisionRecord } from "@/domain";
import { evaluateDecision, replayDecision } from "@/hooks/useDecision";
import { listDocuments, type DocumentListItem } from "@/hooks/useDocuments";
import { ApiError } from "@/lib/api";

const fmtDate = (iso: string) => new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" });

function DecisionWorkspace() {
  const searchParams = useSearchParams();
  const docIdParam = searchParams.get("docId") ?? "";
  const decisionIdParam = searchParams.get("decisionId") ?? "";

  const [documentId, setDocumentId] = useState(docIdParam || "doc_001");
  const [cnpj, setCnpj] = useState("12.345.678/0001-90");
  const [operacao, setOperacao] = useState("movimentacao_financeira");
  const [valorOperacao, setValorOperacao] = useState("500000");
  const [signatarios, setSignatarios] = useState("João da Silva (Diretor)");
  const [confirmReplay, setConfirmReplay] = useState(false);
  const [busy, setBusy] = useState(false);
  const [unauthorized, setUnauthorized] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [record, setRecord] = useState<DecisionRecord | null>(null);
  const [recent, setRecent] = useState<DocumentListItem[]>([]);

  useEffect(() => {
    if (docIdParam) setDocumentId(docIdParam);
  }, [docIdParam]);

  const loadRecent = async () => {
    try {
      const [decidido, revisao] = await Promise.all([
        listDocuments("decidido"),
        listDocuments("revisao_humana")
      ]);
      const merged = [...decidido, ...revisao].sort(
        (a, b) => new Date(b.uploadedAt).getTime() - new Date(a.uploadedAt).getTime()
      );
      setRecent(merged);
      setUnauthorized(false);
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        setUnauthorized(true);
        setRecent([]);
      }
    }
  };

  useEffect(() => {
    void loadRecent();
  }, []);

  useEffect(() => {
    if (!decisionIdParam) return;
    let cancelled = false;
    (async () => {
      setBusy(true);
      setError(null);
      try {
        const replayed = await replayDecision(decisionIdParam);
        if (!cancelled) {
          setRecord(replayed);
          setUnauthorized(false);
        }
      } catch (err) {
        if (cancelled) return;
        if (err instanceof ApiError && err.status === 401) {
          setUnauthorized(true);
        }
        setError(err instanceof Error ? err.message : "Falha no replay.");
      } finally {
        if (!cancelled) setBusy(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [decisionIdParam]);

  const signers = useMemo(
    () => signatarios.split(",").map((s) => s.trim()).filter((s) => s.length > 0),
    [signatarios]
  );

  const runEvaluate = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const valor = Number(valorOperacao.replace(",", "."));
      const next = await evaluateDecision({
        documentId: documentId.trim(),
        cnpj: cnpj.trim() || undefined,
        operacao: operacao.trim(),
        valorOperacao: Number.isFinite(valor) ? valor : undefined,
        currency: "BRL",
        signatariosSolicitados: signers,
        asOf: new Date().toISOString()
      });
      setRecord(next);
      setUnauthorized(false);
      setConfirmReplay(false);
      await loadRecent();
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) setUnauthorized(true);
      setError(err instanceof Error ? err.message : "Falha ao avaliar.");
    } finally {
      setBusy(false);
    }
  };

  const runReplay = async () => {
    if (!record) return;
    setBusy(true);
    setError(null);
    try {
      const next = await replayDecision(record.decisionId);
      setRecord(next);
      setConfirmReplay(false);
      setUnauthorized(false);
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) setUnauthorized(true);
      setError(err instanceof Error ? err.message : "Falha no replay.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Decisão</h2>
          <div className="subtitle">
            POST <code>/v1/decision/evaluate</code> · replay <code>/v1/decision/{"{id}"}/replay</code>
            {record && (
              <>
                {" "}· <code>{record.decisionId}</code> · documento{" "}
                <Link href={`/documents/${record.documentId}`}><code>{record.documentId}</code></Link>
              </>
            )}
          </div>
        </div>
        {record && (
          <button
            type="button"
            className="btn btn--ghost"
            onClick={() => setConfirmReplay((v) => !v)}
            aria-expanded={confirmReplay}
            disabled={busy}
          >
            {confirmReplay ? "Cancelar" : "Replay (determinístico)"}
          </button>
        )}
      </div>

      {unauthorized && (
        <JwtTokenForm
          roleLabel="operador"
          hint="POST /v1/decision/evaluate exige Bearer operador/admin. Cole o JWT em sessionStorage (bbf.access_token)."
          onSaved={async () => {
            setUnauthorized(false);
            await loadRecent();
          }}
        />
      )}

      {error && (
        <div className="banner banner--err" role="alert">✕ {error}</div>
      )}

      {confirmReplay && record && (
        <div className="banner banner--warn" role="alertdialog" aria-label="Confirmar replay">
          Replay devolve o snapshot persistido. Sem reconsulta de fonte oficial.
          <button type="button" className="btn btn--primary" style={{ marginLeft: "auto", marginRight: 8 }} onClick={() => void runReplay()} disabled={busy}>
            Confirmar Replay
          </button>
          <button type="button" className="btn btn--ghost" onClick={() => setConfirmReplay(false)}>Cancelar</button>
        </div>
      )}

      <form className="card" onSubmit={runEvaluate}>
        <h3>Avaliar (interno)</h3>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
          <div>
            <label htmlFor="decision-doc">documentId</label>
            <input id="decision-doc" className="input" value={documentId} onChange={(e) => setDocumentId(e.target.value)} required />
          </div>
          <div>
            <label htmlFor="decision-cnpj">CNPJ</label>
            <input id="decision-cnpj" className="input" value={cnpj} onChange={(e) => setCnpj(e.target.value)} />
          </div>
          <div>
            <label htmlFor="decision-op">operacao</label>
            <select id="decision-op" className="input" value={operacao} onChange={(e) => setOperacao(e.target.value)}>
              <option value="movimentacao_financeira">movimentacao_financeira</option>
              <option value="contratacao_credito">contratacao_credito</option>
              <option value="abertura_conta">abertura_conta</option>
              <option value="emissao_procuracao">emissao_procuracao</option>
            </select>
          </div>
          <div>
            <label htmlFor="decision-valor">valorOperacao</label>
            <input id="decision-valor" className="input" type="number" min={0} value={valorOperacao} onChange={(e) => setValorOperacao(e.target.value)} />
          </div>
          <div style={{ gridColumn: "1 / -1" }}>
            <label htmlFor="decision-signers">signatariosSolicitados (vírgula)</label>
            <input id="decision-signers" className="input" value={signatarios} onChange={(e) => setSignatarios(e.target.value)} required />
          </div>
        </div>
        <button type="submit" className="btn btn--primary" style={{ marginTop: 12 }} disabled={busy || unauthorized}>
          {busy ? "Avaliando…" : "Avaliar"}
        </button>
      </form>

      {record && (
        <>
          <div className={`banner ${record.status === "APROVADO" ? "banner--ok" : record.status === "REPROVADO" ? "banner--err" : "banner--warn"}`}>
            {record.status === "APROVADO" ? "✅" : record.status === "REPROVADO" ? "❌" : "⚠️"} Decisão: <strong style={{ marginLeft: 8 }}>{record.status}</strong>
            <span style={{ marginLeft: "auto", fontWeight: 400, fontSize: 13 }}>{fmtDate(record.evaluatedAt)} · {record.latencyMs}ms</span>
          </div>

          <div className="grid-2">
            <div className="card">
              <h3>Operação solicitada</h3>
              <p style={{ fontSize: 16, margin: "0 0 12px 0" }}>{record.operacao}</p>
              <h3>Signatários informados</h3>
              <ul style={{ margin: 0, paddingLeft: 20 }}>
                {record.signatariosSolicitados.map((s, i) => (<li key={`${s}-${i}`}>{s}</li>))}
              </ul>
            </div>
            <div className="card">
              <h3>Versões usadas (snapshot para replay)</h3>
              <table>
                <tbody>
                  <tr><th>Regras</th><td><code>{record.versions.rules}</code></td></tr>
                  <tr><th>Canônico</th><td><code>{record.versions.canonical}</code></td></tr>
                  <tr><th>Prompt LLM</th><td><code>{record.versions.aiPrompt}</code></td></tr>
                  <tr><th>Modelo LLM</th><td><code>{record.versions.aiModel}</code></td></tr>
                </tbody>
              </table>
            </div>
          </div>

          <div className="card">
            <h3>Por que essa decisão? — motivos ({record.motivos.length})</h3>
            <ol style={{ margin: 0, paddingLeft: 20, lineHeight: 1.8 }}>
              {record.motivos.map((m, i) => (<li key={i}>{m}</li>))}
            </ol>
          </div>

          <div className="card">
            <h3>Evidências ({record.evidencias.length})</h3>
            {record.evidencias.map((e, i) => (
              <div key={i} style={{ padding: 12, marginBottom: 8, borderLeft: `4px solid ${e.type === "documento" ? "var(--color-brand-secondary)" : "var(--color-status-approved)"}`, background: "var(--color-bg-emphasis)", borderRadius: 4 }}>
                <div style={{ fontSize: 12, color: "var(--color-text-secondary)", marginBottom: 4 }}>
                  {e.type === "documento" ? `📄 Documento — pg ${e.trace?.page}, offset ${e.trace?.offsetStart}–${e.trace?.offsetEnd}` : `🏛️ ${e.fonte}`}
                </div>
                <div style={{ fontSize: 14 }}>{e.detalhe}</div>
                {e.trace?.snippet && (
                  <div style={{ marginTop: 8, padding: 8, background: "white", fontSize: 13, fontStyle: "italic", color: "var(--color-text-secondary)" }}>&quot;{e.trace.snippet}&quot;</div>
                )}
              </div>
            ))}
          </div>
        </>
      )}

      <div className="card">
        <h3>Documentos decididos / revisão ({recent.length})</h3>
        <p style={{ fontSize: 13, color: "var(--color-text-secondary)", marginTop: 0 }}>
          GET <code>/v1/documents?status=decidido</code> e <code>status=revisao_humana</code>
        </p>
        <table>
          <thead><tr><th>Documento</th><th>CNPJ</th><th>Razão</th><th>Status</th><th>Enviado</th></tr></thead>
          <tbody>
            {recent.map((d) => (
              <tr key={d.documentId}>
                <td><Link href={`/decision?docId=${d.documentId}`}><code>{d.documentId}</code></Link></td>
                <td>{d.cnpj ?? "—"}</td>
                <td>{d.razaoSocial ?? "—"}</td>
                <td>{d.status}</td>
                <td>{fmtDate(d.uploadedAt)}</td>
              </tr>
            ))}
            {recent.length === 0 && (
              <tr>
                <td colSpan={5} style={{ textAlign: "center", padding: 24, color: "var(--color-text-secondary)" }}>
                  {unauthorized ? "Informe um JWT para listar documentos." : "Nenhum documento decidido ou em revisão humana."}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}

export default function DecisionPage() {
  return (
    <Suspense fallback={<div className="card">Carregando decisão…</div>}>
      <DecisionWorkspace />
    </Suspense>
  );
}
