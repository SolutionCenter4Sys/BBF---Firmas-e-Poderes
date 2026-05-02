import Link from "next/link";
import { notFound } from "next/navigation";
import { findDocument, documentSections } from "@/lib/mocks";

const fmtPct = (n: number) => `${(n * 100).toFixed(0)}%`;

export default function StructuredPage({ params }: { params: { id: string } }) {
  const doc = findDocument(params.id);
  if (!doc) notFound();
  const sections = documentSections[doc.documentId] ?? [];

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Visão semiestruturada</h2>
          <div className="subtitle">
            <code>{doc.documentId}</code> · {doc.razaoSocial} · {doc.paginas} páginas · prompt LLM <code>leitura-contrato-social@2.1.0</code>
          </div>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <Link href={`/documents/${doc.documentId}`} className="btn btn--ghost">← Voltar ao documento</Link>
          <button className="btn btn--secondary">Reprocessar com prompt v2.2.0 (beta)</button>
        </div>
      </div>

      {sections.length === 0 && (
        <div className="card">
          <p>Sem seções identificadas para este documento (ainda em processamento ou em estágio anterior).</p>
        </div>
      )}

      {sections.length > 0 && (
        <>
          <div className="grid-3">
            <div className="metric-card">
              <div className="metric-label">Seções identificadas</div>
              <div className="metric-value">{sections.length}</div>
            </div>
            <div className="metric-card" style={{ borderLeftColor: "var(--color-status-approved)" }}>
              <div className="metric-label">Confiança média</div>
              <div className="metric-value">{fmtPct(sections.reduce((s, x) => s + x.confianca, 0) / sections.length)}</div>
            </div>
            <div className="metric-card" style={{ borderLeftColor: "var(--color-status-manual)" }}>
              <div className="metric-label">Seções abaixo de 90%</div>
              <div className="metric-value">{sections.filter((s) => s.confianca < 0.9).length}</div>
            </div>
          </div>

          <div className="card">
            <h3>Seções extraídas</h3>
            {sections.map((s, i) => (
              <div key={i} style={{ padding: 16, marginBottom: 12, borderRadius: 8, background: "var(--color-bg-emphasis)", borderLeft: `4px solid ${s.confianca >= 0.9 ? "var(--color-status-approved)" : "var(--color-status-manual)"}` }}>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 8 }}>
                  <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
                    <strong>{i + 1}. {s.titulo}</strong>
                    <span className="badge badge--neutral">pg {s.paginaInicio}–{s.paginaFim}</span>
                  </div>
                  <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                    <span style={{ fontSize: 13, color: "var(--color-text-secondary)" }}>Confiança</span>
                    <strong>{fmtPct(s.confianca)}</strong>
                    <div style={{ width: 100 }} className="meter">
                      <div className="meter-fill" style={{ width: `${s.confianca * 100}%` }} />
                    </div>
                  </div>
                </div>
                <p style={{ fontSize: 14, margin: 0, color: "var(--color-text-secondary)" }}>{s.resumo}</p>
                <div style={{ display: "flex", gap: 8, marginTop: 8 }}>
                  <button className="btn btn--ghost" style={{ padding: "4px 12px", fontSize: 12 }}>Ver trecho original</button>
                  <button className="btn btn--ghost" style={{ padding: "4px 12px", fontSize: 12 }}>Reaprovar</button>
                  <button className="btn btn--ghost" style={{ padding: "4px 12px", fontSize: 12 }}>Marcar para correção</button>
                </div>
              </div>
            ))}
          </div>

          <div className="card">
            <h3>Próximos passos</h3>
            <ul style={{ lineHeight: 1.8 }}>
              <li>Revisar seções com confiança {"<"} 90% antes do canônico ser construído.</li>
              <li>Ajustes aprovados aqui são <strong>persistidos como nova versão semiestruturada</strong> e propagam para EP-02 (canônico).</li>
              <li>Cada reprocessamento conta no <strong>custo por decisão</strong> exibido no Dashboard (FinOps).</li>
            </ul>
          </div>
        </>
      )}
    </>
  );
}
