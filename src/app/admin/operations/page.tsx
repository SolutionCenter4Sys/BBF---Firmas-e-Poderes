"use client";
import { useMemo, useState } from "react";
import { operationsCatalog, type Operation } from "@/lib/mocks";

const fmtDate = (iso: string) => new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" });

const categoryConfig: Record<Operation["category"], { label: string; color: string }> = {
  movimentacao: { label: "Movimentação", color: "var(--color-status-info)" },
  credito: { label: "Crédito", color: "var(--color-brand-secondary)" },
  garantia: { label: "Garantia", color: "var(--color-status-manual)" },
  cambio: { label: "Câmbio", color: "#8e24aa" },
  societaria: { label: "Societária", color: "var(--color-status-approved)" }
};

const statusConfig: Record<Operation["status"], { label: string; className: string }> = {
  ativa: { label: "Ativa", className: "badge--ok" },
  rascunho: { label: "Rascunho", className: "badge--warn" },
  depreciada: { label: "Depreciada", className: "badge--neutral" }
};

export default function OperationsCatalogPage() {
  const [filterCat, setFilterCat] = useState<string>("todas");
  const [filterStatus, setFilterStatus] = useState<string>("todos");
  const [query, setQuery] = useState("");

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    return operationsCatalog.filter((o) => {
      if (filterCat !== "todas" && o.category !== filterCat) return false;
      if (filterStatus !== "todos" && o.status !== filterStatus) return false;
      if (q && !o.code.toLowerCase().includes(q) && !o.displayName.toLowerCase().includes(q) && !o.description.toLowerCase().includes(q)) return false;
      return true;
    });
  }, [filterCat, filterStatus, query]);

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Catálogo de Operações</h2>
          <div className="subtitle">Vocabulário controlado de operações suportadas pelo motor de decisão · governança formal por curador</div>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <button className="btn btn--secondary">Exportar</button>
          <button className="btn btn--primary">+ Nova operação</button>
        </div>
      </div>

      <div className="grid-3">
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-approved)" }}>
          <div className="metric-label">Operações ativas</div>
          <div className="metric-value">{operationsCatalog.filter((o) => o.status === "ativa").length}</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-manual)" }}>
          <div className="metric-label">Em rascunho</div>
          <div className="metric-value">{operationsCatalog.filter((o) => o.status === "rascunho").length}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>Aguardando aprovação dupla</div>
        </div>
        <div className="metric-card">
          <div className="metric-label">Categorias cobertas</div>
          <div className="metric-value">{Object.keys(categoryConfig).length}</div>
        </div>
      </div>

      <div className="card">
        <h3>Filtros</h3>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr 1fr", gap: 12 }}>
          <div>
            <label>Categoria</label>
            <select className="input" value={filterCat} onChange={(e) => setFilterCat(e.target.value)}>
              <option value="todas">Todas</option>
              {Object.entries(categoryConfig).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
            </select>
          </div>
          <div>
            <label>Status</label>
            <select className="input" value={filterStatus} onChange={(e) => setFilterStatus(e.target.value)}>
              <option value="todos">Todos</option>
              {Object.entries(statusConfig).map(([k, v]) => <option key={k} value={k}>{v.label}</option>)}
            </select>
          </div>
          <div style={{ gridColumn: "span 2" }}>
            <label>Buscar (code, nome, descrição)</label>
            <input className="input" type="search" value={query} onChange={(e) => setQuery(e.target.value)} placeholder="ex: cambio, contratacao_credito" />
          </div>
        </div>
      </div>

      <div className="card">
        <h3>Operações ({filtered.length})</h3>
        <table>
          <thead>
            <tr>
              <th>Code</th>
              <th>Nome</th>
              <th>Categoria</th>
              <th>Versão</th>
              <th>Owner</th>
              <th>Última alteração</th>
              <th>Status</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((o) => {
              const cat = categoryConfig[o.category];
              const st = statusConfig[o.status];
              return (
                <tr key={o.code}>
                  <td><code style={{ fontSize: 12 }}>{o.code}</code></td>
                  <td>
                    <strong>{o.displayName}</strong>
                    <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>{o.description}</div>
                  </td>
                  <td>
                    <span className="badge badge--neutral" style={{ borderLeft: `3px solid ${cat.color}` }}>{cat.label}</span>
                  </td>
                  <td><code style={{ fontSize: 12 }}>{o.version}</code></td>
                  <td style={{ fontSize: 13 }}>{o.owner}</td>
                  <td style={{ fontSize: 13 }}>{fmtDate(o.lastModified)}</td>
                  <td><span className={`badge ${st.className}`}>{st.label}</span></td>
                  <td>
                    <a href="#">Editar →</a>
                  </td>
                </tr>
              );
            })}
            {filtered.length === 0 && (
              <tr><td colSpan={8} style={{ textAlign: "center", padding: 24, color: "var(--color-text-secondary)" }}>Nenhuma operação encontrada.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="card">
        <h3>Governança do catálogo</h3>
        <ul style={{ lineHeight: 1.8 }}>
          <li>CRUD restrito ao perfil <code>curador-regras</code> (proposta) + <code>aprovador-regras</code> (promoção).</li>
          <li>Cada operação tem um <strong>code estável</strong> usado pela API e pelas regras DMN — alterações de code são <strong>breaking change</strong>.</li>
          <li>Operações em <strong>rascunho</strong> não são aceitas pela API até serem promovidas a <strong>ativas</strong>.</li>
          <li>Operações <strong>depreciadas</strong> continuam aceitas por 6 meses após depreciação (janela de migração).</li>
        </ul>
      </div>
    </>
  );
}
