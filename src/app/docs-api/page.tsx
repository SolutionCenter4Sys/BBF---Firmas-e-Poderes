"use client";
import { useState } from "react";
import { openApiEndpoints, type OpenApiEndpoint } from "@/lib/mocks";

const methodColors: Record<OpenApiEndpoint["method"], string> = {
  GET: "var(--color-status-info)",
  POST: "var(--color-status-approved)",
  PUT: "var(--color-status-manual)",
  DELETE: "var(--color-status-rejected)"
};

export default function DocsApiPage() {
  const [selected, setSelected] = useState<OpenApiEndpoint>(openApiEndpoints[0]);

  return (
    <>
      <div className="page-header">
        <div>
          <h2>API Docs (OpenAPI 3.x)</h2>
          <div className="subtitle">Spec versionado · base URL <code>https://api.bbf.bradesco.com.br/v1</code> · auth: OAuth2 + mTLS</div>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <button className="btn btn--secondary">Baixar openapi.json</button>
          <button className="btn btn--secondary">Baixar openapi.yaml</button>
        </div>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "320px 1fr", gap: 16 }}>
        <aside className="card" style={{ position: "sticky", top: 16, alignSelf: "start", maxHeight: "calc(100vh - 32px)", overflowY: "auto" }}>
          <h3>Endpoints ({openApiEndpoints.length})</h3>
          {openApiEndpoints.map((ep) => {
            const isSelected = selected.path === ep.path && selected.method === ep.method;
            return (
              <button
                key={`${ep.method} ${ep.path}`}
                onClick={() => setSelected(ep)}
                style={{
                  display: "block", width: "100%", textAlign: "left",
                  padding: 10, marginBottom: 6, borderRadius: 6,
                  background: isSelected ? "var(--color-bg-emphasis)" : "transparent",
                  border: isSelected ? "1px solid var(--color-brand-primary)" : "1px solid transparent",
                  cursor: "pointer", fontFamily: "inherit", fontSize: 13
                }}
              >
                <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
                  <span style={{ background: methodColors[ep.method], color: "white", padding: "2px 6px", borderRadius: 3, fontSize: 11, fontWeight: 600, fontFamily: "var(--font-family-mono)" }}>
                    {ep.method}
                  </span>
                  <code style={{ fontSize: 12, flex: 1, overflow: "hidden", textOverflow: "ellipsis" }}>{ep.path}</code>
                </div>
                <div style={{ fontSize: 12, color: "var(--color-text-secondary)", marginTop: 4 }}>{ep.summary}</div>
              </button>
            );
          })}
        </aside>

        <div>
          <div className="card">
            <div style={{ display: "flex", gap: 12, alignItems: "center", marginBottom: 12 }}>
              <span style={{ background: methodColors[selected.method], color: "white", padding: "6px 12px", borderRadius: 4, fontWeight: 600, fontFamily: "var(--font-family-mono)" }}>
                {selected.method}
              </span>
              <code style={{ fontSize: 16 }}>{selected.path}</code>
              <span className="badge badge--neutral" style={{ marginLeft: "auto" }}>{selected.tag}</span>
            </div>
            <h3 style={{ margin: "8px 0" }}>{selected.summary}</h3>
            <p style={{ color: "var(--color-text-secondary)", fontSize: 14, margin: 0 }}>{selected.description}</p>
          </div>

          {selected.params && selected.params.length > 0 && (
            <div className="card">
              <h3>Parâmetros</h3>
              <table>
                <thead>
                  <tr><th>Nome</th><th>Em</th><th>Tipo</th><th>Obrig.</th><th>Descrição</th></tr>
                </thead>
                <tbody>
                  {selected.params.map((p) => (
                    <tr key={p.name}>
                      <td><code>{p.name}</code></td>
                      <td><span className="badge badge--neutral">{p.in}</span></td>
                      <td style={{ fontSize: 13, fontFamily: "var(--font-family-mono)" }}>{p.type}</td>
                      <td>{p.required ? <span style={{ color: "var(--color-status-rejected)" }}>✓</span> : <span style={{ color: "var(--color-text-muted)" }}>—</span>}</td>
                      <td style={{ fontSize: 13 }}>{p.description}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          <div className="card">
            <h3>Respostas</h3>
            {selected.responses.map((r) => (
              <div key={r.code} style={{ marginBottom: 12, padding: 12, background: "var(--color-bg-emphasis)", borderRadius: 6, borderLeft: `4px solid ${r.code < 300 ? "var(--color-status-approved)" : r.code < 400 ? "var(--color-status-info)" : r.code < 500 ? "var(--color-status-manual)" : "var(--color-status-rejected)"}` }}>
                <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
                  <code style={{ fontSize: 14, fontWeight: 600 }}>{r.code}</code>
                  <span style={{ fontSize: 14 }}>{r.description}</span>
                </div>
                {r.example !== undefined && (
                  <pre style={{ marginTop: 8, fontSize: 12 }}>{JSON.stringify(r.example, null, 2)}</pre>
                )}
              </div>
            ))}
          </div>

          <div className="card">
            <h3>Try it out (mock)</h3>
            <p style={{ fontSize: 13, color: "var(--color-text-secondary)" }}>Execução real disponível após Step 8 (backend). Use o <a href="/api-helper">API Helper</a> para visualizar o payload de exemplo.</p>
            <button className="btn btn--primary" disabled>▶ Executar (indisponível no MVP mock)</button>
          </div>
        </div>
      </div>
    </>
  );
}
