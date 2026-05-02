import Link from "next/link";
import { audit } from "@/lib/mocks";

const fmtDate = (iso: string) => new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "long" });

const typeIcons: Record<string, string> = {
  "document.uploaded": "📤",
  "ocr.completed": "🔎",
  "iagen.completed": "🧠",
  "canonical.ready": "🌳",
  "official.queried": "🏛️",
  "decision.evaluated": "⚖️",
  "audit.persisted": "🛡️"
};

export default function AuditPage() {
  const grouped = audit.reduce<Record<string, typeof audit>>((acc, ev) => {
    (acc[ev.correlationId] ||= []).push(ev);
    return acc;
  }, {});

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Auditoria</h2>
          <div className="subtitle">Trilha imutável (append-only) — agrupada por correlationId</div>
        </div>
        <button className="btn btn--secondary">Exportar CSV</button>
      </div>

      <div className="card">
        <h3>Filtros</h3>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr 1fr", gap: 12 }}>
          <div><label>De</label><input className="input" type="date" /></div>
          <div><label>Até</label><input className="input" type="date" /></div>
          <div><label>correlationId</label><input className="input" placeholder="corr_xxxxxx" /></div>
          <div><label>Tipo de evento</label><select className="input"><option>Todos</option><option>decision.evaluated</option><option>document.uploaded</option></select></div>
        </div>
      </div>

      {Object.entries(grouped).map(([corrId, events]) => (
        <div key={corrId} className="card">
          <h3>
            <code>{corrId}</code>
            <span style={{ fontSize: 12, fontWeight: 400, color: "var(--color-text-secondary)", marginLeft: 12 }}>
              {events.length} eventos · documento{" "}
              {events[0].documentId && (<Link href={`/documents/${events[0].documentId}`}><code>{events[0].documentId}</code></Link>)}
            </span>
          </h3>
          <div style={{ position: "relative", paddingLeft: 24, borderLeft: "2px solid var(--color-border-default)" }}>
            {events.map((ev) => (
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
