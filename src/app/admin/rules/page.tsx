"use client";
import { useMemo, useState } from "react";
import { dmnRules, type DmnRule, type RuleStatus } from "@/lib/mocks";

const fmtDate = (iso: string) => new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" });

const statusConfig: Record<RuleStatus, { label: string; className: string; color: string }> = {
  ativa: { label: "Ativa", className: "badge--ok", color: "var(--color-status-approved)" },
  proposta: { label: "Proposta", className: "badge--info", color: "var(--color-status-info)" },
  em_revisao: { label: "Em revisão", className: "badge--warn", color: "var(--color-status-manual)" },
  depreciada: { label: "Depreciada", className: "badge--neutral", color: "var(--color-text-muted)" }
};

export default function RulesCatalogPage() {
  const [filterStatus, setFilterStatus] = useState<string>("todos");
  const [selected, setSelected] = useState<DmnRule | null>(null);

  const filtered = useMemo(() => {
    return dmnRules.filter((r) => filterStatus === "todos" || r.status === filterStatus);
  }, [filterStatus]);

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Catálogo de Regras de Decisão (DMN)</h2>
          <div className="subtitle">Regras estruturais + condicionais usadas pelo motor — versionadas, com aprovação dupla (PO + Jurídico)</div>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <button className="btn btn--secondary">📥 Exportar DMN (XML)</button>
          <button className="btn btn--primary">+ Propor regra</button>
        </div>
      </div>

      <div className="grid-3">
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-approved)" }}>
          <div className="metric-label">Regras ativas</div>
          <div className="metric-value">{dmnRules.filter((r) => r.status === "ativa").length}</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-info)" }}>
          <div className="metric-label">Em fluxo de aprovação</div>
          <div className="metric-value">{dmnRules.filter((r) => r.status === "proposta" || r.status === "em_revisao").length}</div>
        </div>
        <div className="metric-card">
          <div className="metric-label">Decisões afetadas (30d)</div>
          <div className="metric-value">{dmnRules.filter((r) => r.status === "ativa").reduce((s, r) => s + r.decisoesAfetadas30d, 0).toLocaleString("pt-BR")}</div>
        </div>
      </div>

      <div className="card">
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 12, flexWrap: "wrap", gap: 12 }}>
          <h3 style={{ margin: 0 }}>Regras ({filtered.length})</h3>
          <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
            <label htmlFor="rstatus" style={{ margin: 0, fontSize: 13 }}>Status:</label>
            <select id="rstatus" className="input" value={filterStatus} onChange={(e) => setFilterStatus(e.target.value)} style={{ width: 200 }}>
              <option value="todos">Todos</option>
              {Object.entries(statusConfig).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
            </select>
          </div>
        </div>

        <table>
          <thead>
            <tr>
              <th>ID</th>
              <th>Nome</th>
              <th>Aplica a</th>
              <th>Versão</th>
              <th>Status</th>
              <th>Decisões 30d</th>
              <th>Última alteração</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((r) => {
              const st = statusConfig[r.status];
              return (
                <tr key={r.ruleId}>
                  <td><code style={{ fontWeight: 600 }}>{r.ruleId}</code></td>
                  <td>
                    <strong>{r.name}</strong>
                    <div style={{ fontSize: 12, color: "var(--color-text-secondary)", maxWidth: 380 }}>{r.description}</div>
                  </td>
                  <td>
                    {r.appliesTo.includes("*")
                      ? <span className="badge badge--neutral">Todas operações</span>
                      : <div style={{ display: "flex", gap: 4, flexWrap: "wrap" }}>{r.appliesTo.slice(0, 2).map((op) => <span key={op} className="badge badge--neutral" style={{ fontSize: 11 }}>{op}</span>)}{r.appliesTo.length > 2 && <span className="badge badge--neutral" style={{ fontSize: 11 }}>+{r.appliesTo.length - 2}</span>}</div>}
                  </td>
                  <td><code>{r.version}</code></td>
                  <td><span className={`badge ${st.className}`}>{st.label}</span></td>
                  <td style={{ textAlign: "right" }}>{r.decisoesAfetadas30d.toLocaleString("pt-BR")}</td>
                  <td style={{ fontSize: 13 }}>{fmtDate(r.lastModified)}</td>
                  <td>
                    <button onClick={() => setSelected(r)} className="btn btn--ghost" style={{ padding: "4px 12px", fontSize: 12 }}>Detalhar</button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {selected && (
        <div className="card" style={{ borderLeft: `4px solid ${statusConfig[selected.status].color}` }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 12 }}>
            <h3 style={{ margin: 0 }}>
              <code>{selected.ruleId}</code> · {selected.name}
              <span className={`badge ${statusConfig[selected.status].className}`} style={{ marginLeft: 12 }}>{statusConfig[selected.status].label}</span>
            </h3>
            <button onClick={() => setSelected(null)} className="btn btn--ghost">Fechar</button>
          </div>
          <p style={{ fontSize: 14, color: "var(--color-text-secondary)" }}>{selected.description}</p>

          <h3 style={{ marginTop: 16 }}>Expressão (resumida)</h3>
          <pre style={{ fontSize: 13 }}>{selected.expressionSummary}</pre>

          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16, marginTop: 16 }}>
            <div>
              <h3>Metadados</h3>
              <table>
                <tbody>
                  <tr><th>Versão</th><td><code>{selected.version}</code></td></tr>
                  <tr><th>Owner</th><td>{selected.owner}</td></tr>
                  <tr><th>Aprovadores</th><td>{selected.approvers.length > 0 ? selected.approvers.join(", ") : <em style={{ color: "var(--color-text-muted)" }}>aguardando aprovação</em>}</td></tr>
                  <tr><th>Origem regulatória</th><td>{selected.origemRegulatoria}</td></tr>
                  <tr><th>Última alteração</th><td>{fmtDate(selected.lastModified)}</td></tr>
                  <tr><th>Promovida em</th><td>{selected.promotedAt ? fmtDate(selected.promotedAt) : <em style={{ color: "var(--color-text-muted)" }}>—</em>}</td></tr>
                </tbody>
              </table>
            </div>
            <div>
              <h3>Aplica a operações</h3>
              <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}>
                {selected.appliesTo.includes("*")
                  ? <span className="badge badge--info">Todas as operações</span>
                  : selected.appliesTo.map((op) => <span key={op} className="badge badge--neutral">{op}</span>)}
              </div>

              <h3 style={{ marginTop: 16 }}>Impacto (30 dias)</h3>
              <div style={{ fontSize: 32, fontWeight: 600, color: statusConfig[selected.status].color }}>
                {selected.decisoesAfetadas30d.toLocaleString("pt-BR")}
              </div>
              <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>decisões em que esta regra foi avaliada</div>
            </div>
          </div>

          <div style={{ display: "flex", gap: 8, marginTop: 16 }}>
            <button className="btn btn--secondary">📊 Simular</button>
            <button className="btn btn--secondary">📥 Backtest</button>
            {selected.status === "proposta" && <button className="btn btn--primary">Encaminhar para revisão</button>}
            {selected.status === "em_revisao" && <button className="btn btn--primary">Aprovar e promover</button>}
            {selected.status === "ativa" && <button className="btn btn--ghost">Depreciar</button>}
          </div>
        </div>
      )}

      <div className="card">
        <h3>Workflow de promoção (resumo)</h3>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: 8, marginTop: 8 }}>
          <div style={{ padding: 12, border: "1px solid var(--color-border-default)", borderRadius: 8 }}>
            <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>1. Proposta</div>
            <strong>Curador propõe</strong>
            <div style={{ fontSize: 12, marginTop: 4 }}>Cria regra em rascunho com expression DMN</div>
          </div>
          <div style={{ padding: 12, border: "1px solid var(--color-border-default)", borderRadius: 8 }}>
            <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>2. Revisão</div>
            <strong>Em revisão</strong>
            <div style={{ fontSize: 12, marginTop: 4 }}>Backtest contra histórico, simulação, parecer jurídico</div>
          </div>
          <div style={{ padding: 12, border: "1px solid var(--color-border-default)", borderRadius: 8 }}>
            <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>3. Aprovação dupla</div>
            <strong>PO + Jurídico</strong>
            <div style={{ fontSize: 12, marginTop: 4 }}>Necessárias 2 aprovações independentes</div>
          </div>
          <div style={{ padding: 12, border: "1px solid var(--color-border-default)", borderRadius: 8, background: "#E6F4EA" }}>
            <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>4. Promoção</div>
            <strong>Ativa em produção</strong>
            <div style={{ fontSize: 12, marginTop: 4 }}>Snapshot persistido para auditoria — rollback em {"<"} 5 min</div>
          </div>
        </div>
      </div>
    </>
  );
}
