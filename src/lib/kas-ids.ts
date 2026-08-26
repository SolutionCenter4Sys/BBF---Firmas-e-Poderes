export const DEFAULT_KAS_RUN_URL =
  "https://kaas-core-dev.up.railway.app/kas/triggers/journeys/testes-firmas-e-poderes/run";

export const KAS_JOURNEY = "testes-firmas-e-poderes";

const EXECUTION_ID_KEYS = ["executionId", "execution_id", "runId", "run_id"];

export function newCorrelationId(): string {
  return `corr_${crypto.randomUUID()}`;
}

export function extractExecutionId(value: unknown, depth = 0): string | null {
  if (!value || typeof value !== "object" || depth > 5) return null;
  const obj = value as Record<string, unknown>;

  for (const key of EXECUTION_ID_KEYS) {
    const v = obj[key];
    if (typeof v === "string" && v.trim()) return v.trim();
  }

  for (const v of Object.values(obj)) {
    const found = extractExecutionId(v, depth + 1);
    if (found) return found;
  }
  return null;
}
