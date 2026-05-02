"use client";
import { useState } from "react";
import { notFound, useParams } from "next/navigation";
import Link from "next/link";
import { findDocument, canonicalSchemaPreview } from "@/lib/mocks";

const fmtBRL = (n: number) => n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });

interface NodeProps {
  label: string;
  value?: string;
  badge?: string;
  badgeClass?: string;
  defaultOpen?: boolean;
  children?: React.ReactNode;
  meta?: string;
}

function TreeNode({ label, value, badge, badgeClass, defaultOpen = true, children, meta }: NodeProps) {
  const [open, setOpen] = useState(defaultOpen);
  const hasChildren = !!children;
  return (
    <div style={{ marginLeft: 0 }}>
      <button
        onClick={() => hasChildren && setOpen((o) => !o)}
        style={{
          display: "flex", alignItems: "center", gap: 8,
          padding: "6px 10px", border: "none", background: "transparent",
          cursor: hasChildren ? "pointer" : "default", fontFamily: "inherit",
          fontSize: 14, width: "100%", textAlign: "left",
          borderRadius: 6
        }}
      >
        {hasChildren && (
          <span style={{ fontSize: 10, color: "var(--color-text-muted)", width: 12 }}>{open ? "▼" : "▶"}</span>
        )}
        {!hasChildren && <span style={{ width: 12 }} />}
        <strong>{label}</strong>
        {value && <span style={{ color: "var(--color-text-secondary)" }}>: {value}</span>}
        {badge && <span className={`badge ${badgeClass ?? "badge--neutral"}`} style={{ marginLeft: 4 }}>{badge}</span>}
        {meta && <span style={{ marginLeft: "auto", fontSize: 12, color: "var(--color-text-muted)" }}>{meta}</span>}
      </button>
      {open && hasChildren && (
        <div style={{ borderLeft: "1px dashed var(--color-border-default)", marginLeft: 14, paddingLeft: 12 }}>
          {children}
        </div>
      )}
    </div>
  );
}

