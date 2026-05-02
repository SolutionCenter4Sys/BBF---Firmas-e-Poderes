"use client";
import { useMemo, useState } from "react";
import { apiConsumers, type ConsumerStatus } from "@/lib/mocks";

const fmtDate = (iso: string) => new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" });
const fmtPct = (n: number) => `${(n * 100).toFixed(2)}%`;
const fmtN = (n: number) => n.toLocaleString("pt-BR");

const statusConfig: Record<ConsumerStatus, { label: string; className: string }> = {
  ativo: { label: "Ativo", className: "badge--ok" },
  suspenso: { label: "Suspenso", className: "badge--err" },
  em_homologacao: { label: "Em homologação", className: "badge--warn" }
};

export default function ApiConsumersPage() {
  const [filterStatus, setFilterStatus] = useState<string>("todos");

  const filtered = useMemo(() => apiConsumers.filter((c) => filterStatus === "todos" || c.status === filterStatus), [filterStatus]);

  const totalReqsToday = apiConsumers.reduce((s, c) => s + c.requestsToday, 0);
  const totalReqsMonth = apiConsumers.reduce((s, c) => s + c.requestsMonth, 0);
  const avgErrorRate = apiConsumers.filter((c) => c.requestsToday > 0).reduce((s, c) => s + c.errorRate, 0) / apiConsumers.filter((c) => c.requestsToday > 0).length;

  const maxToday = Math.max(...apiConsumers.map((c) => c.requestsToday), 1);

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Consumidores da API</h2>
          <div className="subtitle">Jornadas e sistemas que consomem o endpoint <code>/v1/authority/decision</code> — uso, latência e auditoria por consumer</div>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <button className="btn btn--secondary">Exportar audit log</button>
          <button className="btn btn--primary">+ Cadastrar consumer</button>
        </div>
      </div>

      <div className="grid-3">
        <div className="metric-card">
          <div className="metric-label">Requests hoje</div>
          <div className="metric-value">{fmtN(totalReqsToday)}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>Em {apiConsumers.length} consumers cadastrados</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-info)" }}>
          <div className="metric-label">Requests no mês</div>
          <div className="metric-value">{fmtN(totalReqsMonth)}</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: avgErrorRate > 0.01 ? "var(--color-status-manual)" : "var(--color-status-approved)" }}>
          <div className="metric-label">Error rate médio</div>
          <div className="metric-value">{fmtPct(avgErrorRate)}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>SLO: ≤ 1% — {avgErrorRate <= 0.01 ? "✓" : "⚠"}</div>
        </div>
      </div>

      <div className="card">
        <h3>Volume de requests hoje (top consumers)</h3>
        <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
          {[...apiConsumers].sort((a, b) => b.requestsToday - a.requestsToday).slice(0, 5).map((c) => (
            <div key={c.consumerId} style={{ display: "grid", gridTemplateColumns: "200px 1fr 80px", gap: 12, alignItems: "center" }}>
              <span style={{ fontSize: 13 }}><strong>{c.name}</strong></span>
              <div style={{ background: "var(--color-bg-emphasis)", height: 18, borderRadius: 4, overflow: "hidden", position: "relative" }}>
                <div style={{ width: `${(c.requestsToday / maxToday) * 100}%`, height: "100%", background: "var(--color-brand-secondary)", borderRadius: 4 }} />
              </div>
              <span style={{ textAlign: "right", fontSize: 13, fontWeight: 600 }}>{fmtN(c.requestsToday)}</span>
            </div>
          ))}
        </div>
      </div>

      <div className="card">
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 12 }}>
          <h3 style={{ margin: 0 }}>Consumers cadastrados ({filtered.length})</h3>
          <select className="input" value={filterStatus} onChange={(e) => setFilterStatus(e.target.value)} style={{ width: 200 }}>
            <option value="todos">Todos os status</option>
            {Object.entries(statusConfig).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
          </select>
        </div>
        <table>
          <thead>
            <tr>
              <th>Consumer</th>
              <th>Owner</th>
              <th>Escopos</th>
              <th>Rate limit</th>
              <th>Reqs hoje</th>
              <th>p95 latência</th>
              <th>Success rate</th>
              <th>Status</th>
              <th>Última chamada</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((c) => {
              const st = statusConfig[c.status];
              return (
                <tr key={c.consumerId}>
                  <td>
                    <strong>{c.name}</strong>
                    <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}><code>{c.consumerId}</code></div>
                  </td>
                  <td style={{ fontSize: 13 }}>{c.owner}</td>
                  <td>
                    <div style={{ display: "flex", gap: 4, flexWrap: "wrap" }}>
                      {c.scopes.map((s) => <span key={s} className="badge badge--neutral" style={{ fontFamily: "var(--font-family-mono)", fontSize: 10 }}>{s}</span>)}
                    </div>
                  </td>
                  <td>{c.rateLimitPerMin}/min</td>
                  <td><strong>{fmtN(c.requestsToday)}</strong></td>
                  <td style={{ color: c.p95LatencyMs > 1800 ? "var(--color-status-manual)" : c.p95LatencyMs > 0 ? "var(--color-status-approved)" : "var(--color-text-muted)" }}>
                    {c.p95LatencyMs > 0 ? `${c.p95LatencyMs}ms` : "—"}
                  </td>
                  <td style={{ color: c.successRate < 0.99 && c.successRate > 0 ? "var(--color-status-manual)" : c.successRate > 0 ? "var(--color-status-approved)" : "var(--color-text-muted)" }}>
                    {c.successRate > 0 ? fmtPct(c.successRate) : "—"}
                  </td>
                  <td><span className={`badge ${st.className}`}>{st.label}</span></td>
                  <td style={{ fontSize: 12 }}>{fmtDate(c.lastCall)}</td>
                  <td>
                    <a href={`/audit?consumer=${c.consumerId}`}>Audit log →</a>
                  </td>
                </tr>
              );
            })}
            {filtered.length === 0 && (
              <tr><td colSpan={10} style={{ textAlign: "center", padding: 24, color: "var(--color-text-secondary)" }}>Nenhum consumer encontrado.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="grid-2">
        <div className="card">
          <h3>Políticas de rate limit</h3>
          <ul style={{ lineHeight: 1.8 }}>
            <li>Limites são aplicados no <strong>API Gateway corporativo</strong> (Kong) por <code>consumerId</code>.</li>
            <li>Rate limit padrão: <strong>100 req/min</strong>; aumento sob demanda via portal interno.</li>
            <li>Burst: até <strong>2× o limite</strong> por 10 segundos consecutivos.</li>
            <li>Resposta <code>429 Too Many Requests</code> com header <code>Retry-After</code>.</li>
          </ul>
        </div>
        <div className="card">
          <h3>Auditoria por consumer</h3>
          <ul style={{ lineHeight: 1.8 }}>
            <li>Cada chamada é registrada com <code>consumerId</code>, <code>scope</code>, <code>correlationId</code>, payload mascarado e timestamp.</li>
            <li>Log <strong>imutável</strong> (append-only) — retenção de 5 anos.</li>
            <li>Acesso restrito a perfis <code>auditor</code> e <code>admin</code>.</li>
            <li>Disponível para download em CSV ou via API <code>GET /v1/api-audit?consumer=...</code>.</li>
          </ul>
        </div>
      </div>
    </>
  );
}
