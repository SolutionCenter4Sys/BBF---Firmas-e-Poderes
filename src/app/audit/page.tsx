"use client";

import { FormEvent, useMemo, useState } from "react";
import Link from "next/link";
import { JwtTokenForm } from "@/components/JwtTokenForm";
import type { AuditEvent } from "@/domain";
import { auditEventsToCsv, getAuditTrail } from "@/hooks/useAuditTrail";
import { ApiError } from "@/lib/api";

const fmtDate = (iso: string) => new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "long" });

const typeIcons: Record<string, string> = {
  "document.uploaded": "📤",
  "ocr.completed": "🔎",
  "iagen.completed": "🧠",
  "canonical.ready": "🌳",
  "official.queried": "🏛️",
  "decision.evaluated": "⚖️",
  "kas.ingest": "📥",
  "kas.result": "📤",
  "audit.persisted": "🛡️"
};

function toIsoStart(date: string): string {
  return date ? `${date}T00:00:00.000Z` : "";
}

function toIsoEnd(date: string): string {
  return date ? `${date}T23:59:59.999Z` : "";
}

export default function AuditPage() {
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [correlationId, setCorrelationId] = useState("");
  const [documentId, setDocumentId] = useState("");
  const [eventType, setEventType] = useState("todos");
  const [events, setEvents] = useState<AuditEvent[]>([]);
  const [loading, setLoading] = useState(false);
  const [loaded, setLoaded] = useState(false);
  const [unauthorized, setUnauthorized] = useState(false);
  const [forbidden, setForbidden] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const queryTrail = async () => {
    setLoading(true);
    setError(null);
    try {
      const items = await getAuditTrail({
        documentId: documentId.trim() || undefined,
        correlationId: correlationId.trim() || undefined,
        from: from ? toIsoStart(from) : undefined,
        to: to ? toIsoEnd(to) : undefined
      });
      setEvents(items);
      setLoaded(true);
      setUnauthorized(false);
      setForbidden(false);
    } catch (err) {
      setEvents([]);
      setLoaded(true);
      if (err instanceof ApiError && err.status === 401) {
        setUnauthorized(true);
        setForbidden(false);
        setError(err.message);
      } else if (err instanceof ApiError && err.status === 403) {
        setForbidden(true);
        setError("403 — GET /v1/audit/trail exige perfil auditor ou admin.");
      } else {
        setError(err instanceof Error ? err.message : "Falha ao carregar a trilha.");
      }
    } finally {
      setLoading(false);
    }
  };

  const onFilter = (event: FormEvent) => {
    event.preventDefault();
    void queryTrail();
  };

  const filtered = useMemo(
    () => (eventType === "todos" ? events : events.filter((ev) => ev.type === eventType)),
    [events, eventType]
  );

  const grouped = useMemo(
    () => filtered.reduce<Record<string, AuditEvent[]>>((acc, ev) => {
      (acc[ev.correlationId] ||= []).push(ev);
      return acc;
    }, {}),
    [filtered]
  );

  const exportCsv = () => {
    const csv = auditEventsToCsv(filtered);
    const blob = new Blob([csv], { type: "text/csv;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = "audit-trail.csv";
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Auditoria</h2>
          <div className="subtitle">GET <code>/v1/audit/trail</code> — trilha imutável agrupada por correlationId</div>
        </div>
        <button type="button" className="btn btn--secondary" onClick={exportCsv} disabled={filtered.length === 0}>
          Exportar CSV
        </button>
      </div>

      {unauthorized && (
        <JwtTokenForm
          roleLabel="auditor"
          hint="GET /v1/audit/trail é restrito a auditor/admin. Operador recebe 403. Cole o JWT em sessionStorage (bbf.access_token)."
          onSaved={() => {
            setUnauthorized(false);
            void queryTrail();
          }}
        />
      )}

      {forbidden && !unauthorized && (
        <div className="banner banner--warn" role="status">
          ⚠️ Perfil sem <code>audit:read</code>. Troque o JWT para auditor ou admin.
        </div>
      )}

      {error && !forbidden && (
        <div className="banner banner--err" role="alert">✕ {error}</div>
      )}

      <form className="card" onSubmit={onFilter}>
        <h3>Filtros (API)</h3>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr 1fr", gap: 12 }}>
          <div><label htmlFor="audit-from">De</label><input id="audit-from" className="input" type="date" value={from} onChange={(e) => setFrom(e.target.value)} /></div>
          <div><label htmlFor="audit-to">Até</label><input id="audit-to" className="input" type="date" value={to} onChange={(e) => setTo(e.target.value)} /></div>
          <div><label htmlFor="audit-corr">correlationId</label><input id="audit-corr" className="input" placeholder="corr_xxxxxx" value={correlationId} onChange={(e) => setCorrelationId(e.target.value)} /></div>
          <div><label htmlFor="audit-doc">documentId</label><input id="audit-doc" className="input" placeholder="doc_001" value={documentId} onChange={(e) => setDocumentId(e.target.value)} /></div>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "1fr auto", gap: 12, marginTop: 12, alignItems: "end" }}>
          <div>
            <label htmlFor="audit-type">Tipo de evento (cliente)</label>
            <select id="audit-type" className="input" value={eventType} onChange={(e) => setEventType(e.target.value)}>
              <option value="todos">Todos</option>
              <option value="document.uploaded">document.uploaded</option>
              <option value="ocr.completed">ocr.completed</option>
              <option value="canonical.ready">canonical.ready</option>
              <option value="decision.evaluated">decision.evaluated</option>
              <option value="kas.ingest">kas.ingest</option>
              <option value="kas.result">kas.result</option>
            </select>
          </div>
          <button type="submit" className="btn btn--primary" disabled={loading}>
            {loading ? "Consultando…" : "Consultar trilha"}
          </button>
        </div>
      </form>

      {!loaded && !loading && (
        <div className="card">Informe um JWT auditor e clique em Consultar trilha (GET /v1/audit/trail).</div>
      )}

      {loaded && Object.keys(grouped).length === 0 && !loading && (
        <div className="card">Nenhum evento para os filtros aplicados.</div>
      )}

      {Object.entries(grouped).map(([corrId, group]) => (
        <div key={corrId} className="card">
          <h3>
            <code>{corrId}</code>
            <span style={{ fontSize: 12, fontWeight: 400, color: "var(--color-text-secondary)", marginLeft: 12 }}>
              {group.length} eventos · documento{" "}
              {group[0].documentId && (<Link href={`/documents/${group[0].documentId}`}><code>{group[0].documentId}</code></Link>)}
            </span>
          </h3>
          <div style={{ position: "relative", paddingLeft: 24, borderLeft: "2px solid var(--color-border-default)" }}>
            {group.map((ev) => (
              <div key={ev.eventId} style={{ position: "relative", padding: "8px 0" }}>
                <div style={{ position: "absolute", left: -33, top: 8, width: 18, height: 18, borderRadius: "50%", background: "white", border: "2px solid var(--color-brand-secondary)", display: "flex", alignItems: "center", justifyContent: "center", fontSize: 10 }}>
                  {typeIcons[ev.type] ?? "•"}
                </div>
                <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
                  <code style={{ fontSize: 12 }}>{ev.type}</code>
                  <span style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>{fmtDate(ev.timestamp)}</span>
                  <span className="badge badge--neutral">{ev.actor}</span>
                </div>
                <div style={{ fontSize: 14, marginTop: 4 }}>{ev.details}</div>
              </div>
            ))}
          </div>
        </div>
      ))}
    </>
  );
}
