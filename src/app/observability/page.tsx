import { dashboardsByStage, slo, decisionsLast24h, errorsLast24h, latencyP95Last24h, costPerDecisionBreakdown, costByConsumer } from "@/lib/mocks";

const fmtBRL = (n: number) => n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
const fmtPct = (n: number) => `${(n * 100).toFixed(2)}%`;
const fmtN = (n: number) => n.toLocaleString("pt-BR");

const tendenciaIcon = { estavel: "→", alta: "↑", queda: "↓" } as const;
const tendenciaColor = { estavel: "var(--color-text-secondary)", alta: "var(--color-status-rejected)", queda: "var(--color-status-approved)" } as const;

export default function ObservabilityPage() {
  const totalDecisoesMes = costByConsumer.reduce((s, c) => s + c.decisoesMes, 0);
  const totalCostMes = costByConsumer.reduce((s, c) => s + c.custoTotalMesBRL, 0);
  const custoMedioReal = totalCostMes / totalDecisoesMes;
  const totalCustoUnitario = costPerDecisionBreakdown.reduce((s, c) => s + c.custoUnitarioBRL, 0);

  const max24h = Math.max(...decisionsLast24h);
  const maxLat = Math.max(...latencyP95Last24h);

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Observabilidade · SLO · FinOps</h2>
          <div className="subtitle">Saúde do produto por estágio do Flow + custo agregado por decisão</div>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <button className="btn btn--secondary">Abrir Grafana</button>
          <button className="btn btn--secondary">Exportar relatório FinOps</button>
        </div>
      </div>

      {/* SLO consolidado */}
      <div className="grid-3">
        <div className="metric-card" style={{ borderLeftColor: slo.p95LatencyMs.status === "ok" ? "var(--color-status-approved)" : "var(--color-status-rejected)" }}>
          <div className="metric-label">SLO p95 latência</div>
          <div className="metric-value">{slo.p95LatencyMs.current}ms</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>Meta {"<"} {slo.p95LatencyMs.target}ms · {slo.p95LatencyMs.status === "ok" ? "✓" : "⚠"}</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: slo.uptime30d.status === "ok" ? "var(--color-status-approved)" : "var(--color-status-rejected)" }}>
          <div className="metric-label">SLO disponibilidade 30d</div>
          <div className="metric-value">{fmtPct(slo.uptime30d.current)}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>Meta ≥ {fmtPct(slo.uptime30d.target)} · {slo.uptime30d.status === "ok" ? "✓" : "⚠"}</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-info)" }}>
          <div className="metric-label">Error budget</div>
          <div className="metric-value">{fmtPct(slo.errorBudgetConsumed)}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>consumido · ~{slo.errorBudgetRemainingDays} dias restantes no ciclo</div>
        </div>
      </div>

      {/* Time-series 24h */}
      <div className="grid-2">
        <div className="card">
          <h3>Decisões por hora (últimas 24h)</h3>
          <div style={{ display: "flex", alignItems: "flex-end", gap: 3, height: 120, padding: "8px 0" }}>
            {decisionsLast24h.map((v, i) => (
              <div key={i} title={`${i}h: ${v} decisões`} style={{ flex: 1, height: `${(v / max24h) * 100}%`, background: "var(--color-brand-secondary)", borderRadius: "3px 3px 0 0", opacity: 0.85, cursor: "help" }} />
            ))}
          </div>
          <div style={{ display: "flex", justifyContent: "space-between", fontSize: 11, color: "var(--color-text-secondary)" }}>
            <span>00h</span><span>06h</span><span>12h</span><span>18h</span><span>23h</span>
          </div>
          <div style={{ marginTop: 8, fontSize: 13, color: "var(--color-text-secondary)" }}>
            Total nas últimas 24h: <strong>{fmtN(decisionsLast24h.reduce((s, v) => s + v, 0))}</strong> decisões
          </div>
        </div>
        <div className="card">
          <h3>p95 latência por hora (últimas 24h)</h3>
          <div style={{ display: "flex", alignItems: "flex-end", gap: 3, height: 120, padding: "8px 0" }}>
            {latencyP95Last24h.map((v, i) => {
              const slaOk = v < 2000;
              return (
                <div key={i} title={`${i}h: ${v}ms`} style={{ flex: 1, height: `${(v / Math.max(maxLat, 2000)) * 100}%`, background: slaOk ? "var(--color-status-approved)" : "var(--color-status-manual)", borderRadius: "3px 3px 0 0", opacity: 0.85, cursor: "help" }} />
              );
            })}
          </div>
          <div style={{ display: "flex", justifyContent: "space-between", fontSize: 11, color: "var(--color-text-secondary)" }}>
            <span>00h</span><span>06h</span><span>12h</span><span>18h</span><span>23h</span>
          </div>
          <div style={{ marginTop: 8, fontSize: 13, color: "var(--color-text-secondary)" }}>
            Linha vermelha do SLO: <code>2000ms</code> · pico no período: <strong>{Math.max(...latencyP95Last24h)}ms</strong>
          </div>
        </div>
      </div>

      {/* Dashboards por estágio */}
      <div className="card">
        <h3>Saúde por estágio do Flow</h3>
        <table>
          <thead>
            <tr><th>Estágio</th><th>p95 latência</th><th>Throughput (decisões/min)</th><th>Error rate</th><th>Status</th></tr>
          </thead>
          <tbody>
            {dashboardsByStage.map((s, i) => {
              const ok = s.errorRate < 0.02 && s.p95Ms < 2000;
              return (
                <tr key={i}>
                  <td><strong>{s.stage}</strong></td>
                  <td style={{ color: s.p95Ms > 1500 ? "var(--color-status-manual)" : undefined }}>{s.p95Ms}ms</td>
                  <td>{s.throughputPerMin}</td>
                  <td style={{ color: s.errorRate > 0.01 ? "var(--color-status-manual)" : undefined }}>{fmtPct(s.errorRate)}</td>
                  <td>{ok ? <span className="badge badge--ok">OK</span> : <span className="badge badge--warn">Atenção</span>}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
        <div style={{ display: "flex", gap: 16, marginTop: 12, fontSize: 13, color: "var(--color-text-secondary)" }}>
          <div>Erros nas últimas 24h: <strong>{errorsLast24h.reduce((s, v) => s + v, 0)}</strong></div>
          <div>Saturação CPU média: <strong>34%</strong></div>
          <div>Saturação memória: <strong>52%</strong></div>
        </div>
      </div>

      {/* FinOps */}
      <div className="page-header" style={{ marginTop: 32 }}>
        <div>
          <h2 style={{ fontSize: 18 }}>💰 FinOps — Custo por decisão</h2>
          <div className="subtitle">Atribuição de custo por componente e por jornada consumidora · base mensal</div>
        </div>
      </div>

      <div className="grid-3">
        <div className="metric-card" style={{ borderLeftColor: "var(--color-brand-primary)" }}>
          <div className="metric-label">Custo médio por decisão</div>
          <div className="metric-value">{fmtBRL(custoMedioReal)}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>Σ componentes: {fmtBRL(totalCustoUnitario)}</div>
        </div>
        <div className="metric-card">
          <div className="metric-label">Decisões no mês</div>
          <div className="metric-value">{fmtN(totalDecisoesMes)}</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-approved)" }}>
          <div className="metric-label">Custo total mensal</div>
          <div className="metric-value">{fmtBRL(totalCostMes)}</div>
        </div>
      </div>

      <div className="grid-2">
        <div className="card">
          <h3>Custo por componente (unitário por decisão)</h3>
          <table>
            <thead><tr><th>Componente</th><th>Custo</th><th>%</th><th>Tendência 30d</th></tr></thead>
            <tbody>
              {costPerDecisionBreakdown.map((c, i) => (
                <tr key={i}>
                  <td>
                    <strong>{c.componente}</strong>
                    {c.observacao && <div style={{ fontSize: 11, color: "var(--color-text-muted)" }}>{c.observacao}</div>}
                  </td>
                  <td style={{ fontSize: 14, fontWeight: 600 }}>{fmtBRL(c.custoUnitarioBRL)}</td>
                  <td>
                    <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
                      <strong>{(c.participacaoPct * 100).toFixed(0)}%</strong>
                      <div style={{ width: 60, height: 6, background: "var(--color-bg-emphasis)", borderRadius: 3 }}>
                        <div style={{ width: `${c.participacaoPct * 100}%`, height: "100%", background: "var(--color-brand-secondary)", borderRadius: 3 }} />
                      </div>
                    </div>
                  </td>
                  <td style={{ color: tendenciaColor[c.tendencia30d], fontWeight: 600 }}>
                    {tendenciaIcon[c.tendencia30d]} {c.tendencia30d}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="card">
          <h3>Custo por jornada consumidora (mês)</h3>
          <table>
            <thead><tr><th>Consumer</th><th>Decisões</th><th>Total</th><th>Médio</th></tr></thead>
            <tbody>
              {costByConsumer.map((c, i) => (
                <tr key={i}>
                  <td><strong>{c.name}</strong></td>
                  <td>{fmtN(c.decisoesMes)}</td>
                  <td><strong>{fmtBRL(c.custoTotalMesBRL)}</strong></td>
                  <td style={{ color: c.custoMedioBRL > 0.5 ? "var(--color-status-manual)" : undefined }}>{fmtBRL(c.custoMedioBRL)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)", marginTop: 12 }}>
            Diferença de custo médio se explica por: tipo de operação, complexidade do documento e cache hit-rate da fonte oficial.
          </div>
        </div>
      </div>

      <div className="card">
        <h3>Iniciativas de otimização (FinOps)</h3>
        <ul style={{ lineHeight: 1.8 }}>
          <li><strong>Migração progressiva para Tesseract</strong> em casos simples (queda projetada de 18% no custo do OCR).</li>
          <li><strong>Cache TTL otimizado</strong> para Junta Comercial (atualmente 42% hit-rate; meta 60% reduz custo da fonte em ~30%).</li>
          <li><strong>Truncamento inteligente</strong> de prompts LLM em documentos grandes (a estudar — pode reduzir tokens em ~20%).</li>
          <li><strong>Compactação JSON</strong> no canônico para reduzir custo de armazenamento (queda de ~10% no S3).</li>
        </ul>
      </div>
    </>
  );
}
