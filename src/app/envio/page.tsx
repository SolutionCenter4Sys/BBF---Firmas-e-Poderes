"use client";
import { useRef, useState } from "react";
import { saveKasResult } from "@/lib/kas-result";
import { extractExecutionId, newCorrelationId } from "@/lib/kas-ids";
import JsonTree from "@/components/JsonTree";

const MAX_UPLOAD_BYTES = 50 * 1024 * 1024;
const ACCEPT_ATTR = "application/pdf,image/png,image/jpeg,image/webp,image/tiff";

type Stage = "idle" | "file" | "kaas" | "json" | "returned" | "error";

function isAllowedFile(file: File): boolean {
  if (file.type === "application/pdf" || file.type.startsWith("image/")) return true;
  return /\.(pdf|png|jpe?g|webp|tiff?)$/i.test(file.name);
}

function extractKasBody(payload: unknown): unknown {
  if (payload && typeof payload === "object" && "body" in payload) {
    return (payload as { body: unknown }).body;
  }
  return payload;
}

function readMeta(payload: unknown): { correlationId: string | null; executionId: string | null } {
  if (!payload || typeof payload !== "object") {
    return { correlationId: null, executionId: extractExecutionId(payload) };
  }
  const obj = payload as { correlationId?: unknown; executionId?: unknown };
  const correlationId = typeof obj.correlationId === "string" ? obj.correlationId : null;
  const executionId =
    typeof obj.executionId === "string"
      ? obj.executionId
      : extractExecutionId(payload);
  return { correlationId, executionId };
}

