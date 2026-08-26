"use client";
import { useEffect, useMemo, useRef, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  addSessionDocument,
  documents,
  getSessionDocuments,
  metrics,
  type Document
} from "@/lib/mocks";
import { saveKasResult } from "@/lib/kas-result";
import { newCorrelationId } from "@/lib/kas-ids";
import { DocStatusBadge } from "@/components/StatusBadge";

const MAX_UPLOAD_BYTES = 50 * 1024 * 1024;
const ACCEPT_ATTR = "application/pdf,image/png,image/jpeg,image/webp,image/tiff";

function isAllowedFile(file: File): boolean {
  if (file.type === "application/pdf" || file.type.startsWith("image/")) return true;
  return /\.(pdf|png|jpe?g|webp|tiff?)$/i.test(file.name);
}

const fmtDate = (iso: string) => new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" });
const fmtPct = (n: number) => `${(n * 100).toFixed(1)}%`;
const fmtBRL = (n: number) => n.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });

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

export default function DashboardPage() {
  const router = useRouter();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [query, setQuery] = useState("");
  const [uploaded, setUploaded] = useState<Document[]>([]);
  const [dragging, setDragging] = useState(false);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<{ kind: "ok" | "err"; text: string } | null>(null);

  useEffect(() => {
    setUploaded(getSessionDocuments());
  }, []);

  const catalog = useMemo(() => [...uploaded, ...documents], [uploaded]);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return catalog;
    return catalog.filter(
      (d) => d.cnpj.toLowerCase().includes(q) || d.razaoSocial.toLowerCase().includes(q) || d.fileName.toLowerCase().includes(q)
    );
  }, [query, catalog]);

  const openPicker = () => fileInputRef.current?.click();

  const ingestFiles = async (files: FileList | null) => {
    const file = files?.[0];
    if (!file || busy) return;

    if (file.size > MAX_UPLOAD_BYTES) {
      setNotice({ kind: "err", text: `${file.name} excede 50 MB.` });
      return;
    }
    if (!isAllowedFile(file)) {
      setNotice({ kind: "err", text: "Formato inválido. Envie PDF ou imagem (PNG, JPEG, WebP, TIFF)." });
      return;
    }

    setBusy(true);
    setNotice({ kind: "ok", text: `Enviando ${file.name} para a jornada KAAS (sync)… isso pode demorar.` });

    try {
      const form = new FormData();
      const correlationId = newCorrelationId();
      form.append("file", file);
      form.append("correlationId", correlationId);
      const res = await fetch("/api/kas/run", { method: "POST", body: form });
      const payload: unknown = await res.json().catch(() => ({ error: "Resposta inválida do proxy." }));
      const meta = payload && typeof payload === "object"
        ? (payload as { correlationId?: string; executionId?: string | null })
        : {};

      saveKasResult({
        at: new Date().toISOString(),
        fileName: file.name,
        httpStatus: res.status,
        ok: res.ok,
        body: payload,
        correlationId: meta.correlationId || correlationId,
        executionId: meta.executionId ?? null,
        action: "ingest"
      });

      const doc: Document = {
        documentId: `doc_${Date.now()}`,
        fileName: file.name,
        cnpj: "—",
        razaoSocial: "Identificação pendente (OCR)",
        tipoSocietario: "LTDA",
        uploadedAt: new Date().toISOString(),
        uploadedBy: "voce@bbf.com.br",
        status: res.ok ? "processando_ocr" : "falha",
        hash: "—",
        paginas: 0,
        confianca: { ocr: 0, iagen: 0, ner: 0 },
        socios: [],
        poderes: []
      };
      addSessionDocument(doc);
      setUploaded((prev) => [doc, ...prev]);
      router.push("/kas-result");
    } catch (err) {
      const message = err instanceof Error ? err.message : "Falha ao enviar o arquivo.";
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
        onChange={(e) => ingestFiles(e.target.files)}
      />
      <div className="page-header">
        <div>
          <h2>Dashboard</h2>
          <div className="subtitle">Visão geral do pipeline e dos documentos em processamento</div>
        </div>
        <button type="button" className="btn btn--primary" onClick={openPicker} disabled={busy}>
          {busy ? "Enviando…" : "+ Novo upload"}
        </button>
      </div>
      {notice && (
        <div className={`banner ${notice.kind === "ok" ? "banner--ok" : "banner--err"}`} role="status">
          {notice.kind === "ok" ? "✓" : "✕"} {notice.text}
        </div>
      )}

      <div className="grid-3">
        <div className="metric-card">
          <div className="metric-label">Decisões hoje</div>
          <div className="metric-value">{metrics.decisionsToday}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>
            {fmtPct(metrics.approvedRate)} aprovadas · {fmtPct(metrics.rejectedRate)} reprovadas · {fmtPct(metrics.manualRate)} manual
          </div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-approved)" }}>
          <div className="metric-label">p95 latência</div>
          <div className="metric-value">{metrics.p95LatencyMs}ms</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>SLO: &lt; 2.000 ms ✓</div>
        </div>
        <div className="metric-card" style={{ borderLeftColor: "var(--color-status-info)" }}>
          <div className="metric-label">Disponibilidade 30d</div>
          <div className="metric-value">{fmtPct(metrics.uptime30d)}</div>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>SLO: ≥ 99% ✓ · Custo médio {fmtBRL(metrics.costPerDecisionBRL)}/decisão</div>
        </div>
      </div>

      <div className="card">
        <h3>Upload de documento societário</h3>
        <div
          className={`dropzone${dragging ? " dropzone--active" : ""}`}
          role="button"
          tabIndex={0}
          aria-label="Enviar documento: clique ou arraste um arquivo"
          aria-disabled={busy}
          onClick={() => {
            if (!busy) openPicker();
          }}
          onKeyDown={(e) => {
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              openPicker();
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
            ingestFiles(e.dataTransfer.files);
          }}
        >
          📄 Arraste e solte um <strong>contrato social</strong>, <strong>procuração</strong> ou <strong>alteração contratual</strong>
          <div style={{ fontSize: 12, color: "var(--color-text-secondary)", marginTop: 8 }}>
            PDF ou imagem · até 50 MB · validação antivírus + extração via IA Gen
          </div>
        </div>
      </div>

      <div className="card">
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12, marginBottom: 12 }}>
          <h3 style={{ margin: 0 }}>Documentos recentes ({filtered.length})</h3>
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
            {filtered.map((d) => (
              <tr key={d.documentId}>
                <td><code style={{ fontSize: 12 }}>{d.fileName}</code></td>
                <td>{d.cnpj}</td>
                <td>{d.razaoSocial}</td>
                <td>{d.tipoSocietario}</td>
                <td>{fmtDate(d.uploadedAt)}</td>
                <td style={{ color: "var(--color-text-secondary)", fontSize: 13 }}>{elapsed(d.uploadedAt)}</td>
                <td><DocStatusBadge status={d.status} /></td>
                <td><Link href={`/documents/${d.documentId}`}>Abrir →</Link></td>
              </tr>
            ))}
            {filtered.length === 0 && (
              <tr><td colSpan={8} style={{ textAlign: "center", padding: 24, color: "var(--color-text-secondary)" }}>Nenhum documento encontrado para &quot;{query}&quot;.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
