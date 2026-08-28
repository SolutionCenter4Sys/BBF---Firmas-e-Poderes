"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { JwtTokenForm } from "@/components/JwtTokenForm";
import type { DocStatus } from "@/domain";
import { useDocuments } from "@/hooks/useDocuments";

const fmtBRL = (n: number) => n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
const elapsedHours = (iso: string) => (Date.now() - new Date(iso).getTime()) / 3_600_000;

const QUEUE_STATUSES: readonly DocStatus[] = [
  "revisao_humana",
  "validacao_oficial",
  "canonico_pronto",
  "decidido"
];

function isQueueStatus(value: string): value is DocStatus {
  return (QUEUE_STATUSES as readonly string[]).includes(value);
}

function prioridadeOf(status: DocStatus): "alta" | "media" | "baixa" {
  if (status === "revisao_humana") return "alta";
  if (status === "validacao_oficial") return "media";
  return "baixa";
}

export default function ManualQueuePage() {
  const [status, setStatus] = useState<DocStatus>("revisao_humana");
  const [filterPrioridade, setFilterPrioridade] = useState<string>("todas");
  const { items, loading, error, unauthorized, refresh } = useDocuments(status);

  const rows = useMemo(() => items.map((d) => ({
    documentId: d.documentId,
    cnpj: d.cnpj ?? "—",
    razaoSocial: d.razaoSocial ?? "—",
    operacao: d.fileName,
    motivo: `Documento em ${d.status}`,
    enfileiradoEm: d.uploadedAt,
    slaHoras: 4,
    prioridade: prioridadeOf(d.status),
    valorOperacao: undefined as number | undefined
  })), [items]);

  const filtered = useMemo(
    () => rows.filter((m) => filterPrioridade === "todas" || m.prioridade === filterPrioridade),
    [rows, filterPrioridade]
  );

  const nextHref = filtered[0] ? `/decision?docId=${filtered[0].documentId}` : null;
  const volume = filtered.reduce((s, m) => s + (m.valorOperacao ?? 0), 0);

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Análise Manual</h2>
          <div className="subtitle">GET <code>/v1/documents?status={status}</code> — decisões MANUAL / canônico a validar</div>
        </div>
        {nextHref
          ? <Link className="btn btn--primary" href={nextHref}>Atender próximo</Link>
          : <button type="button" className="btn btn--primary" disabled>Atender próximo</button>}
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
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-manual)" }}>
          <div className="metric-label">Em fila manual</div>
          <div className="metric-value">{loading ? "…" : rows.length}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>{rows.filter((m) => m.prioridade === "alta").length} prioridade alta</div>
        </div>
        <div className="metric-card">
          <div className="metric-label">Volume financeiro envolvido</div>
          <div className="metric-value">{fmtBRL(volume)}</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-rejected)" }}>
          <div className="metric-label">SLA estourado</div>
          <div className="metric-value">{rows.filter((m) => elapsedHours(m.enfileiradoEm) > m.slaHoras).length}</div>
        </div>
      </div>

      <div className="card">
        <h3>Filtros</h3>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
          <div>
            <label htmlFor="manual-status">Status (API)</label>
            <select id="manual-status" className="input" value={status} onChange={(e) => {
              if (isQueueStatus(e.target.value)) setStatus(e.target.value);
            }}>
              {QUEUE_STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
            </select>
          </div>
          <div>
            <label htmlFor="manual-prio">Prioridade</label>
            <select id="manual-prio" className="input" value={filterPrioridade} onChange={(e) => setFilterPrioridade(e.target.value)}>
              <option value="todas">Todas</option>
              <option value="alta">Alta</option>
              <option value="media">Média</option>
              <option value="baixa">Baixa</option>
            </select>
          </div>
        </div>
      </div>

      <div className="card">
        <h3>Decisões em análise manual ({loading ? "…" : filtered.length})</h3>
        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>CNPJ / Razão</th>
              <th>Arquivo</th>
              <th>Motivo</th>
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
            {!loading && filtered.map((m) => {
              const horas = elapsedHours(m.enfileiradoEm);
              const slaStourado = horas > m.slaHoras;
              return (
                <tr key={m.documentId}>
                  <td><code>{m.documentId}</code></td>
                  <td>{m.cnpj}<br /><span style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>{m.razaoSocial}</span></td>
                  <td>{m.operacao}</td>
                  <td style={{ maxWidth: 280 }}>{m.motivo}</td>
                  <td>{horas < 1 ? `${Math.round(Math.max(horas, 0) * 60)} min` : `${horas.toFixed(1)} h`}</td>
                  <td><span className={`badge ${slaStourado ? "badge--err" : "badge--ok"}`}>{m.slaHoras}h {slaStourado ? "🔥" : "✓"}</span></td>
                  <td><span className={`badge ${m.prioridade === "alta" ? "badge--err" : m.prioridade === "media" ? "badge--warn" : "badge--neutral"}`}>{m.prioridade}</span></td>
                  <td>
                    <Link href={`/decision?docId=${m.documentId}`}>Abrir →</Link>
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
    </>
  );
}