export default function CanonicalPage() {
  const params = useParams<{ id: string }>();
  const doc = findDocument(params.id);
  if (!doc) notFound();
  const [view, setView] = useState<"tree" | "json" | "schema">("tree");

  const canonicalJson = {
    documentId: doc.documentId,
    cnpj: doc.cnpj,
    razaoSocial: doc.razaoSocial,
    tipoSocietario: doc.tipoSocietario,
    versao: "1.0.0",
    pessoas: doc.socios,
    poderes: doc.poderes
  };

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Modelo Canônico</h2>
          <div className="subtitle">
            <code>{doc.documentId}</code> · {doc.razaoSocial} · schema <code>canonical-powers/v1.0.0</code>
          </div>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <Link href={`/documents/${doc.documentId}`} className="btn btn--ghost">← Voltar ao documento</Link>
          <button className="btn btn--secondary">Validar contra schema</button>
        </div>
      </div>

      <div className="card" style={{ padding: 0 }}>
        <div style={{ display: "flex", borderBottom: "1px solid var(--color-border-default)" }}>
          {([
            { key: "tree", label: "🌳 Árvore canônico" },
            { key: "json", label: "🧾 JSON" },
            { key: "schema", label: "📐 JSON Schema (v1)" }
          ] as const).map((t) => (
            <button
              key={t.key}
              onClick={() => setView(t.key)}
              className="btn btn--ghost"
              style={{
                borderRadius: 0, padding: "14px 20px",
                borderBottom: view === t.key ? "2px solid var(--color-brand-primary)" : "2px solid transparent",
                color: view === t.key ? "var(--color-brand-primary)" : undefined,
                fontWeight: view === t.key ? 600 : 400
              }}
            >{t.label}</button>
          ))}
        </div>

        <div style={{ padding: 20 }}>
          {view === "tree" && (
            <>
              <TreeNode label={`📄 Documento`} value={doc.documentId} badge={doc.tipoSocietario} badgeClass="badge--info" defaultOpen meta={`CNPJ ${doc.cnpj}`}>
                <TreeNode label="Metadados" defaultOpen={false}>
                  <TreeNode label="cnpj" value={doc.cnpj} />
                  <TreeNode label="razaoSocial" value={doc.razaoSocial} />
                  <TreeNode label="tipoSocietario" value={doc.tipoSocietario} />
                  <TreeNode label="schemaVersion" value="canonical-powers/v1.0.0" />
                </TreeNode>

                <TreeNode label="👥 pessoas" badge={`${doc.socios.length}`} badgeClass="badge--neutral">
                  {doc.socios.map((s) => (
                    <TreeNode
                      key={s.personId}
                      label={s.nome}
                      badge={s.status}
                      badgeClass={s.status === "ativo" ? "badge--ok" : "badge--err"}
                      defaultOpen={false}
                      meta={s.cargo}
                    >
                      <TreeNode label="personId" value={s.personId} />
                      <TreeNode label="cpf" value={s.cpf} />
                      <TreeNode label="qualificacao" value={s.qualificacao} />
                      <TreeNode label="cargo" value={s.cargo} />
                      <TreeNode label="status" value={s.status} />
                    </TreeNode>
                  ))}
                </TreeNode>

                <TreeNode label="⚖️ poderes" badge={`${doc.poderes.length}`} badgeClass="badge--neutral">
                  {doc.poderes.map((p) => (
                    <TreeNode
                      key={p.powerId}
                      label={p.operacao}
                      meta={fmtBRL(p.limite.value)}
                    >
                      <TreeNode label="powerId" value={p.powerId} />
                      <TreeNode label="pessoa" value={p.pessoa} />
                      <TreeNode label="operacao" value={p.operacao} />
                      <TreeNode label="limite" defaultOpen={true}>
                        <TreeNode label="currency" value={p.limite.currency} />
                        <TreeNode label="value" value={fmtBRL(p.limite.value)} />
                        <TreeNode label="expression (origem)" value={p.limite.expression} />
                      </TreeNode>
                      <TreeNode label="modoAssinatura" defaultOpen={true}>
                        <TreeNode label="tipo" value={p.modoAssinatura.tipo} badge={p.modoAssinatura.tipo} badgeClass={p.modoAssinatura.tipo === "isolada" ? "badge--info" : "badge--warn"} />
                        {p.modoAssinatura.n && <TreeNode label="n" value={String(p.modoAssinatura.n)} />}
                        {p.modoAssinatura.m && <TreeNode label="m" value={String(p.modoAssinatura.m)} />}
                        {p.modoAssinatura.qualificacoes && (
                          <TreeNode label="qualificacoes" value={p.modoAssinatura.qualificacoes.join(" + ")} />
                        )}
                      </TreeNode>
                      <TreeNode label="vigencia">
                        <TreeNode label="validFrom" value={p.vigencia.validFrom} />
                        {p.vigencia.validTo && <TreeNode label="validTo" value={p.vigencia.validTo} />}
                      </TreeNode>
                      <TreeNode label="🔗 sourceTrace" defaultOpen={false}>
                        <TreeNode label="page" value={String(p.sourceTrace.page)} />
                        <TreeNode label="offsetStart" value={String(p.sourceTrace.offsetStart)} />
                        <TreeNode label="offsetEnd" value={String(p.sourceTrace.offsetEnd)} />
                        <TreeNode label="snippet" value={`"${p.sourceTrace.snippet}"`} />
                      </TreeNode>
                    </TreeNode>
                  ))}
                </TreeNode>
              </TreeNode>
            </>
          )}

          {view === "json" && <pre>{JSON.stringify(canonicalJson, null, 2)}</pre>}

          {view === "schema" && <pre>{JSON.stringify(canonicalSchemaPreview, null, 2)}</pre>}
        </div>
      </div>

      <div className="card">
        <h3>Sobre o modelo canônico</h3>
        <ul style={{ lineHeight: 1.8 }}>
          <li>O modelo canônico é o <strong>contrato semântico</strong> consumido pelo Motor de Decisão (EP-04).</li>
          <li>Cada item carrega <strong>sourceTrace</strong> (página + offset + snippet) para garantir rastreabilidade até o documento original.</li>
          <li>O schema é <strong>versionado em SemVer</strong> com prioridade de backward compatibility.</li>
          <li>Validação automática contra schema acontece em <strong>todo build</strong> e em runtime ao construir o canônico.</li>
        </ul>
      </div>
    </>
  );
}
