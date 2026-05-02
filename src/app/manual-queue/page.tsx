"use client";
import { useMemo, useState } from "react";
import Link from "next/link";
import { manualQueue } from "@/lib/mocks";

const fmtBRL = (n: number) => n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
const elapsedHours = (iso: string) => (Date.now() - new Date(iso).getTime()) / 3_600_000;

export default function ManualQueuePage() {
  const [filterPrioridade, setFilterPrioridade] = useState<string>("todas");
  const filtered = useMemo(() => manualQueue.filter((m) => filterPrioridade === "todas" || m.prioridade === filterPrioridade), [filterPrioridade]);

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Análise Manual</h2>
          <div className="subtitle">Decisões encaminhadas para revisão humana — threshold, ambiguidade ou indisponibilidade externa</div>
        </div>
        <button className="btn btn--primary">Atender próximo</button>
      </div>

      <div className="grid-3">
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-manual)" }}>
          <div className="metric-label">Em fila manual</div>
          <div className="metric-value">{manualQueue.length}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>{manualQueue.filter((m) => m.prioridade === "alta").length} prioridade alta</div>
        </div>
        <div className="metric-card">
          <div className="metric-label">Volume financeiro envolvido</div>
          <div className="metric-value">{fmtBRL(manualQueue.reduce((s, m) => s + (m.valorOperacao ?? 0), 0))}</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-rejected)" }}>
          <div className="metric-label">SLA estourado</div>
          <div className="metric-value">{manualQueue.filter((m) => elapsedHours(m.enfileiradoEm) > m.slaHoras).length}</div>
        </div>
      </div>

      <div className="card">
        <h3>Filtros</h3>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr 1fr", gap: 12 }}>
          <div>
            <label>Prioridade</label>
            <select className="input" value={filterPrioridade} onChange={(e) => setFilterPrioridade(e.target.value)}>
              <option value="todas">Todas</option>
              <option value="alta">Alta</option>
              <option value="media">Média</option>
              <option value="baixa">Baixa</option>
            </select>
          </div>
          <div><label>Operação</label><select className="input"><option>Todas</option></select></div>
          <div><label>Responsável</label><select className="input"><option>Todos</option><option>Apenas meus</option></select></div>
          <div><label>Valor mínimo</label><input className="input" type="number" placeholder="R$ 0" /></div>
        </div>
      </div>

      <div className="card">
        <h3>Decisões em análise manual ({filtered.length})</h3>
        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>CNPJ / Razão</th>
              <th>Operação</th>
              <th>Valor</th>
              <th>Motivo</th>
              <th>Em fila há</th>
              <th>SLA</th>
              <th>Prioridade</th>
              <th>Responsável</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((m) => {
              const horas = elapsedHours(m.enfileiradoEm);
              const slaStourado = horas > m.slaHoras;
              return (
                <tr key={m.manualId}>
                  <td><code>{m.manualId}</code></td>
                  <td>{m.cnpj}<br /><span style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>{m.razaoSocial}</span></td>
                  <td>{m.operacao}</td>
                  <td>{m.valorOperacao ? fmtBRL(m.valorOperacao) : <em style={{ color: "var(--color-text-muted)" }}>n/a</em>}</td>
                  <td style={{ maxWidth: 280 }}>{m.motivo}</td>
                  <td>{horas < 1 ? `${Math.round(horas * 60)} min` : `${horas.toFixed(1)} h`}</td>
                  <td><span className={`badge ${slaStourado ? "badge--err" : "badge--ok"}`}>{m.slaHoras}h {slaStourado ? "🔥" : "✓"}</span></td>
                  <td><span className={`badge ${m.prioridade === "alta" ? "badge--err" : m.prioridade === "media" ? "badge--warn" : "badge--neutral"}`}>{m.prioridade}</span></td>
                  <td style={{ fontSize: 13 }}>{m.responsavel ?? <em style={{ color: "var(--color-text-muted)" }}>—</em>}</td>
                  <td>
                    {m.decisionId
                      ? <Link href={`/decision?docId=${m.documentId}`}>Abrir →</Link>
                      : <Link href={`/documents/${m.documentId}`}>Ver doc →</Link>}
                  </td>
                </tr>
              );
            })}
            {filtered.length === 0 && (
              <tr><td colSpan={10} style={{ textAlign: "center", padding: 24, color: "var(--color-text-secondary)" }}>Nenhum item para os filtros aplicados.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
