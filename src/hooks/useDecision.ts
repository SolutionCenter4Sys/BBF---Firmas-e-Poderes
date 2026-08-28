import type { DecisionEvidence, DecisionRecord, DecisionStatus } from "@/domain";
import { ApiError, apiFetch, parseApiProblem, readApiBody } from "@/lib/api";

const DECISION_STATUSES: readonly DecisionStatus[] = ["APROVADO", "REPROVADO", "MANUAL"];

export interface EvaluationRequest {
  documentId: string;
  cnpj?: string;
  operacao: string;
  valorOperacao?: number;
  currency?: string;
  signatariosSolicitados: string[];
  asOf?: string;
}

export interface AuthorityDecisionQuery {
  cnpj: string;
  operation: string;
  signers: string;
  valor?: number;
  idempotencyKey?: string;
}

function isDecisionStatus(value: string): value is DecisionStatus {
  return (DECISION_STATUSES as readonly string[]).includes(value);
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === "object" ? (value as Record<string, unknown>) : null;
}

function stringArray(value: unknown): string[] {
  if (!Array.isArray(value)) return [];
  return value.filter((item): item is string => typeof item === "string");
}

function parseEvidence(raw: unknown): DecisionEvidence | null {
  const row = asRecord(raw);
  if (!row) return null;
  const type = row.type === "fonte_oficial" ? "fonte_oficial" : "documento";
  const traceRaw = asRecord(row.trace);
  const detalhe = typeof row.detalhe === "string" ? row.detalhe : "";
  return {
    type,
    detalhe,
    fonte: typeof row.fonte === "string" ? row.fonte : undefined,
    trace: traceRaw && typeof traceRaw.page === "number"
      ? {
          page: traceRaw.page,
          offsetStart: typeof traceRaw.offsetStart === "number" ? traceRaw.offsetStart : 0,
          offsetEnd: typeof traceRaw.offsetEnd === "number" ? traceRaw.offsetEnd : 0,
          snippet: typeof traceRaw.snippet === "string" ? traceRaw.snippet : ""
        }
      : undefined
  };
}

export function parseDecisionRecord(raw: unknown): DecisionRecord | null {
  const row = asRecord(raw);
  if (!row || typeof row.decisionId !== "string" || row.decisionId.length === 0) return null;
  const statusRaw = typeof row.status === "string" ? row.status : "";
  const versions = asRecord(row.versions);
  return {
    decisionId: row.decisionId,
    documentId: typeof row.documentId === "string" ? row.documentId : "",
    cnpj: typeof row.cnpj === "string" ? row.cnpj : "",
    operacao: typeof row.operacao === "string" ? row.operacao : "",
    signatariosSolicitados: stringArray(row.signatariosSolicitados),
    status: isDecisionStatus(statusRaw) ? statusRaw : "MANUAL",
    motivos: stringArray(row.motivos),
    evidencias: Array.isArray(row.evidencias)
      ? row.evidencias.map(parseEvidence).filter((item): item is DecisionEvidence => item !== null)
      : [],
    versions: {
      rules: typeof versions?.rules === "string" ? versions.rules : "",
      canonical: typeof versions?.canonical === "string" ? versions.canonical : "",
      aiPrompt: typeof versions?.aiPrompt === "string" ? versions.aiPrompt : "",
      aiModel: typeof versions?.aiModel === "string" ? versions.aiModel : ""
    },
    evaluatedAt: typeof row.evaluatedAt === "string" ? row.evaluatedAt : new Date().toISOString(),
    latencyMs: typeof row.latencyMs === "number" ? row.latencyMs : 0
  };
}

async function expectDecision(response: Response, fallback: string): Promise<DecisionRecord> {
  const body = await readApiBody(response);
  if (!response.ok) {
    throw new ApiError(response.status, body, parseApiProblem(body, fallback));
  }
  const record = parseDecisionRecord(body);
  if (!record) {
    throw new ApiError(response.status, body, "DecisionRecord inválido.");
  }
  return record;
}

export async function evaluateDecision(request: EvaluationRequest): Promise<DecisionRecord> {
  const response = await apiFetch("/v1/decision/evaluate", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request)
  });
  return expectDecision(response, `POST /v1/decision/evaluate → ${response.status}`);
}

export async function replayDecision(decisionId: string): Promise<DecisionRecord> {
  const id = encodeURIComponent(decisionId);
  const response = await apiFetch(`/v1/decision/${id}/replay`, { method: "POST" });
  return expectDecision(response, `POST /v1/decision/${decisionId}/replay → ${response.status}`);
}

export async function getAuthorityDecision(query: AuthorityDecisionQuery): Promise<DecisionRecord> {
  const params = new URLSearchParams({
    cnpj: query.cnpj,
    operation: query.operation,
    signers: query.signers
  });
  if (query.valor !== undefined && !Number.isNaN(query.valor)) {
    params.set("valor", String(query.valor));
  }

  const headers = new Headers();
  if (query.idempotencyKey?.trim()) {
    headers.set("Idempotency-Key", query.idempotencyKey.trim());
  }

  const response = await apiFetch(`/v1/authority/decision?${params.toString()}`, {
    method: "GET",
    cache: "no-store",
    headers
  });
  return expectDecision(response, `GET /v1/authority/decision → ${response.status}`);
}
