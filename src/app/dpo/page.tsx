"use client";
import { useMemo, useState } from "react";
import { dpoRequests, type DpoRequestStatus, type DpoRequestType } from "@/lib/mocks";

const fmtDate = (iso: string) => new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" });

const tipoLabels: Record<DpoRequestType, string> = {
  acesso: "Acesso",
  retificacao: "Retificação",
  eliminacao: "Eliminação",
  portabilidade: "Portabilidade",
  informacao: "Informação"
};

const statusConfig: Record<DpoRequestStatus, { label: string; className: string }> = {
  novo: { label: "Novo", className: "badge--info" },
  em_atendimento: { label: "Em atendimento", className: "badge--warn" },
  concluido: { label: "Concluído", className: "badge--ok" },
  negado: { label: "Negado", className: "badge--err" }
};

export default function DpoPage() {
  const [filterStatus, setFilterStatus] = useState<string>("todos");

  const filtered = useMemo(() => {
    return dpoRequests.filter((r) => filterStatus === "todos" || r.status === filterStatus);
  }, [filterStatus]);

  const novos = dpoRequests.filter((r) => r.status === "novo").length;
  const emAtend = dpoRequests.filter((r) => r.status === "em_atendimento").length;
  const slaCritico = dpoRequests.filter((r) => r.diasRestantes <= 3 && r.status !== "concluido" && r.status !== "negado").length;

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Console DPO — Direitos dos Titulares (LGPD)</h2>
          <div className="subtitle">Atendimento a requisições de acesso, retificação, eliminação, portabilidade e informação</div>
        </div>
        <button className="btn btn--primary">+ Nova requisição</button>
      </div>

      <div className="grid-3">
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-info)" }}>
          <div className="metric-label">Novos</div>
          <div className="metric-value">{novos}</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-manual)" }}>
          <div className="metric-label">Em atendimento</div>
          <div className="metric-value">{emAtend}</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-rejected)" }}>
          <div className="metric-label">SLA crítico (≤ 3 dias)</div>
          <div className="metric-value">{slaCritico}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>Prazo legal: 15 dias úteis</div>
        </div>
      </div>

      <div className="card">
        <h3>Filtros</h3>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: 12 }}>
          <div>
            <label>Status</label>
            <select className="input" value={filterStatus} onChange={(e) => setFilterStatus(e.target.value)}>
              <option value="todos">Todos</option>
              {Object.entries(statusConfig).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
            </select>
          </div>
          <div><label>Tipo</label><select className="input"><option>Todos</option>{Object.entries(tipoLabels).map(([k, v]) => <option key={k}>{v}</option>)}</select></div>
          <div><label>Buscar titular</label><input className="input" placeholder="Nome ou documento" /></div>
        </div>
      </div>

      <div className="card">
        <h3>Requisições ({filtered.length})</h3>
        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>Titular</th>
              <th>Tipo</th>
              <th>Recebida em</th>
              <th>Prazo restante</th>
              <th>Status</th>
              <th>Responsável</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((r) => {
              const s = statusConfig[r.status];
              const slaCrit = r.diasRestantes <= 3 && r.status !== "concluido" && r.status !== "negado";
              return (
                <tr key={r.requestId}>
                  <td><code>{r.requestId}</code></td>
                  <td>
                    <strong>{r.titularNome}</strong>
                    <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}><code>{r.titularDocumento}</code></div>
                  </td>
                  <td>{tipoLabels[r.tipo]}</td>
                  <td style={{ fontSize: 13 }}>{fmtDate(r.receivedAt)}</td>
                  <td>
                    {r.status === "concluido" || r.status === "negado"
                      ? <em style={{ color: "var(--color-text-muted)" }}>encerrado</em>
                      : <span className={`badge ${slaCrit ? "badge--err" : "badge--neutral"}`}>{r.diasRestantes} dias</span>}
                  </td>
                  <td><span className={`badge ${s.className}`}>{s.label}</span></td>
                  <td style={{ fontSize: 13 }}>{r.responsavel ?? <em style={{ color: "var(--color-text-muted)" }}>não atribuído</em>}</td>
                  <td><a href="#">Abrir →</a></td>
                </tr>
              );
            })}
            {filtered.length === 0 && (
              <tr><td colSpan={8} style={{ textAlign: "center", padding: 24, color: "var(--color-text-secondary)" }}>Nenhuma requisição encontrada.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="card">
        <h3>Bases legais utilizadas</h3>
        <ul style={{ lineHeight: 1.8 }}>
          <li><strong>Cumprimento de obrigação legal/regulatória</strong> (LGPD art. 7º, II) — base principal para validação de firmas e poderes (BACEN/CVM).</li>
          <li><strong>Execução de contrato</strong> (LGPD art. 7º, V) — para clientes PJ em onboarding.</li>
          <li><strong>Legítimo interesse</strong> (LGPD art. 7º, IX) — para auditoria interna e prevenção a fraude.</li>
        </ul>
      </div>
    </>
  );
}
