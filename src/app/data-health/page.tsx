import { piiScans, piiTendencia30d } from "@/lib/mocks";

const fmtDate = (iso: string) => new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" });
const fmtPct = (n: number) => `${(n * 100).toFixed(2)}%`;

const statusConfig = {
  ok: { className: "badge--ok", label: "OK", icon: "✓" },
  alerta: { className: "badge--warn", label: "Alerta", icon: "⚠" },
  critico: { className: "badge--err", label: "Crítico", icon: "✗" }
} as const;

export default function DataHealthPage() {
  const totalDetectadas = piiScans.reduce((s, p) => s + p.ocorrenciasDetectadas, 0);
  const totalMascaradas = piiScans.reduce((s, p) => s + p.ocorrenciasMascaradas, 0);
  const coberturaGlobal = totalDetectadas > 0 ? totalMascaradas / totalDetectadas : 1;
  const minTrend = Math.min(...piiTendencia30d);

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Saúde de Dados — Mascaramento de PII</h2>
          <div className="subtitle">Resultado dos scans automáticos semanais sobre amostras de logs (CI)</div>
        </div>
        <button className="btn btn--secondary">Executar scan agora</button>
      </div>

      <div className="grid-3">
        <div className="metric-card" style={{ borderLeftColor: coberturaGlobal >= 0.999 ? "var(--color-status-approved)" : "var(--color-status-manual)" }}>
          <div className="metric-label">Cobertura global de mascaramento</div>
          <div className="metric-value">{fmtPct(coberturaGlobal)}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>Meta: ≥ 99.9% — {coberturaGlobal >= 0.999 ? "✓ atingida" : "⚠ abaixo"}</div>
        </div>
        <div className="metric-card">
          <div className="metric-label">Ocorrências de PII (semana)</div>
          <div className="metric-value">{totalDetectadas.toLocaleString("pt-BR")}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>{totalMascaradas.toLocaleString("pt-BR")} mascaradas · {(totalDetectadas - totalMascaradas).toLocaleString("pt-BR")} vazadas</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: minTrend >= 0.999 ? "var(--color-status-approved)" : "var(--color-status-manual)" }}>
          <div className="metric-label">Mínimo nos últimos 30 dias</div>
          <div className="metric-value">{fmtPct(minTrend)}</div>
        </div>
      </div>

      <div className="card">
        <h3>Tendência (últimos 30 dias)</h3>
        <div style={{ display: "flex", alignItems: "flex-end", gap: 4, height: 120, padding: "12px 0" }}>
          {piiTendencia30d.map((v, i) => {
            const altura = ((v - 0.99) / 0.01) * 100;
            const ok = v >= 0.999;
            return (
              <div
                key={i}
                title={`Dia ${i + 1}: ${fmtPct(v)}`}
                style={{
                  flex: 1,
                  height: `${Math.max(altura, 8)}%`,
                  background: ok ? "var(--color-status-approved)" : "var(--color-status-manual)",
                  borderRadius: "4px 4px 0 0",
                  opacity: 0.85,
                  cursor: "help"
                }}
              />
            );
          })}
        </div>
        <div style={{ display: "flex", justifyContent: "space-between", fontSize: 11, color: "var(--color-text-secondary)" }}>
          <span>D-30</span><span>D-15</span><span>Hoje</span>
        </div>
      </div>

      <div className="card">
        <h3>Resultado por serviço (último scan)</h3>
        <table>
          <thead>
            <tr>
              <th>Serviço</th>
              <th>Amostra de logs</th>
              <th>Ocorrências detectadas</th>
              <th>Mascaradas</th>
              <th>Vazadas</th>
              <th>Cobertura</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {piiScans.map((p, i) => {
              const cfg = statusConfig[p.status];
              const vazadas = p.ocorrenciasDetectadas - p.ocorrenciasMascaradas;
              return (
                <tr key={i}>
                  <td><code>{p.servico}</code></td>
                  <td>{p.amostraLogs.toLocaleString("pt-BR")}</td>
                  <td>{p.ocorrenciasDetectadas.toLocaleString("pt-BR")}</td>
                  <td style={{ color: "var(--color-status-approved)" }}>{p.ocorrenciasMascaradas.toLocaleString("pt-BR")}</td>
                  <td style={{ color: vazadas > 0 ? "var(--color-status-rejected)" : undefined }}>
                    {vazadas > 0 ? <strong>{vazadas}</strong> : "0"}
                  </td>
                  <td>{fmtPct(p.cobertura)}</td>
                  <td><span className={`badge ${cfg.className}`}>{cfg.icon} {cfg.label}</span></td>
                </tr>
              );
            })}
          </tbody>
        </table>
        <p style={{ fontSize: 13, color: "var(--color-text-secondary)", marginTop: 12 }}>
          Último scan: <strong>{fmtDate(piiScans[0]?.executadoEm ?? "")}</strong> · próximo: agendado para todas as quartas às 22:00 UTC.
        </p>
      </div>

      <div className="card">
        <h3>Ações em andamento</h3>
        <ul style={{ lineHeight: 1.8 }}>
          <li><strong>ai-ocr-service:</strong> 3 ocorrências de PII vazadas em logs verbose. Investigação em andamento (ticket SEC-1428).</li>
          <li><strong>Política:</strong> qualquer scan com cobertura {"<"} 99.9% <strong>bloqueia o pipeline de release</strong>.</li>
          <li><strong>Cobertura</strong> de detecção (regex/ML) de tipos PII: CPF, CNPJ, e-mail, telefone, RG, nome composto.</li>
        </ul>
      </div>
    </>
  );
}
