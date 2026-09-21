"use client";

import type { SourceHealth } from "@/domain";

const fmtDate = (iso: string) => new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "long" });
const fmtPct = (n: number) => `${(n * 100).toFixed(1)}%`;

const statusConfig = {
  operacional: { color: "var(--color-status-approved)", icon: "🟢", badge: "badge--ok", label: "Operacional" },
  degradado: { color: "var(--color-status-manual)", icon: "🟡", badge: "badge--warn", label: "Degradado" },
  indisponivel: { color: "var(--color-status-rejected)", icon: "🔴", badge: "badge--err", label: "Indisponível" }
} as const;

const breakerConfig = {
  fechado: { label: "Fechado (normal)", color: "var(--color-status-approved)" },
  "meio-aberto": { label: "Meio-aberto (probing)", color: "var(--color-status-manual)" },
  aberto: { label: "Aberto (curto-circuito)", color: "var(--color-status-rejected)" }
} as const;

export function SourceHealthPanel({ sources }: { sources: SourceHealth[] }) {
  const ok = sources.filter((s) => s.status === "operacional").length;
  const deg = sources.filter((s) => s.status === "degradado").length;
  const off = sources.filter((s) => s.status === "indisponivel").length;

  return (
    <>
      <div className="grid-3">
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-approved)" }}>
          <div className="metric-label">Operacionais</div>
          <div className="metric-value">{ok}</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-manual)" }}>
          <div className="metric-label">Degradadas</div>
          <div className="metric-value">{deg}</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-rejected)" }}>
          <div className="metric-label">Indisponíveis</div>
          <div className="metric-value">{off}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>
            {off > 0 ? "Fallback para AnáliseManual ativo" : "Sem fallback ativo"}
          </div>
        </div>
      </div>

      {sources.map((s) => {
        const cfg = statusConfig[s.status];
        const bcfg = breakerConfig[s.circuitBreaker];
        return (
          <div key={s.sourceId} className="card" style={{ borderLeft: `4px solid ${cfg.color}` }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 12 }}>
              <div>
                <h3 style={{ margin: 0, display: "flex", alignItems: "center", gap: 8 }}>
                  {cfg.icon} {s.nome}
                  <code style={{ fontSize: 12, fontWeight: 400, color: "var(--color-text-muted)" }}>{s.sourceId}</code>
                </h3>
              </div>
              <span className={`badge ${cfg.badge}`}>{cfg.label}</span>
            </div>

            <div style={{ display: "grid", gridTemplateColumns: "repeat(5, 1fr)", gap: 12 }}>
              <div>
                <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>Uptime 24h</div>
                <strong style={{ fontSize: 18 }}>{fmtPct(s.uptime24h)}</strong>
              </div>
              <div>
                <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>p95 latência</div>
                <strong style={{ fontSize: 18 }}>{s.latenciaP95Ms === 0 ? "—" : `${s.latenciaP95Ms}ms`}</strong>
              </div>
              <div>
                <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>Error rate</div>
                <strong style={{ fontSize: 18, color: s.errorRate > 0.05 ? "var(--color-status-rejected)" : undefined }}>{fmtPct(s.errorRate)}</strong>
              </div>
              <div>
                <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>Cache hit-rate</div>
                <strong style={{ fontSize: 18 }}>{fmtPct(s.cacheHitRate)}</strong>
              </div>
              <div>
                <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>Circuit breaker</div>
                <strong style={{ fontSize: 14, color: bcfg.color }}>{bcfg.label}</strong>
              </div>
            </div>

            <div style={{ marginTop: 12, fontSize: 12, color: "var(--color-text-secondary)" }}>
              Última consulta: {fmtDate(s.ultimaConsulta)}
            </div>

            {s.observacao && (
              <div style={{ marginTop: 12, padding: 10, background: s.status === "indisponivel" ? "#FCE8E6" : "#FEF7E0", borderRadius: 6, fontSize: 13 }}>
                ⚠️ {s.observacao}
              </div>
            )}
          </div>
        );
      })}
    </>
  );
}
