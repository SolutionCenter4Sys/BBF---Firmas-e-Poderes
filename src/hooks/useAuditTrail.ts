import type { AuditEvent } from "@/domain";
import { ApiError, apiFetch, parseApiProblem, readApiBody } from "@/lib/api";

export interface AuditTrailFilters {
  documentId?: string;
  correlationId?: string;
  from?: string;
  to?: string;
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function parseEvent(raw: unknown): AuditEvent | null {
  const row = asRecord(raw);
  if (!row || typeof row.eventId !== "string" || typeof row.correlationId !== "string") return null;
  const timestamp = typeof row.timestamp === "string"
    ? row.timestamp
    : typeof row.occurredAt === "string"
      ? row.occurredAt
      : "";
  if (!timestamp) return null;
  return {
    eventId: row.eventId,
    correlationId: row.correlationId,
    documentId: typeof row.documentId === "string" ? row.documentId : undefined,
    decisionId: typeof row.decisionId === "string" ? row.decisionId : undefined,
    type: typeof row.type === "string" ? row.type : "unknown",
    actor: typeof row.actor === "string" ? row.actor : "",
    timestamp,
    details: typeof row.details === "string" ? row.details : ""
  };
}

export async function getAuditTrail(filters: AuditTrailFilters = {}): Promise<AuditEvent[]> {
  const params = new URLSearchParams();
  if (filters.documentId?.trim()) params.set("documentId", filters.documentId.trim());
  if (filters.correlationId?.trim()) params.set("correlationId", filters.correlationId.trim());
  if (filters.from?.trim()) params.set("from", filters.from.trim());
  if (filters.to?.trim()) params.set("to", filters.to.trim());
  const query = params.toString();
  const suffix = query.length > 0 ? `?${query}` : "";

  const response = await apiFetch(`/v1/audit/trail${suffix}`, { method: "GET", cache: "no-store" });
  const body = await readApiBody(response);
  if (!response.ok) {
    throw new ApiError(response.status, body, parseApiProblem(body, `GET /v1/audit/trail → ${response.status}`));
  }
  if (!Array.isArray(body)) {
    throw new ApiError(response.status, body, "Trilha de auditoria inválida.");
  }
  return body.map(parseEvent).filter((item): item is AuditEvent => item !== null);
}

export function auditEventsToCsv(events: AuditEvent[]): string {
  const header = ["eventId", "correlationId", "documentId", "decisionId", "type", "actor", "timestamp", "details"];
  const rows = events.map((ev) =>
    [ev.eventId, ev.correlationId, ev.documentId ?? "", ev.decisionId ?? "", ev.type, ev.actor, ev.timestamp, ev.details]
      .map((cell) => `"${String(cell).replace(/"/g, "\"\"")}"`)
      .join(",")
  );
  return [header.join(","), ...rows].join("\n");
}
