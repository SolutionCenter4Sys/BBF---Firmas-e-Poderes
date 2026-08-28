"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { JwtTokenForm } from "@/components/JwtTokenForm";
import type { DocStatus, ReviewMotivo } from "@/domain";
import { useDocuments, type DocumentListItem } from "@/hooks/useDocuments";

const fmtPct = (n: number) => `${(n * 100).toFixed(0)}%`;
const elapsedHours = (iso: string) => (Date.now() - new Date(iso).getTime()) / 3_600_000;

const motivoLabels: Record<ReviewMotivo, string> = {
  ocr_baixa_confianca: "OCR baixa confiança",
  iagen_baixa_confianca: "IA Gen baixa confiança",
  ner_baixa_confianca: "NER baixa confiança",
  clausula_ambigua: "Cláusula ambígua",
  qualidade_documento: "Qualidade do documento"
};

const QUEUE_STATUSES: readonly DocStatus[] = [
  "revisao_humana",
  "falha",
  "processando_ocr",
  "processando_iagen",
  "processando_ner",
  "canonico_pronto"
];

function isQueueStatus(value: string): value is DocStatus {
  return (QUEUE_STATUSES as readonly string[]).includes(value);
}

function deriveMotivo(doc: DocumentListItem): ReviewMotivo {
  if (doc.status === "falha") return "qualidade_documento";
  const ocr = doc.confianca?.ocr ?? 0;
  const iagen = doc.confianca?.iagen ?? 0;
  const ner = doc.confianca?.ner ?? 0;
  const scores: { motivo: ReviewMotivo; value: number }[] = [];
  if (ocr > 0) scores.push({ motivo: "ocr_baixa_confianca", value: ocr });
  if (iagen > 0) scores.push({ motivo: "iagen_baixa_confianca", value: iagen });
  if (ner > 0) scores.push({ motivo: "ner_baixa_confianca", value: ner });
  if (scores.length === 0) return "clausula_ambigua";
  const lowest = scores.reduce((min, cur) => (cur.value < min.value ? cur : min));
  return lowest.value < 0.75 ? lowest.motivo : "clausula_ambigua";
}

function criticalScore(doc: DocumentListItem): number {
  const values = [doc.confianca?.ocr, doc.confianca?.iagen, doc.confianca?.ner]
    .filter((n): n is number => typeof n === "number" && n > 0);
  return values.length > 0 ? Math.min(...values) : 0;
}

function prioridadeOf(score: number): "alta" | "media" | "baixa" {
  if (score < 0.7) return "alta";
  if (score < 0.85) return "media";
  return "baixa";
}

