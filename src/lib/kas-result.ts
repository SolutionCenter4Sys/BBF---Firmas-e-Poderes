export const KAS_RESULT_KEY = "kas:last-run";

export interface KasRunResult {
  at: string;
  fileName: string;
  httpStatus: number;
  ok: boolean;
  body: unknown;
  correlationId?: string;
  executionId?: string | null;
  action?: "ingest" | "result";
}

export function saveKasResult(result: KasRunResult): void {
  sessionStorage.setItem(KAS_RESULT_KEY, JSON.stringify(result));
}

export function loadKasResult(): KasRunResult | null {
  if (typeof window === "undefined") return null;
  const raw = sessionStorage.getItem(KAS_RESULT_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as KasRunResult;
  } catch {
    return null;
  }
}