export default function EnvioKaasPage() {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File | null>(null);
  const [dragging, setDragging] = useState(false);
  const [busy, setBusy] = useState(false);
  const [returning, setReturning] = useState(false);
  const [stage, setStage] = useState<Stage>("idle");
  const [notice, setNotice] = useState<{ kind: "ok" | "err"; text: string } | null>(null);
  const [kasBody, setKasBody] = useState<unknown>(null);
  const [returnBody, setReturnBody] = useState<unknown>(null);
  const [copied, setCopied] = useState(false);
  const [correlationId, setCorrelationId] = useState<string | null>(null);
  const [executionId, setExecutionId] = useState<string | null>(null);

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
    setKasBody(null);
    setReturnBody(null);
    setCorrelationId(null);
    setExecutionId(null);
    setNotice({ kind: "ok", text: `${next.name} selecionado. Clique em Enviar para o KAAS.` });
  };

  const send = async () => {
    if (!file || busy || returning) return;
    setBusy(true);
    setStage("kaas");
    setReturnBody(null);
    const corr = newCorrelationId();
    setCorrelationId(corr);
    setNotice({ kind: "ok", text: "Enviando para o KAAS (sync)… o KAAS gera o JSON. Pode demorar." });

    try {
      const form = new FormData();
      form.append("file", file);
      form.append("correlationId", corr);
      const res = await fetch("/api/kas/run", { method: "POST", body: form });
      const payload: unknown = await res.json().catch(() => ({ error: "Resposta inválida do proxy." }));
      const body = extractKasBody(payload);
      const meta = readMeta(payload);

      setExecutionId(meta.executionId);
      if (meta.correlationId) setCorrelationId(meta.correlationId);

      saveKasResult({
        at: new Date().toISOString(),
        fileName: file.name,
        httpStatus: res.status,
        ok: res.ok,
        body: payload,
        correlationId: meta.correlationId || corr,
        executionId: meta.executionId,
        action: "ingest"
      });

      setKasBody(body);
      if (res.ok) {
        setStage("json");
        setNotice({
          kind: "ok",
          text: "KAAS retornou JSON. Confira e clique em Devolver JSON para o KAAS."
        });
      } else {
        setStage("error");
        const errObj = payload as { error?: string; detail?: string };
        setNotice({ kind: "err", text: errObj.error || errObj.detail || `HTTP ${res.status}` });
      }
    } catch (err) {
      setStage("error");
      setNotice({ kind: "err", text: err instanceof Error ? err.message : "Falha ao enviar." });
    } finally {
      setBusy(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  };

  const sendBack = async () => {
    if (kasBody == null || !correlationId || busy || returning) return;
    setReturning(true);
    setNotice({ kind: "ok", text: "Devolvendo JSON para a mesma jornada (action: result)…" });

    try {
      const res = await fetch("/api/kas/return", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          correlationId,
          executionId,
          fileName: file?.name || "",
          result: kasBody
        })
      });
      const payload: unknown = await res.json().catch(() => ({ error: "Resposta inválida do proxy." }));
      const body = extractKasBody(payload);

      saveKasResult({
        at: new Date().toISOString(),
        fileName: file?.name || "",
        httpStatus: res.status,
        ok: res.ok,
        body: payload,
        correlationId,
        executionId,
        action: "result"
      });

      setReturnBody(body);
      if (res.ok) {
        setStage("returned");
        setNotice({ kind: "ok", text: "JSON devolvido para o KAAS na mesma jornada." });
      } else {
        setStage("error");
        const errObj = payload as { error?: string; detail?: string };
        setNotice({ kind: "err", text: errObj.error || errObj.detail || `HTTP ${res.status}` });
      }
    } catch (err) {
      setStage("error");
      setNotice({ kind: "err", text: err instanceof Error ? err.message : "Falha ao devolver." });
    } finally {
      setReturning(false);
    }
  };

  const copyJson = async () => {
    if (kasBody == null) return;
    await navigator.clipboard.writeText(JSON.stringify(kasBody, null, 2));
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const stepClass = (id: "file" | "kaas" | "json" | "callback") => {
    if (stage === "error" && (id === "kaas" || id === "json" || id === "callback")) return "pipeline-step";
    if (id === "callback") {
      if (stage === "returned") return "pipeline-step done";
      if (stage === "json") return "pipeline-step active";
      return "pipeline-step";
    }
    const order: Record<Stage, number> = {
      idle: 0,
      file: 1,
      kaas: 2,
      json: 3,
      returned: 4,
      error: 0
    };
    const target: Record<string, number> = { file: 1, kaas: 2, json: 3 };
    const now = order[stage];
    const t = target[id] ?? 0;
    if (now > t) return "pipeline-step done";
    if (now === t) return "pipeline-step active";
    return "pipeline-step";
  };

  const canReturn = kasBody != null && !!correlationId && !busy && !returning && stage !== "returned";

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Enviar para o KAAS</h2>
          <div className="subtitle">
            Arquivo → KAAS (ingest) → JSON → mesma jornada (result).
          </div>
        </div>
        <button type="button" className="btn btn--primary" onClick={send} disabled={!file || busy || returning}>
          {busy ? "Enviando…" : "Enviar para o KAAS"}
        </button>
      </div>

      <div className="pipeline" aria-label="Fluxo KAAS">
        <div className={stepClass("file")}>1. Arquivo</div>
        <div className={stepClass("kaas")}>2. KAAS</div>
        <div className={stepClass("json")}>3. JSON</div>
        <div className={stepClass("callback")}>4. Devolver para o KAAS</div>
      </div>

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
          aria-label="Escolher arquivo para enviar para o KAAS"
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
        <h3>2–3. KAAS gera JSON</h3>
        <p style={{ margin: "0 0 12px", color: "var(--color-text-secondary)", fontSize: 14 }}>
          O arquivo vai para a jornada <code>testes-firmas-e-poderes</code> (
          <code>mode: sync</code>, <code>payload.action: ingest</code>).
          O KAAS processa e retorna JSON nesta tela.
        </p>
        {(correlationId || executionId) && (
          <p style={{ margin: "0 0 12px", fontSize: 13, color: "var(--color-text-muted)" }}>
            {correlationId && (
              <>
                <code>correlationId</code>: {correlationId}
              </>
            )}
            {correlationId && executionId && " · "}
            {executionId && (
              <>
                <code>executionId</code>: {executionId}
              </>
            )}
            {!executionId && correlationId && " · KAAS não devolveu executionId"}
          </p>
        )}
        {!kasBody && (
          <p style={{ margin: 0, color: "var(--color-text-muted)", fontSize: 14 }}>
            {busy ? "Aguardando a resposta do KAAS…" : "Ainda sem JSON. Escolha um arquivo e envie."}
          </p>
        )}
        {kasBody != null && (
          <>
            <div style={{ display: "flex", justifyContent: "flex-end", marginBottom: 8 }}>
              <button type="button" className="btn btn--secondary" onClick={copyJson}>
                {copied ? "Copiado" : "Copiar JSON"}
              </button>
            </div>
            {typeof kasBody === "object" ? <JsonTree value={kasBody} /> : <pre>{String(kasBody)}</pre>}
            <h3 style={{ marginTop: 20 }}>JSON bruto</h3>
            <pre>{JSON.stringify(kasBody, null, 2)}</pre>
          </>
        )}
      </div>

      <div className="card">
        <h3>4. Devolver o JSON para o KAAS</h3>
        <p style={{ margin: "0 0 8px", fontSize: 14 }}>
          Segundo POST na <strong>mesma</strong> jornada <code>testes-firmas-e-poderes/run</code>.
          Sem arquivo. Header <code>X-Flow-Api-Key</code>. Payload:
        </p>
        <pre style={{ fontSize: 12 }}>
{`{
  "mode": "sync",
  "payload": {
    "action": "result",
    "correlationId": "${correlationId || "corr_<uuid>"}",
    "executionId": "${executionId || "<se o KAAS devolver>"}",
    "fileName": "${file?.name || "arquivo.pdf"}",
    "result": { /* JSON da etapa 3 */ }
  }
}`}
        </pre>
        <p style={{ margin: "8px 0 0", fontSize: 13, color: "var(--color-text-muted)" }}>
          A jornada precisa ramificar em <code>payload.action === "result"</code>. Sem isso, o segundo
          /run tenta processar arquivo de novo.
        </p>
        <button
          type="button"
          className="btn btn--primary"
          disabled={!canReturn}
          onClick={sendBack}
          style={{ marginTop: 12 }}
        >
          {returning ? "Devolvendo…" : stage === "returned" ? "Já devolvido" : "Devolver JSON para o KAAS"}
        </button>
        {returnBody != null && (
          <div style={{ marginTop: 16 }}>
            <h3>Resposta da volta</h3>
            {typeof returnBody === "object" ? <JsonTree value={returnBody} /> : <pre>{String(returnBody)}</pre>}
            <pre>{JSON.stringify(returnBody, null, 2)}</pre>
          </div>
        )}
      </div>
    </>
  );
}
