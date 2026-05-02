"use client";
import { useMemo, useState } from "react";
import Link from "next/link";
import { reviewQueue, type ReviewMotivo } from "@/lib/mocks";

const fmtDate = (iso: string) => new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" });
const fmtPct = (n: number) => `${(n * 100).toFixed(0)}%`;

function elapsedHours(iso: string): number {
  return (Date.now() - new Date(iso).getTime()) / 3_600_000;
}

const motivoLabels: Record<ReviewMotivo, string> = {
  ocr_baixa_confianca: "OCR baixa confiança",
  iagen_baixa_confianca: "IA Gen baixa confiança",
  ner_baixa_confianca: "NER baixa confiança",
  clausula_ambigua: "Cláusula ambígua",
  qualidade_documento: "Qualidade do documento"
};

export default function ReviewQueuePage() {
  const [filterPrioridade, setFilterPrioridade] = useState<string>("todas");
  const [filterMotivo, setFilterMotivo] = useState<string>("todos");

  const filtered = useMemo(() => {
    return reviewQueue.filter((r) => {
      if (filterPrioridade !== "todas" && r.prioridade !== filterPrioridade) return false;
      if (filterMotivo !== "todos" && r.motivo !== filterMotivo) return false;
      return true;
    });
  }, [filterPrioridade, filterMotivo]);

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Fila de Revisão Humana</h2>
          <div className="subtitle">Documentos com baixa confiança em algum estágio do pipeline (OCR / IA Gen / NER)</div>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <button className="btn btn--secondary">Reatribuir</button>
          <button className="btn btn--primary">Atender próximo</button>
        </div>
      </div>

      <div className="grid-3">
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-rejected)" }}>
          <div className="metric-label">Em fila</div>
          <div className="metric-value">{reviewQueue.length}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>{reviewQueue.filter((r) => r.prioridade === "alta").length} prioridade alta</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-manual)" }}>
          <div className="metric-label">SLA crítico (&lt; 1h)</div>
          <div className="metric-value">{reviewQueue.filter((r) => elapsedHours(r.enfileiradoEm) > r.slaHoras - 1).length}</div>
        </div>
        <div className="metric-card">
          <div className="metric-label">Tempo médio em fila</div>
          <div className="metric-value">2h 15min</div>
        </div>
      </div>

      <div className="card">
        <h3>Filtros</h3>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: 12 }}>
          <div>
            <label>Prioridade</label>
            <select className="input" value={filterPrioridade} onChange={(e) => setFilterPrioridade(e.target.value)}>
              <option value="todas">Todas</option>
              <option value="alta">Alta</option>
              <option value="media">Média</option>
              <option value="baixa">Baixa</option>
            </select>
          </div>
          <div>
            <label>Motivo</label>
            <select className="input" value={filterMotivo} onChange={(e) => setFilterMotivo(e.target.value)}>
              <option value="todos">Todos</option>
              {Object.entries(motivoLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div>
            <label>Responsável</label>
            <select className="input">
              <option>Todos</option>
              <option>Apenas meus</option>
            </select>
          </div>
        </div>
      </div>

      <div className="card">
        <h3>Itens em revisão ({filtered.length})</h3>
        <table>
          <thead>
            <tr>
              <th>Documento</th>
              <th>CNPJ / Razão</th>
              <th>Motivo</th>
              <th>Score crítico</th>
              <th>Em fila há</th>
              <th>SLA</th>
              <th>Prioridade</th>
              <th>Responsável</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((r) => {
              const horas = elapsedHours(r.enfileiradoEm);
              const slaCritico = horas > r.slaHoras - 1;
              return (
                <tr key={r.reviewId}>
                  <td><Link href={`/documents/${r.documentId}`}><code>{r.documentId}</code></Link></td>
                  <td>{r.cnpj}<br /><span style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>{r.razaoSocial}</span></td>
                  <td>{r.motivoLegivel}</td>
                  <td>
                    <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                      <span><strong>{fmtPct(r.scoreCritico)}</strong></span>
                      <div style={{ flex: 1, minWidth: 60 }} className="meter">
                        <div className="meter-fill" style={{ width: `${r.scoreCritico * 100}%` }} />
                      </div>
                    </div>
                  </td>
                  <td>{horas < 1 ? `${Math.round(horas * 60)} min` : `${horas.toFixed(1)} h`}</td>
                  <td>
                    <span className={`badge ${slaCritico ? "badge--err" : "badge--ok"}`}>
                      {slaCritico ? "🔥" : "✓"} {r.slaHoras}h
                    </span>
                  </td>
                  <td>
                    <span className={`badge ${r.prioridade === "alta" ? "badge--err" : r.prioridade === "media" ? "badge--warn" : "badge--neutral"}`}>
                      {r.prioridade}
                    </span>
                  </td>
                  <td style={{ fontSize: 13 }}>{r.responsavel ?? <em style={{ color: "var(--color-text-muted)" }}>não atribuído</em>}</td>
                  <td>
                    <Link href={`/documents/${r.documentId}/structured`}>Revisar →</Link>
                  </td>
                </tr>
              );
            })}
            {filtered.length === 0 && (
              <tr><td colSpan={9} style={{ textAlign: "center", padding: 24, color: "var(--color-text-secondary)" }}>Nenhum item para os filtros aplicados.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="card">
        <h3>Observações</h3>
        <ul style={{ lineHeight: 1.8 }}>
          <li>Após a revisão e correção, o pipeline retoma <strong>automaticamente</strong> a partir do estágio onde havia parado.</li>
          <li>Itens com SLA crítico ({"<"} 1h restante) ficam <strong>destacados em vermelho</strong> e disparam alerta para o gestor.</li>
          <li>Toda ação de revisão é <strong>persistida na trilha de auditoria</strong> com o usuário e timestamp.</li>
        </ul>
      </div>
    </>
  );
}
