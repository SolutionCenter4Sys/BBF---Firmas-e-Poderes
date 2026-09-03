"use client";
import { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { DocStatusBadge } from "@/components/StatusBadge";
import { isProcessingStatus, uploadDocument, useDocuments } from "@/hooks/useDocuments";
import { ApiError } from "@/lib/api";
import { metrics } from "@/lib/mocks";
import type { DocStatus } from "@/domain";

const MAX_UPLOAD_BYTES = 50 * 1024 * 1024;
const ACCEPT_ATTR = [
  "application/pdf",
  "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
  "application/msword",
  "application/vnd.oasis.opendocument.text",
  "application/rtf",
  "text/rtf",
  ".pdf",
  ".docx",
  ".doc",
  ".odt",
  ".rtf"
].join(",");
const SLO_P95_MS = 2000;
const SLO_UPTIME = 0.99;
const SLO_MANUAL_MAX = 0.15;

function sloBorder(kind: "ok" | "warn" | "fail"): string {
  if (kind === "ok") return "var(--color-status-approved)";
  if (kind === "warn") return "var(--color-status-manual)";
  return "var(--color-status-rejected)";
}

function p95Slo(ms: number): "ok" | "warn" | "fail" {
  if (ms < SLO_P95_MS) return "ok";
  if (ms < SLO_P95_MS * 1.25) return "warn";
  return "fail";
}

function uptimeSlo(rate: number): "ok" | "warn" | "fail" {
  if (rate >= SLO_UPTIME) return "ok";
  if (rate >= SLO_UPTIME - 0.02) return "warn";
  return "fail";
}

function decisionsSlo(count: number, manualRate: number): "ok" | "warn" | "fail" {
  if (count <= 0) return "fail";
  if (manualRate <= SLO_MANUAL_MAX) return "ok";
  if (manualRate <= 0.25) return "warn";
  return "fail";
}

function isAllowedFile(file: File): boolean {
  if (
    file.type === "application/pdf"
    || file.type === "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    || file.type === "application/msword"
    || file.type === "application/vnd.oasis.opendocument.text"
    || file.type === "application/rtf"
    || file.type === "text/rtf"
  ) return true;
  return /\.(pdf|docx?|odt|rtf)$/i.test(file.name);
}

const fmtDate = (iso: string) => new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" });
const fmtPct = (n: number) => `${(n * 100).toFixed(1)}%`;
const fmtBRL = (n: number) => n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
const dash = (value?: string) => {
  const v = value?.trim();
  return v ? v : "—";
};

function elapsed(iso: string): string {
  const ms = Date.now() - new Date(iso).getTime();
  const min = Math.floor(ms / 60000);
  if (min < 1) return "agora há pouco";
  if (min < 60) return `há ${min} min`;
  const h = Math.floor(min / 60);
  if (h < 24) return `há ${h} h`;
  const d = Math.floor(h / 24);
  return `há ${d} d`;
}

function pipelineLabel(status: DocStatus): string {
  if (status === "pendente") return "Na fila";
  if (status === "processando_ocr") return "OCR";
  if (status === "processando_iagen") return "Leitura KAAS";
  if (status === "processando_ner") return "NER";
  return status;
}

function pipelinePercent(status: DocStatus): number {
  if (status === "pendente") return 20;
  if (status === "processando_ocr") return 45;
  if (status === "processando_iagen") return 70;
  if (status === "processando_ner") return 88;
  return 30;
}

function PipelineActionProgress({ status }: { status: DocStatus }) {
  const pct = pipelinePercent(status);
  const label = pipelineLabel(status);
  return (
    <div
      className="kas-progress"
      role="progressbar"
      aria-valuemin={0}
      aria-valuemax={100}
      aria-valuenow={pct}
      aria-label={`Processando: ${label}`}
      title={label}
    >
      <div className="kas-progress__track">
        <div className="kas-progress__fill" style={{ width: `${pct}%` }} />
        <span className="kas-progress__sweep" />
      </div>
    </div>
  );
}

export default function DashboardPage() {
  const router = useRouter();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [query, setQuery] = useState("");
  const [dragging, setDragging] = useState(false);
  const [busy, setBusy] = useState(false);
  const [watchId, setWatchId] = useState<string | null>(null);
  const [pendingFile, setPendingFile] = useState<File | null>(null);
  const [notice, setNotice] = useState<{ kind: "ok" | "err"; text: string } | null>(null);
  const [showProcessingHint, setShowProcessingHint] = useState(false);
  const { items, loading, error, unauthorized, refresh } = useDocuments();

  const processing = useMemo(
    () => items.filter((d) => isProcessingStatus(d.status)),
    [items]
  );
  const processingKey = processing.map((d) => d.documentId).sort().join(",");

  useEffect(() => {
    if (!watchId) return;
    const current = items.find((d) => d.documentId === watchId);
    if (!current) return;
    if (isProcessingStatus(current.status)) return;
    if (current.status === "falha") {
      setWatchId(null);
      setNotice({
        kind: "err",
        text: current.lastError
          ? `Pipeline falhou: ${current.lastError}`
          : "Pipeline falhou. Use Reenviar para tentar de novo."
      });
      return;
    }
    setWatchId(null);
    router.push(`/documents/${current.documentId}`);
  }, [items, watchId, router]);

  useEffect(() => {
    if (!notice) return;
    const timer = window.setTimeout(() => setNotice(null), 10_000);
    return () => window.clearTimeout(timer);
  }, [notice]);

  useEffect(() => {
    if (!processingKey) {
      setShowProcessingHint(false);
      return;
    }
    setShowProcessingHint(true);
    const timer = window.setTimeout(() => setShowProcessingHint(false), 10_000);
    return () => window.clearTimeout(timer);
  }, [processingKey]);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return items;
    return items.filter(
      (d) =>
        dash(d.cnpj).toLowerCase().includes(q)
        || dash(d.razaoSocial).toLowerCase().includes(q)
        || d.fileName.toLowerCase().includes(q)
        || d.documentId.toLowerCase().includes(q)
    );
  }, [query, items]);

  const openPicker = () => fileInputRef.current?.click();

  const selectFile = (files: FileList | null) => {
    const file = files?.[0];
    if (!file || busy) return;

    if (file.size > MAX_UPLOAD_BYTES) {
      setNotice({ kind: "err", text: `${file.name} excede 50 MB.` });
      return;
    }
    if (!isAllowedFile(file)) {
      setNotice({ kind: "err", text: "Formato inválido. Envie PDF, DOCX, DOC, ODT ou RTF." });
      return;
    }

    setNotice(null);
    setPendingFile(file);
  };

  const cancelUpload = () => {
    setPendingFile(null);
    if (fileInputRef.current) fileInputRef.current.value = "";
  };

  const confirmUpload = async () => {
    const file = pendingFile;
    if (!file || busy) return;

    setPendingFile(null);
    setBusy(true);
    setNotice({ kind: "ok", text: `Persistindo ${file.name} e enviando para o KAAS…` });

    try {
      const accepted = await uploadDocument(file);
      setWatchId(accepted.documentId);
      setNotice({
        kind: "ok",
        text: `${file.name} persistido. O Dashboard acompanha o status.`
      });
      await refresh();
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : err instanceof Error
          ? err.message
          : "Falha ao enviar o arquivo.";
      setNotice({ kind: "err", text: message });
    } finally {
      setBusy(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  };

  return (
    <>
      <input
        ref={fileInputRef}
        type="file"
        accept={ACCEPT_ATTR}
        hidden
        onChange={(e) => selectFile(e.target.files)}
      />
      {pendingFile && (
        <div className="modal-backdrop" role="presentation">
          <section
            className="modal-card"
            role="alertdialog"
            aria-modal="true"
            aria-labelledby="confirm-upload-title"
            aria-describedby="confirm-upload-description"
          >
            <h3 id="confirm-upload-title">Confirmar envio do documento</h3>
            <p id="confirm-upload-description">
              O arquivo será persistido no Postgres e enviado ao KAAS.
              O Dashboard permanece aberto e atualiza o status sozinho.
            </p>
            <div className="modal-file">
              <span aria-hidden="true">📄</span>
              <div>
                <strong>{pendingFile.name}</strong>
                <small>{(pendingFile.size / 1024).toFixed(1)} KB</small>
              </div>
            </div>
            <div className="modal-actions">
              <button type="button" className="btn btn--ghost" onClick={cancelUpload}>
                Cancelar
              </button>
              <button type="button" className="btn btn--primary" onClick={() => void confirmUpload()}>
                Confirmar envio
              </button>
            </div>
          </section>
        </div>
      )}
      <div className="page-header">
        <div>
          <h2>Dashboard</h2>
          <div className="subtitle">Visão geral do pipeline e dos documentos em processamento</div>
        </div>
        <button type="button" className="btn btn--primary" onClick={openPicker} disabled={busy || unauthorized}>
          {busy ? "Enviando…" : "+ Novo upload"}
        </button>
      </div>
      {notice && (
        <div className={`banner ${notice.kind === "ok" ? "banner--ok" : "banner--err"}`} role="status">
          {notice.kind === "ok" ? "✓" : "✕"} {notice.text}
        </div>
      )}
      {error && !notice && !unauthorized && (
        <div className="banner banner--err" role="alert">
          ✕ {error}
        </div>
      )}
      {showProcessingHint && processing.length > 0 && (
        <div className="banner banner--ok" role="status">
          ✓ Leitura KAAS em andamento ({processing.map((d) => `${d.fileName}: ${pipelineLabel(d.status)}`).join(" · ")}).
          Não é necessário reenviar enquanto o status for processamento.
        </div>
      )}

      <div className="grid-3">
        <div className="metric-card" style={{ borderLeftColor: sloBorder(decisionsSlo(metrics.decisionsToday, metrics.manualRate)) }}>
          <div className="metric-label">Decisões hoje</div>
          <div className="metric-value">{metrics.decisionsToday}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>
            {fmtPct(metrics.approvedRate)} aprovadas · {fmtPct(metrics.rejectedRate)} reprovadas · {fmtPct(metrics.manualRate)} manual
          </div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: sloBorder(p95Slo(metrics.p95LatencyMs)) }}>
          <div className="metric-label">p95 latência</div>
          <div className="metric-value">{metrics.p95LatencyMs}ms</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>SLO: &lt; 2.000 ms {metrics.p95LatencyMs < SLO_P95_MS ? "✓" : "⚠"}</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: sloBorder(uptimeSlo(metrics.uptime30d)) }}>
          <div className="metric-label">Disponibilidade 30d</div>
          <div className="metric-value">{fmtPct(metrics.uptime30d)}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>SLO: ≥ 99% {metrics.uptime30d >= SLO_UPTIME ? "✓" : "⚠"} · Custo médio {fmtBRL(metrics.costPerDecisionBRL)}/decisão</div>
        </div>
      </div>

      <div className="card">
        <h3>Upload de documento societário</h3>
        <div
          className={`dropzone${dragging ? " dropzone--active" : ""}`}
          role="button"
          tabIndex={0}
          aria-label="Enviar documento: clique ou arraste um arquivo"
          aria-disabled={busy || unauthorized}
          onClick={() => {
            if (!busy && !unauthorized) openPicker();
          }}
          onKeyDown={(e) => {
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              if (!busy && !unauthorized) openPicker();
            }
          }}
          onDragOver={(e) => {
            e.preventDefault();
            if (!unauthorized) setDragging(true);
          }}
          onDragLeave={() => setDragging(false)}
          onDrop={(e) => {
            e.preventDefault();
            setDragging(false);
            if (!unauthorized) selectFile(e.dataTransfer.files);
          }}
        >
          📄 Arraste e solte um <strong>contrato social</strong>, <strong>procuração</strong> ou <strong>alteração contratual</strong>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)", marginTop: 8 }}>
            PDF, DOCX, DOC, ODT ou RTF · até 50 MB · seu documento fica protegido durante toda a análise
          </div>
        </div>
      </div>

      <div className="card">
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12, marginBottom: 12 }}>
          <h3 style={{ margin: 0 }}>Documentos recentes ({loading ? "…" : filtered.length})</h3>
          <input
            className="input"
            style={{ maxWidth: 360 }}
            type="search"
            placeholder="Buscar por CNPJ, razão social ou arquivo…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            aria-label="Buscar documentos"
          />
        </div>
        <table>
          <thead>
            <tr>
              <th>Documento</th>
              <th>CNPJ</th>
              <th>Razão Social</th>
              <th>Tipo</th>
              <th>Enviado em</th>
              <th>Decorrido</th>
              <th>Status</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr>
                <td colSpan={8} style={{ textAlign: "center", padding: 24, color: "var(--color-text-secondary)" }}>
                  Carregando GET /v1/documents…
                </td>
              </tr>
            )}
            {!loading && filtered.map((d) => (
              <tr key={d.documentId}>
                <td><code style={{ fontSize: 12 }}>{d.fileName}</code></td>
                <td>{dash(d.cnpj)}</td>
                <td>{dash(d.razaoSocial)}</td>
                <td>{dash(d.tipoSocietario)}</td>
                <td>{fmtDate(d.uploadedAt)}</td>
                <td style={{ color: "var(--color-text-secondary)", fontSize: 13 }}>{elapsed(d.uploadedAt)}</td>
                <td><DocStatusBadge status={d.status} /></td>
                <td>
                  {d.status === "falha" ? (
                    <Link href={`/documents/${d.documentId}#erro`}>Ver erro</Link>
                  ) : isProcessingStatus(d.status) ? (
                    <PipelineActionProgress status={d.status} />
                  ) : (
                    <Link href={`/documents/${d.documentId}`}>Abrir →</Link>
                  )}
                </td>
              </tr>
            ))}
            {!loading && filtered.length === 0 && (
              <tr>
                <td colSpan={8} style={{ textAlign: "center", padding: 24, color: "var(--color-text-secondary)" }}>
                  {query
                    ? `Nenhum documento encontrado para "${query}".`
                    : "Nenhum documento na API."}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
