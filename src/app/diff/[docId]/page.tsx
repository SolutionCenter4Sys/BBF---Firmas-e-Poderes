import Link from "next/link";
import { notFound } from "next/navigation";
import { diffs, findDocument } from "@/lib/mocks";

const fmtDate = (iso: string) => new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "long" });

const sevConfig = {
  alta: { className: "badge--err", label: "ALTA", color: "var(--color-status-rejected)" },
  media: { className: "badge--warn", label: "MÉDIA", color: "var(--color-status-manual)" },
  baixa: { className: "badge--info", label: "BAIXA", color: "var(--color-status-info)" },
  ok: { className: "badge--ok", label: "OK", color: "var(--color-status-approved)" }
} as const;

export default function DiffPage({ params }: { params: { docId: string } }) {
  const diff = diffs[params.docId];
  if (!diff) notFound();
  const doc = findDocument(diff.documentId);

  const counts = diff.itens.reduce(
    (acc, it) => ({ ...acc, [it.severidade]: (acc[it.severidade] ?? 0) + 1 }),
    {} as Record<string, number>
  );

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Diff: Documento × Fonte Oficial</h2>
          <div className="subtitle">
            <code>{diff.documentId}</code>
            {doc && <> · {doc.razaoSocial} ({doc.cnpj})</>} · fonte: <strong>{diff.fonteConsultada}</strong>
          </div>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <Link href={`/documents/${diff.documentId}`} className="btn btn--ghost">← Voltar ao documento</Link>
          <button className="btn btn--secondary">Reconsultar fonte</button>
        </div>
      </div>

      <div className="grid-3">
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-rejected)" }}>
          <div className="metric-label">Divergências altas</div>
          <div className="metric-value">{counts.alta ?? 0}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>Bloqueiam decisão</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-manual)" }}>
          <div className="metric-label">Divergências médias</div>
          <div className="metric-value">{counts.media ?? 0}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>Atenção do analista</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-approved)" }}>
          <div className="metric-label">Campos coincidentes</div>
          <div className="metric-value">{counts.ok ?? 0}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>{diff.cacheHit ? "📦 cache hit" : "🌐 consulta direta"} · {diff.latenciaMs}ms · {fmtDate(diff.consultadoEm)}</div>
        </div>
      </div>

      <div className="card">
        <h3>Comparação campo a campo</h3>
        <div style={{ overflowX: "auto" }}>
          <table>
            <thead>
              <tr>
                <th style={{ width: "20%" }}>Campo</th>
                <th style={{ width: "32%" }}>📄 Documento apresentado</th>
                <th style={{ width: "32%" }}>🏛️ {diff.fonteConsultada}</th>
                <th style={{ width: "16%" }}>Severidade</th>
              </tr>
            </thead>
            <tbody>
              {diff.itens.map((it, i) => {
                const cfg = sevConfig[it.severidade];
                return (
                  <tr key={i} style={{ background: it.severidade === "alta" ? "#FFF8F8" : it.severidade === "media" ? "#FFFBF0" : undefined }}>
                    <td style={{ fontWeight: 500, borderLeft: `4px solid ${cfg.color}` }}>{it.campo}</td>
                    <td style={{ fontSize: 13 }}>{it.valorDocumento}</td>
                    <td style={{ fontSize: 13 }}>{it.valorOficial}</td>
                    <td>
                      <span className={`badge ${cfg.className}`}>{cfg.label}</span>
                      {it.observacao && (
                        <div style={{ fontSize: 12, color: "var(--color-text-secondary)", marginTop: 4, fontStyle: "italic" }}>
                          {it.observacao}
                        </div>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      {(counts.alta ?? 0) > 0 && (
        <div className="banner banner--err" role="alert">
          ⚠️ <strong>Divergências críticas detectadas.</strong> A decisão tende a ser <strong>REPROVADA</strong> ou <strong>MANUAL</strong>. Recomenda-se análise antes de aprovar.
        </div>
      )}
      {(counts.alta ?? 0) === 0 && (counts.media ?? 0) > 0 && (
        <div className="banner banner--warn">
          ⚠️ Divergências de severidade média encontradas. Avalie se há impacto no escopo da operação solicitada.
        </div>
      )}
      {(counts.alta ?? 0) === 0 && (counts.media ?? 0) === 0 && (
        <div className="banner banner--ok">
          ✅ Documento <strong>coincide com a fonte oficial</strong>. Decisão pode prosseguir automatizada.
        </div>
      )}

      <div className="card">
        <h3>Outros documentos com diff disponível</h3>
        <ul style={{ lineHeight: 1.8 }}>
          {Object.keys(diffs).filter((k) => k !== diff.documentId).map((k) => (
            <li key={k}><Link href={`/diff/${k}`}><code>{k}</code> → {diffs[k].fonteConsultada}</Link></li>
          ))}
        </ul>
      </div>
    </>
  );
}