export default function ReviewQueuePage() {
  const [status, setStatus] = useState<DocStatus>("revisao_humana");
  const [filterPrioridade, setFilterPrioridade] = useState<string>("todas");
  const [filterMotivo, setFilterMotivo] = useState<string>("todos");
  const { items, loading, error, unauthorized, refresh } = useDocuments(status);

  const rows = useMemo(() => items.map((d) => {
    const score = criticalScore(d);
    const motivo = deriveMotivo(d);
    return {
      documentId: d.documentId,
      cnpj: d.cnpj ?? "—",
      razaoSocial: d.razaoSocial ?? "—",
      motivo,
      motivoLegivel: motivoLabels[motivo],
      scoreCritico: score,
      enfileiradoEm: d.uploadedAt,
      slaHoras: 4,
      prioridade: prioridadeOf(score)
    };
  }), [items]);

  const filtered = useMemo(() => rows.filter((r) => {
    if (filterPrioridade !== "todas" && r.prioridade !== filterPrioridade) return false;
    if (filterMotivo !== "todos" && r.motivo !== filterMotivo) return false;
    return true;
  }), [rows, filterPrioridade, filterMotivo]);

  const nextHref = filtered[0] ? `/documents/${filtered[0].documentId}` : null;

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Fila de Revisão Humana</h2>
          <div className="subtitle">GET <code>/v1/documents?status={status}</code> — baixa confiança / revisão</div>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          {nextHref
            ? <Link className="btn btn--primary" href={nextHref}>Atender próximo</Link>
            : <button type="button" className="btn btn--primary" disabled>Atender próximo</button>}
        </div>
      </div>

      {unauthorized && (
        <JwtTokenForm
          roleLabel="operador"
          hint="GET /v1/documents exige Bearer. Cole JWT operador em sessionStorage (bbf.access_token)."
          onSaved={() => void refresh()}
        />
      )}

      {error && !unauthorized && (
        <div className="banner banner--err" role="alert">✕ {error}</div>
      )}

      <div className="grid-3">
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-rejected)" }}>
          <div className="metric-label">Em fila</div>
          <div className="metric-value">{loading ? "…" : rows.length}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>{rows.filter((r) => r.prioridade === "alta").length} prioridade alta</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-manual)" }}>
          <div className="metric-label">SLA crítico (&lt; 1h)</div>
          <div className="metric-value">{rows.filter((r) => elapsedHours(r.enfileiradoEm) > r.slaHoras - 1).length}</div>
        </div>
        <div className="metric-card">
          <div className="metric-label">Filtro API</div>
          <div className="metric-value" style={{ fontSize: 18 }}>{status}</div>
        </div>
      </div>

      <div className="card">
        <h3>Filtros</h3>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: 12 }}>
          <div>
            <label htmlFor="review-status">Status (API)</label>
            <select id="review-status" className="input" value={status} onChange={(e) => {
              if (isQueueStatus(e.target.value)) setStatus(e.target.value);
            }}>
              {QUEUE_STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
            </select>
          </div>
          <div>
            <label htmlFor="review-prio">Prioridade</label>
            <select id="review-prio" className="input" value={filterPrioridade} onChange={(e) => setFilterPrioridade(e.target.value)}>
              <option value="todas">Todas</option>
              <option value="alta">Alta</option>
              <option value="media">Média</option>
              <option value="baixa">Baixa</option>
            </select>
          </div>
          <div>
            <label htmlFor="review-motivo">Motivo</label>
            <select id="review-motivo" className="input" value={filterMotivo} onChange={(e) => setFilterMotivo(e.target.value)}>
              <option value="todos">Todos</option>
              {Object.entries(motivoLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
        </div>
      </div>

      <div className="card">
        <h3>Itens em revisão ({loading ? "…" : filtered.length})</h3>
        <table>
          <thead>
            <tr>
              <th>Documento</th>
              <th>CNPJ / Razão</th>
              <th>Motivo</th>
              <th>Score crítico</th>
              <th>Em fila há</th>
              <th>SLA</th>
              <th>Prioridade</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr><td colSpan={8} style={{ textAlign: "center", padding: 24, color: "var(--color-text-secondary)" }}>Carregando GET /v1/documents?status={status}…</td></tr>
            )}
            {!loading && filtered.map((r) => {
              const horas = elapsedHours(r.enfileiradoEm);
              const slaCritico = horas > r.slaHoras - 1;
              return (
                <tr key={r.documentId}>
                  <td><Link href={`/documents/${r.documentId}`}><code>{r.documentId}</code></Link></td>
                  <td>{r.cnpj}<br /><span style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>{r.razaoSocial}</span></td>
                  <td>{r.motivoLegivel}</td>
                  <td>
                    <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                      <span><strong>{fmtPct(r.scoreCritico)}</strong></span>
                      <div style={{ flex: 1, minWidth: 60 }} className="meter">
                        <div className="meter-fill" style={{ width: `${r.scoreCritico * 100}%` }} />
                      </div>
                    </div>
                  </td>
                  <td>{horas < 1 ? `${Math.round(Math.max(horas, 0) * 60)} min` : `${horas.toFixed(1)} h`}</td>
                  <td>
                    <span className={`badge ${slaCritico ? "badge--err" : "badge--ok"}`}>
                      {slaCritico ? "🔥" : "✓"} {r.slaHoras}h
                    </span>
                  </td>
                  <td>
                    <span className={`badge ${r.prioridade === "alta" ? "badge--err" : r.prioridade === "media" ? "badge--warn" : "badge--neutral"}`}>
                      {r.prioridade}
                    </span>
                  </td>
                  <td>
                    <Link href={`/documents/${r.documentId}`}>Revisar →</Link>
                  </td>
                </tr>
              );
            })}
            {!loading && filtered.length === 0 && (
              <tr>
                <td colSpan={8} style={{ textAlign: "center", padding: 24, color: "var(--color-text-secondary)" }}>
                  {unauthorized ? "Informe um JWT para listar a fila." : "Nenhum item para os filtros aplicados."}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="card">
        <h3>Observações</h3>
        <ul style={{ lineHeight: 1.8 }}>
          <li>Fila lê <strong>documentos persistidos</strong> filtrados por <code>status</code> na API. Sem seed de <code>mocks.ts</code>.</li>
          <li>Default <code>revisao_humana</code> (TH01 / decisão MANUAL). Troque o status para ver falha ou estágios de pipeline.</li>
          <li>Motivo e prioridade são <strong>derivados da confiança</strong> devolvida na lista. Atribuição de responsável não existe na API.</li>
        </ul>
      </div>
    </>
  );
}
