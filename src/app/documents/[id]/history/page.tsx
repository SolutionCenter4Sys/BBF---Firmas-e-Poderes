import Link from "next/link";
import { notFound } from "next/navigation";
import { findDocument, documentHistory } from "@/lib/mocks";

const fmtDate = (iso: string) => new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "long" });
const fmtBytes = (n: number) => n < 1024 ? `${n} B` : n < 1_048_576 ? `${(n / 1024).toFixed(1)} KB` : `${(n / 1_048_576).toFixed(2)} MB`;

const tipoLabels = {
  raw: { label: "Original (raw)", icon: "📄", color: "var(--color-text-secondary)" },
  ocr: { label: "OCR", icon: "🔎", color: "var(--color-status-info)" },
  iagen: { label: "IA Gen (semiestruturado)", icon: "🧠", color: "#8e24aa" },
  ner: { label: "NER (entidades)", icon: "🏷️", color: "var(--color-brand-secondary)" },
  canonical: { label: "Canônico", icon: "🌳", color: "var(--color-status-approved)" }
} as const;

export default function HistoryPage({ params }: { params: { id: string } }) {
  const doc = findDocument(params.id);
  if (!doc) notFound();
  const versions = documentHistory[doc.documentId] ?? [];

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Histórico de versões</h2>
          <div className="subtitle">
            <code>{doc.documentId}</code> · {doc.razaoSocial} · {versions.length} versões persistidas (idempotência por hash)
          </div>
        </div>
        <Link href={`/documents/${doc.documentId}`} className="btn btn--ghost">← Voltar ao documento</Link>
      </div>

      {versions.length === 0 && (
        <div className="card">
          <p>Nenhuma versão registrada ainda.</p>
        </div>
      )}

      {versions.length > 0 && (
        <div className="card">
          <h3>Linha do tempo</h3>
          <div style={{ position: "relative", paddingLeft: 32, borderLeft: "2px solid var(--color-border-default)", marginLeft: 8 }}>
            {versions.map((v) => {
              const cfg = tipoLabels[v.tipo];
              return (
                <div key={v.versionId} style={{ position: "relative", padding: "16px 0" }}>
                  <div style={{ position: "absolute", left: -47, top: 16, width: 32, height: 32, borderRadius: "50%", background: "white", border: `2px solid ${cfg.color}`, display: "flex", alignItems: "center", justifyContent: "center", fontSize: 16 }}>
                    {cfg.icon}
                  </div>
                  <div style={{ background: "var(--color-bg-emphasis)", borderRadius: 8, padding: 16 }}>
                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 8 }}>
                      <div>
                        <strong>{cfg.label}</strong>
                        <span style={{ marginLeft: 12, fontSize: 12, color: "var(--color-text-secondary)" }}>
                          versão <code>{v.versionId}</code> · {fmtDate(v.criadoEm)}
                        </span>
                      </div>
                      <div style={{ display: "flex", gap: 12, fontSize: 13, color: "var(--color-text-secondary)" }}>
                        <span>{fmtBytes(v.tamanhoBytes)}</span>
                        <code style={{ fontSize: 12 }}>{v.hash}</code>
                      </div>
                    </div>
                    <div style={{ fontSize: 14 }}>{v.notas}</div>
                    <div style={{ display: "flex", gap: 8, marginTop: 12 }}>
                      <button className="btn btn--ghost" style={{ padding: "4px 12px", fontSize: 12 }}>Download</button>
                      <button className="btn btn--ghost" style={{ padding: "4px 12px", fontSize: 12 }}>Comparar com versão anterior</button>
                      {v.tipo === "canonical" && <button className="btn btn--ghost" style={{ padding: "4px 12px", fontSize: 12 }}>Usar como base para replay</button>}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}

      <div className="card">
        <h3>Política de retenção (resumida)</h3>
        <ul style={{ lineHeight: 1.8 }}>
          <li><strong>Original (raw):</strong> retido por 5 anos (documento jurídico) — alinhado com EP-06-F3.</li>
          <li><strong>Versões intermediárias (OCR/IA Gen/NER):</strong> retidas por 90 dias após decisão final.</li>
          <li><strong>Canônico:</strong> retido pelo mesmo período do original (5 anos).</li>
          <li>Toda exclusão é <strong>auditada</strong> com motivo, autor e timestamp.</li>
        </ul>
      </div>
    </>
  );
}
