"use client";
import { useMemo, useState } from "react";
import Link from "next/link";
import { documents, metrics } from "@/lib/mocks";
import { DocStatusBadge } from "@/components/StatusBadge";

const fmtDate = (iso: string) => new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" });
const fmtPct = (n: number) => `${(n * 100).toFixed(1)}%`;
const fmtBRL = (n: number) => n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });

function elapsed(iso: string): string {
  const ms = Date.now() - new Date(iso).getTime();
  const min = Math.floor(ms / 60000);
  if (min < 1) return "agora há pouco";
  if (min < 60) return `há ${min} min`;
  const h = Math.floor(min / 60);
  if (h < 24) return `há ${h} h`;
  const d = Math.floor(h / 24);
  return `há ${d} d`;
}

export default function DashboardPage() {
  const [query, setQuery] = useState("");

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return documents;
    return documents.filter(
      (d) => d.cnpj.toLowerCase().includes(q) || d.razaoSocial.toLowerCase().includes(q) || d.fileName.toLowerCase().includes(q)
    );
  }, [query]);

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Dashboard</h2>
          <div className="subtitle">Visão geral do pipeline e dos documentos em processamento</div>
        </div>
        <button className="btn btn--primary">+ Novo upload</button>
      </div>

      <div className="grid-3">
        <div className="metric-card">
          <div className="metric-label">Decisões hoje</div>
          <div className="metric-value">{metrics.decisionsToday}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>
            {fmtPct(metrics.approvedRate)} aprovadas · {fmtPct(metrics.rejectedRate)} reprovadas · {fmtPct(metrics.manualRate)} manual
          </div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-approved)" }}>
          <div className="metric-label">p95 latência</div>
          <div className="metric-value">{metrics.p95LatencyMs}ms</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>SLO: &lt; 2.000 ms ✓</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-info)" }}>
          <div className="metric-label">Disponibilidade 30d</div>
          <div className="metric-value">{fmtPct(metrics.uptime30d)}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>SLO: ≥ 99% ✓ · Custo médio {fmtBRL(metrics.costPerDecisionBRL)}/decisão</div>
        </div>
      </div>

      <div className="card">
        <h3>Upload de documento societário</h3>
        <div className="dropzone" role="button" tabIndex={0} aria-label="Arraste um documento aqui">
          📄 Arraste e solte um <strong>contrato social</strong>, <strong>procuração</strong> ou <strong>alteração contratual</strong>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)", marginTop: 8 }}>
            PDF ou imagem · até 50 MB · validação antivírus + extração via IA Gen
          </div>
        </div>
      </div>

      <div className="card">
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12, marginBottom: 12 }}>
          <h3 style={{ margin: 0 }}>Documentos recentes ({filtered.length})</h3>
          <input
            className="input"
            style={{ maxWidth: 360 }}
            type="search"
            placeholder="Buscar por CNPJ, razão social ou arquivo…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            aria-label="Buscar documentos"
          />
        </div>
        <table>
          <thead>
            <tr>
              <th>Documento</th>
              <th>CNPJ</th>
              <th>Razão Social</th>
              <th>Tipo</th>
              <th>Enviado em</th>
              <th>Decorrido</th>
              <th>Status</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((d) => (
              <tr key={d.documentId}>
                <td><code style={{ fontSize: 12 }}>{d.fileName}</code></td>
                <td>{d.cnpj}</td>
                <td>{d.razaoSocial}</td>
                <td>{d.tipoSocietario}</td>
                <td>{fmtDate(d.uploadedAt)}</td>
                <td style={{ color: "var(--color-text-secondary)", fontSize: 13 }}>{elapsed(d.uploadedAt)}</td>
                <td><DocStatusBadge status={d.status} /></td>
                <td><Link href={`/documents/${d.documentId}`}>Abrir →</Link></td>
              </tr>
            ))}
            {filtered.length === 0 && (
              <tr><td colSpan={8} style={{ textAlign: "center", padding: 24, color: "var(--color-text-secondary)" }}>Nenhum documento encontrado para &quot;{query}&quot;.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
