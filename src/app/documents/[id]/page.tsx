"use client";
import { useMemo, useState } from "react";
import { notFound, useParams } from "next/navigation";
import Link from "next/link";
import { findDocument, findDecisionByDocumentId, diffs } from "@/lib/mocks";
import { DocStatusBadge, DecisionStatusBadge } from "@/components/StatusBadge";
import PipelineStatusBar from "@/components/PipelineStatusBar";

const fmtDate = (iso: string) => new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" });
const fmtBRL = (n: number) => n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });

export default function DocumentDetailPage() {
  const params = useParams<{ id: string }>();
  const doc = findDocument(params.id);
  if (!doc) notFound();
  const decision = findDecisionByDocumentId(doc.documentId);

  const today = new Date().toISOString().substring(0, 10);
  const [vigenteEm, setVigenteEm] = useState<string>(today);

  const poderesVigentes = useMemo(() => {
    const ref = new Date(vigenteEm);
    return doc.poderes.filter((p) => {
      const validFrom = new Date(p.vigencia.validFrom);
      const validTo = p.vigencia.validTo ? new Date(p.vigencia.validTo) : null;
      return ref >= validFrom && (!validTo || ref <= validTo);
    });
  }, [vigenteEm, doc.poderes]);

  return (
    <>
      <div className="page-header">
        <div>
          <h2>{doc.razaoSocial}</h2>
          <div className="subtitle">
            <code>{doc.documentId}</code> · {doc.fileName} · CNPJ {doc.cnpj} · {doc.tipoSocietario} · {doc.paginas} pgs
          </div>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <Link href="/" className="btn btn--ghost">← Voltar</Link>
          {decision && <Link href={`/decision?docId=${doc.documentId}`} className="btn btn--secondary">Ver decisão</Link>}
          <button className="btn btn--primary">Replay</button>
        </div>
      </div>

      <div className="card">
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
          <strong>Status do pipeline</strong>
          <DocStatusBadge status={doc.status} />
        </div>
        <PipelineStatusBar status={doc.status} />
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: 16, marginTop: 16 }}>
          {(["ocr", "iagen", "ner"] as const).map((k) => (
            <div key={k}>
              <div style={{ display: "flex", justifyContent: "space-between", fontSize: 13, marginBottom: 4 }}>
                <span style={{ textTransform: "uppercase", color: "var(--color-text-secondary)" }}>{k}</span>
                <strong>{(doc.confianca[k] * 100).toFixed(0)}%</strong>
              </div>
              <div className="meter"><div className="meter-fill" style={{ width: `${doc.confianca[k] * 100}%` }} /></div>
            </div>
          ))}
        </div>
        <div style={{ display: "flex", gap: 12, marginTop: 16, flexWrap: "wrap" }}>
          <Link href={`/documents/${doc.documentId}/structured`} className="btn btn--ghost" style={{ padding: "6px 14px", fontSize: 13 }}>
            🧠 Visão semiestruturada
          </Link>
          <Link href={`/documents/${doc.documentId}/canonical`} className="btn btn--ghost" style={{ padding: "6px 14px", fontSize: 13 }}>
            🌳 Modelo canônico
          </Link>
          <Link href={`/documents/${doc.documentId}/history`} className="btn btn--ghost" style={{ padding: "6px 14px", fontSize: 13 }}>
            📜 Histórico de versões
          </Link>
          {diffs[doc.documentId] && (
            <Link href={`/diff/${doc.documentId}`} className="btn btn--ghost" style={{ padding: "6px 14px", fontSize: 13 }}>
              ⚖️ Diff Documento × Oficial
            </Link>
          )}
        </div>
      </div>

      {doc.socios.length > 0 && (
        <div className="card">
          <h3>Sócios extraídos ({doc.socios.length})</h3>
          <table>
            <thead><tr><th>Nome</th><th>CPF</th><th>Qualificação</th><th>Cargo</th><th>Status</th></tr></thead>
            <tbody>
              {doc.socios.map((s) => (
                <tr key={s.personId}>
                  <td>{s.nome}</td>
                  <td><code>{s.cpf}</code></td>
                  <td>{s.qualificacao}</td>
                  <td>{s.cargo}</td>
                  <td>{s.status === "ativo" ? <span className="badge badge--ok">Ativo</span> : <span className="badge badge--err">Inativo</span>}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {doc.poderes.length > 0 && (
        <div className="card">
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 12, flexWrap: "wrap", gap: 12 }}>
            <h3 style={{ margin: 0 }}>Poderes extraídos (modelo canônico)</h3>
            <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
              <label htmlFor="vigenteEm" style={{ margin: 0, fontSize: 13 }}>Vigente em:</label>
              <input
                id="vigenteEm"
                className="input"
                type="date"
                value={vigenteEm}
                onChange={(e) => setVigenteEm(e.target.value)}
                style={{ width: 160, padding: "6px 10px", fontSize: 13 }}
              />
              <button className="btn btn--ghost" style={{ padding: "6px 12px", fontSize: 12 }} onClick={() => setVigenteEm(today)}>Hoje</button>
              <span className="badge badge--info">{poderesVigentes.length} de {doc.poderes.length} vigentes</span>
            </div>
          </div>
          {poderesVigentes.map((p) => (
            <div key={p.powerId} style={{ padding: 16, background: "var(--color-bg-emphasis)", borderRadius: 8, marginBottom: 12 }}>
              <div style={{ display: "grid", gridTemplateColumns: "2fr 1fr 1fr 1fr", gap: 16, alignItems: "center" }}>
                <div>
                  <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>Operação</div>
                  <strong>{p.operacao}</strong>
                </div>
                <div>
                  <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>Limite</div>
                  <strong>{fmtBRL(p.limite.value)}</strong>
                  <div style={{ fontSize: 11, color: "var(--color-text-muted)" }}>{p.limite.expression}</div>
                </div>
                <div>
                  <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>Modo</div>
                  <strong>{p.modoAssinatura.tipo === "isolada" ? "Isolada" : `Conjunta ${p.modoAssinatura.n}/${p.modoAssinatura.m}`}</strong>
                  {p.modoAssinatura.qualificacoes && (
                    <div style={{ fontSize: 11, color: "var(--color-text-muted)" }}>{p.modoAssinatura.qualificacoes.join(" + ")}</div>
                  )}
                </div>
                <div>
                  <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>Vigência</div>
                  <strong>desde {p.vigencia.validFrom}</strong>
                  {p.vigencia.validTo && <div style={{ fontSize: 11, color: "var(--color-text-muted)" }}>até {p.vigencia.validTo}</div>}
                </div>
              </div>
              <div style={{ marginTop: 12, padding: 12, background: "white", borderLeft: "4px solid var(--color-brand-secondary)", fontSize: 13, fontStyle: "italic", color: "var(--color-text-secondary)" }}>
                <div style={{ fontSize: 11, fontStyle: "normal", color: "var(--color-text-muted)", marginBottom: 4 }}>Origem: pg {p.sourceTrace.page}, offset {p.sourceTrace.offsetStart}-{p.sourceTrace.offsetEnd}</div>
                &quot;{p.sourceTrace.snippet}&quot;
              </div>
            </div>
          ))}
          {poderesVigentes.length === 0 && (
            <div style={{ padding: 24, textAlign: "center", color: "var(--color-text-secondary)" }}>
              Nenhum poder vigente em <strong>{vigenteEm}</strong>.
            </div>
          )}
        </div>
      )}

      {decision && (
        <div className="card">
          <h3>Decisão associada</h3>
          <div style={{ display: "flex", gap: 12, alignItems: "center", marginBottom: 12 }}>
            <DecisionStatusBadge status={decision.status} />
            <span><strong>{decision.operacao}</strong></span>
            <span style={{ color: "var(--color-text-secondary)", fontSize: 13 }}>· em {fmtDate(decision.evaluatedAt)} · {decision.latencyMs}ms</span>
          </div>
          <Link href={`/decision?docId=${doc.documentId}`}>Abrir decisão completa →</Link>
        </div>
      )}

      {doc.status === "falha" && (
        <div className="banner banner--err" role="alert" style={{ flexDirection: "column", alignItems: "flex-start" }}>
          <div>❌ <strong>Não foi possível processar este documento.</strong> A falha pode ter ocorrido na leitura óptica ou na extração via IA.</div>
          <div style={{ fontWeight: 400, fontSize: 13, marginTop: 8 }}>
            <strong>O que fazer agora:</strong>
            <ol style={{ margin: "4px 0 0 20px" }}>
              <li>Verifique se o PDF está legível (sem proteção, resolução adequada).</li>
              <li>Faça o reupload pelo Dashboard.</li>
              <li>Se persistir, abra um chamado de suporte com o <code>correlationId</code> abaixo.</li>
            </ol>
            <div style={{ marginTop: 8 }}>
              <code style={{ background: "white", padding: "4px 8px", borderRadius: 4 }}>corr_xyz123</code>{" "}
              <button className="btn btn--ghost" style={{ padding: "4px 12px", fontSize: 12 }}>Copiar correlationId</button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
