"use client";
import { Suspense, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { JwtTokenForm } from "@/components/JwtTokenForm";
import JsonTree from "@/components/JsonTree";
import { STATUS_POLL_MS } from "@/app/documents/_lib/document-api";
import { listDocuments } from "@/hooks/useDocuments";
import { ApiError } from "@/lib/api";
import { AUTH_UNAUTHORIZED_EVENT } from "@/lib/auth";
import { kasJsonBody, loadKasRunView, type KasRunView } from "@/lib/kas-result";

function KasResultWorkspace() {
  const searchParams = useSearchParams();
  const documentIdParam = searchParams.get("documentId") ?? "";
  const [documentId, setDocumentId] = useState(documentIdParam);
  const [view, setView] = useState<KasRunView | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [unauthorized, setUnauthorized] = useState(false);
  const [copied, setCopied] = useState(false);
  const [loading, setLoading] = useState(true);
  const [reloadTick, setReloadTick] = useState(0);

  useEffect(() => {
    setDocumentId(documentIdParam);
  }, [documentIdParam]);

  useEffect(() => {
    const onUnauthorized = () => setUnauthorized(true);
    window.addEventListener(AUTH_UNAUTHORIZED_EVENT, onUnauthorized);
    return () => window.removeEventListener(AUTH_UNAUTHORIZED_EVENT, onUnauthorized);
  }, []);

  useEffect(() => {
    let cancelled = false;

    const resolveId = async (): Promise<string | null> => {
      if (documentId.trim()) return documentId.trim();
      const docs = await listDocuments();
      const latest = [...docs].sort(
        (a, b) => new Date(b.uploadedAt).getTime() - new Date(a.uploadedAt).getTime()
      )[0];
      return latest?.documentId ?? null;
    };

    const tick = async (id: string) => {
      const next = await loadKasRunView(id);
      if (cancelled) return next;
      setView(next);
      setUnauthorized(false);
      setError(null);
      return next;
    };

    const run = async () => {
      setLoading(true);
      try {
        const id = await resolveId();
        if (cancelled) return;
        if (!id) {
          setView(null);
          setError(null);
          return;
        }
        if (!documentId.trim()) setDocumentId(id);
        const next = await tick(id);
        if (cancelled || !next.polling) return;
        const poll = async () => {
          const again = await tick(id);
          if (cancelled || !again.polling) return;
          window.setTimeout(() => void poll(), STATUS_POLL_MS);
        };
        window.setTimeout(() => void poll(), STATUS_POLL_MS);
      } catch (err) {
        if (cancelled) return;
        if (err instanceof ApiError && err.status === 401) {
          setUnauthorized(true);
          setView(null);
        }
        setError(err instanceof Error ? err.message : "Falha ao carregar retorno KAAS.");
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    void run();
    return () => {
      cancelled = true;
    };
  }, [documentId, reloadTick]);

  const kasBody = useMemo(() => (view ? kasJsonBody(view) : null), [view]);

  const copy = async () => {
    if (kasBody == null) return;
    await navigator.clipboard.writeText(JSON.stringify(kasBody, null, 2));
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  if (loading && !view) {
    return (
      <div className="card">
        <p style={{ margin: 0, color: "var(--color-text-secondary)" }}>Carregando GET /v1/documents…</p>
      </div>
    );
  }

  if (!view) {
    return (
      <>
        <div className="page-header">
          <div>
            <h2>Retorno KAAS</h2>
            <div className="subtitle">Fonte: Postgres via API .NET — sem sessionStorage</div>
          </div>
          <Link href="/envio" className="btn btn--primary">Enviar documento</Link>
        </div>
        {unauthorized && (
          <JwtTokenForm
            roleLabel="operador"
            hint="GET /v1/documents exige Bearer. Cole JWT operador (sessionStorage bbf.access_token)."
            onSaved={async () => {
              setUnauthorized(false);
              setReloadTick((n) => n + 1);
            }}
          />
        )}
        {error && (
          <div className="banner banner--err" role="alert">✕ {error}</div>
        )}
        <div className="card">
          <p style={{ margin: 0, color: "var(--color-text-secondary)" }}>
            Nenhum documento na API. Envie em <Link href="/envio">/envio</Link>. Worker grava result em <code>kas_runs</code>; esta tela lê status + canônico.
          </p>
        </div>
      </>
    );
  }

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Retorno KAAS</h2>
          <div className="subtitle">
            Worker <code>action: result</code> · <code>{view.documentId}</code> · {view.status}
          </div>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <Link href="/envio" className="btn btn--ghost">← Envio</Link>
          <Link href={`/documents/${view.documentId}`} className="btn btn--ghost">Documento</Link>
          <button type="button" className="btn btn--secondary" onClick={() => void copy()}>
            {copied ? "Copiado" : "Copiar JSON"}
          </button>
        </div>
      </div>

      {unauthorized && (
        <JwtTokenForm
          roleLabel="operador"
          hint="GET /v1/documents exige Bearer. Cole JWT operador (sessionStorage bbf.access_token)."
          onSaved={async () => {
            setUnauthorized(false);
            setReloadTick((n) => n + 1);
          }}
        />
      )}

      {error && (
        <div className="banner banner--err" role="alert">✕ {error}</div>
      )}

      <div className={`banner ${view.ok ? "banner--ok" : "banner--err"}`} role="status">
        {view.ok ? "✓" : "✕"} {view.status}
        {view.polling
          ? " — worker ainda processa (poll)"
          : view.ok
            ? " — result persistido pelo worker"
            : " — pipeline falhou"}
      </div>

      <div className="grid-3">
        <div className="metric-card">
          <div className="metric-label">Arquivo</div>
          <div className="metric-value" style={{ fontSize: 16 }}>{view.fileName}</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: view.ok ? "var(--color-status-approved)" : "var(--color-status-rejected)" }}>
          <div className="metric-label">Status</div>
          <div className="metric-value" style={{ fontSize: 16 }}>{view.status}</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-info)" }}>
          <div className="metric-label">Quando</div>
          <div className="metric-value" style={{ fontSize: 16 }}>
            {new Date(view.uploadedAt).toLocaleString("pt-BR")}
          </div>
        </div>
      </div>

      {view.correlationId && (
        <p style={{ fontSize: 13, color: "var(--color-text-muted)" }}>
          <code>correlationId</code>: {view.correlationId}
        </p>
      )}

      <div className="card">
        <h3>Corpo (canônico / status)</h3>
        {kasBody && typeof kasBody === "object" ? (
          <JsonTree value={kasBody} />
        ) : (
          <pre>{kasBody == null ? "(vazio)" : String(kasBody)}</pre>
        )}
      </div>

      <div className="card">
        <h3>JSON bruto</h3>
        <pre>{JSON.stringify(kasBody, null, 2)}</pre>
      </div>
    </>
  );
}

export default function KasResultPage() {
  return (
    <Suspense fallback={<div className="card">Carregando retorno KAAS…</div>}>
      <KasResultWorkspace />
    </Suspense>
  );
}
