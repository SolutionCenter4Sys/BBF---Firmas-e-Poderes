import type { SourceHealth } from "@/domain";
import { ApiError, apiFetch, parseApiProblem, readApiBody } from "@/lib/api";

const STATUSES = ["operacional", "degradado", "indisponivel"] as const;
const BREAKERS = ["fechado", "meio-aberto", "aberto"] as const;

function isStatus(value: string): value is SourceHealth["status"] {
  return (STATUSES as readonly string[]).includes(value);
}

function isBreaker(value: string): value is SourceHealth["circuitBreaker"] {
  return (BREAKERS as readonly string[]).includes(value);
}

function num(value: unknown, fallback = 0): number {
  if (typeof value === "number" && Number.isFinite(value)) return value;
  if (typeof value === "string" && value.trim()) {
    const n = Number(value);
    if (Number.isFinite(n)) return n;
  }
  return fallback;
}

function str(value: unknown, fallback = ""): string {
  return typeof value === "string" ? value : fallback;
}

export function parseSourceHealth(raw: unknown): SourceHealth | null {
  if (!raw || typeof raw !== "object") return null;
  const row = raw as Record<string, unknown>;
  const sourceId = str(row.sourceId);
  if (!sourceId) return null;
  const statusRaw = str(row.status, "operacional");
  const breakerRaw = str(row.circuitBreaker, "fechado");
  const observacao = str(row.observacao);
  return {
    sourceId,
    nome: str(row.nome, sourceId),
    status: isStatus(statusRaw) ? statusRaw : "operacional",
    uptime24h: num(row.uptime24h),
    latenciaP95Ms: num(row.latenciaP95Ms),
    errorRate: num(row.errorRate),
    cacheHitRate: num(row.cacheHitRate),
    ultimaConsulta: str(row.ultimaConsulta) || new Date().toISOString(),
    circuitBreaker: isBreaker(breakerRaw) ? breakerRaw : "fechado",
    observacao: observacao.trim() ? observacao : undefined
  };
}

export async function getVerificationHealth(): Promise<SourceHealth[]> {
  const response = await apiFetch("/v1/verification/health", { method: "GET", cache: "no-store" });
  const body = await readApiBody(response);
  if (!response.ok) {
    throw new ApiError(
      response.status,
      body,
      parseApiProblem(body, `GET /v1/verification/health → ${response.status}`)
    );
  }
  if (!Array.isArray(body)) {
    throw new ApiError(response.status, body, "Lista de fontes inválida.");
  }
  return body.map(parseSourceHealth).filter((item): item is SourceHealth => item !== null);
}
