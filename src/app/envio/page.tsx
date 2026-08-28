"use client";
import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import { JwtTokenForm } from "@/components/JwtTokenForm";
import JsonTree from "@/components/JsonTree";
import { STATUS_POLL_MS } from "@/app/documents/_lib/document-api";
import { uploadDocument } from "@/hooks/useDocuments";
import { ApiError } from "@/lib/api";
import { AUTH_UNAUTHORIZED_EVENT } from "@/lib/auth";
import { newCorrelationId } from "@/lib/kas-ids";
import { kasJsonBody, loadKasRunView, resultAlreadyPosted, type KasRunView } from "@/lib/kas-result";

const MAX_UPLOAD_BYTES = 50 * 1024 * 1024;
const ACCEPT_ATTR = "application/pdf,image/png,image/jpeg,image/webp,image/tiff";

type Stage = "idle" | "file" | "api" | "worker" | "json" | "done" | "error";

function isAllowedFile(file: File): boolean {
  if (file.type === "application/pdf" || file.type.startsWith("image/")) return true;
  return /\.(pdf|png|jpe?g|webp|tiff?)$/i.test(file.name);
}

export default function EnvioKaasPage() {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File | null>(null);
  const [dragging, setDragging] = useState(false);
  const [busy, setBusy] = useState(false);
  const [stage, setStage] = useState<Stage>("idle");
  const [notice, setNotice] = useState<{ kind: "ok" | "err"; text: string } | null>(null);
  const [unauthorized, setUnauthorized] = useState(false);
  const [copied, setCopied] = useState(false);
  const [documentId, setDocumentId] = useState<string | null>(null);
  const [correlationId, setCorrelationId] = useState<string | null>(null);
  const [view, setView] = useState<KasRunView | null>(null);

  const kasBody = view ? kasJsonBody(view) : null;

  const pick = (files: FileList | null) => {
    const next = files?.[0];
    if (!next) return;
    if (next.size > MAX_UPLOAD_BYTES) {
      setNotice({ kind: "err", text: `${next.name} excede 50 MB.` });
      return;
    }
    if (!isAllowedFile(next)) {
      setNotice({ kind: "err", text: "Formato inválido. Envie PDF ou imagem." });
      return;
    }
    setFile(next);
    setStage("file");
    setView(null);
    setDocumentId(null);
    setCorrelationId(null);
    setNotice({ kind: "ok", text: `${next.name} selecionado. Clique em Enviar para iniciar a leitura completa no KAAS.` });
  };

  const send = async () => {
    if (!file || busy) return;
    setBusy(true);

    setStage("api");
    setView(null);
    const corr = newCorrelationId();
    setCorrelationId(corr);
    setNotice({
      kind: "ok",
      text: "Documento aceito. Worker enviará o conteúdo ao KAAS e persistirá o modelo canônico."
    });

    try {
      const accepted = await uploadDocument(file, corr);
      setUnauthorized(false);
      setDocumentId(accepted.documentId);
      if (accepted.correlationId) setCorrelationId(accepted.correlationId);
      setStage("worker");
      setNotice({
        kind: "ok",
        text: `${accepted.documentId} aceito (${accepted.status}). Aguardando leitura e extração do KAAS.`
      });
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        setUnauthorized(true);
        setStage("error");
        setNotice({ kind: "err", text: err.message });
        return;
      }
      setStage("error");
      setNotice({
        kind: "err",
        text: err instanceof Error ? err.message : "Falha ao enviar."
      });
    } finally {
      setBusy(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  };

  useEffect(() => {
    const onUnauthorized = () => setUnauthorized(true);
    window.addEventListener(AUTH_UNAUTHORIZED_EVENT, onUnauthorized);
    return () => window.removeEventListener(AUTH_UNAUTHORIZED_EVENT, onUnauthorized);
  }, []);

  useEffect(() => {
    if (!documentId) return;
    let cancelled = false;
    let timer: ReturnType<typeof setTimeout> | undefined;

    const tick = async () => {
      try {
        const next = await loadKasRunView(documentId);
        if (cancelled) return;
        setUnauthorized(false);
        setView(next);
        if (next.polling) {
          setStage("worker");
          timer = setTimeout(() => void tick(), STATUS_POLL_MS);
          return;
        }
        if (next.status === "falha") {
          setStage("error");
          setNotice({ kind: "err", text: "Pipeline falhou. Verifique o contrato e envie novamente." });
          return;
        }
        setStage(resultAlreadyPosted(next.status) ? "done" : "json");
        setNotice({
          kind: "ok",
          text: "Leitura concluída. Sócios, representantes e poderes estão no JSON canônico abaixo."
        });
      } catch (err) {
        if (cancelled) return;
        if (err instanceof ApiError && err.status === 401) {
          setUnauthorized(true);
          setStage("error");
          setNotice({ kind: "err", text: err.message });
          return;
        }
        setStage("error");
        setNotice({
          kind: "err",
          text: err instanceof Error ? err.message : "Falha ao consultar status."
        });
      }
    };

    void tick();
    return () => {
      cancelled = true;
      if (timer) clearTimeout(timer);
    };
  }, [documentId]);

  const copyJson = async () => {
    if (kasBody == null) return;
    await navigator.clipboard.writeText(JSON.stringify(kasBody, null, 2));
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const stepClass = (id: "file" | "api" | "worker" | "json") => {
    if (stage === "error" && (id === "api" || id === "worker" || id === "json")) return "pipeline-step";
    const order: Record<Stage, number> = {
      idle: 0,
      file: 1,
      api: 2,
      worker: 3,
      json: 4,
      done: 5,
      error: 0
    };
    const target: Record<string, number> = { file: 1, api: 2, worker: 3, json: 4 };
    const now = order[stage];
    const t = target[id] ?? 0;
    if (now > t) return "pipeline-step done";
    if (now === t) return "pipeline-step active";
    return "pipeline-step";
  };

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Enviar para o KAAS</h2>
          <div className="subtitle">
            Escolha o contrato → KAAS lê o documento completo → tela mostra o modelo canônico.
          </div>
        </div>
        <button
          type="button"
          className="btn btn--primary"
          onClick={() => void send()}
          disabled={!file || busy}
        >
          {busy ? "Enviando…" : "Enviar (API .NET)"}
        </button>
      </div>

      <div className="pipeline" aria-label="Fluxo KAAS">
        <div className={stepClass("file")}>1. Arquivo</div>
        <div className={stepClass("api")}>2. API .NET</div>
        <div className={stepClass("worker")}>3. Worker KAAS</div>
        <div className={stepClass("json")}>4. Dados extraídos</div>
      </div>

      {unauthorized && (
        <JwtTokenForm
          roleLabel="operador"
          hint="POST /v1/documents exige Bearer. Cole JWT operador (sessionStorage bbf.access_token)."
          onSaved={async () => {
            setUnauthorized(false);
            if (file && !documentId) await send();
          }}
        />
      )}

      {notice && (
        <div className={`banner ${notice.kind === "ok" ? "banner--ok" : "banner--err"}`} role="status">
          {notice.kind === "ok" ? "✓" : "✕"} {notice.text}
        </div>
      )}

      <div className="card">
        <h3>1. Escolher arquivo</h3>
        <input
          ref={fileInputRef}
          type="file"
          accept={ACCEPT_ATTR}
          hidden
          onChange={(e) => pick(e.target.files)}
        />
        <div
          className={`dropzone${dragging ? " dropzone--active" : ""}`}
          role="button"
          tabIndex={0}
          aria-label="Escolher arquivo para enviar via API .NET"
          onClick={() => fileInputRef.current?.click()}
          onKeyDown={(e) => {
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              fileInputRef.current?.click();
            }
          }}
          onDragOver={(e) => {
            e.preventDefault();
            setDragging(true);
          }}
          onDragLeave={() => setDragging(false)}
          onDrop={(e) => {
            e.preventDefault();
            setDragging(false);
            pick(e.dataTransfer.files);
          }}
        >
          {file ? (
            <>
              📄 <strong>{file.name}</strong>
              <div style={{ fontSize: 12, color: "var(--color-text-secondary)", marginTop: 8 }}>
                {(file.size / 1024).toFixed(1)} KB · {file.type || "tipo desconhecido"}
              </div>
            </>
          ) : (
            <>
              📄 Arraste o documento ou clique para escolher
              <div style={{ fontSize: 12, color: "var(--color-text-secondary)", marginTop: 8 }}>
                PDF ou imagem · até 50 MB
              </div>
            </>
          )}
        </div>
      </div>

      <div className="card">
        <h3>2–3. KAAS lê e estrutura o contrato</h3>
        <p style={{ margin: "0 0 12px", color: "var(--color-text-secondary)", fontSize: 14 }}>
          API enfileira o documento. Worker chama a jornada <code>testes-firmas-e-poderes</code>.
          A tela acompanha <code>GET /v1/documents/{"{id}"}/status</code> e carrega o canônico.
        </p>
        {(documentId || correlationId || view) && (
          <p style={{ margin: "0 0 12px", fontSize: 13, color: "var(--color-text-muted)" }}>
            {documentId && (
              <>
                <code>documentId</code>:{" "}
                <Link href={`/documents/${documentId}`}>{documentId}</Link>
              </>
            )}
            {documentId && correlationId && " · "}
            {correlationId && (
              <>
                <code>correlationId</code>: {correlationId}
              </>
            )}
            {view && (
              <>
                {" · "}
                <code>status</code>: {view.status}
                {view.polling ? " (poll 2,5 s)" : null}
              </>
            )}
          </p>
        )}
        {!kasBody && (
          <p style={{ margin: 0, color: "var(--color-text-muted)", fontSize: 14 }}>
            {busy || (view?.polling ?? false)
              ? "Aguardando worker…"
              : "Ainda sem JSON. Escolha um arquivo e envie."}
          </p>
        )}
        {kasBody != null && (
          <>
            <div style={{ display: "flex", justifyContent: "flex-end", marginBottom: 8, gap: 8 }}>
              {documentId && (
                <Link href={`/kas-result?documentId=${encodeURIComponent(documentId)}`} className="btn btn--ghost">
                  Abrir retorno
                </Link>
              )}
              <button type="button" className="btn btn--secondary" onClick={() => void copyJson()}>
                {copied ? "Copiado" : "Copiar JSON"}
              </button>
            </div>
            {typeof kasBody === "object" ? <JsonTree value={kasBody} /> : <pre>{String(kasBody)}</pre>}
            <h3 style={{ marginTop: 20 }}>JSON bruto</h3>
            <pre>{JSON.stringify(kasBody, null, 2)}</pre>
          </>
        )}
      </div>

    </>
  );
}
