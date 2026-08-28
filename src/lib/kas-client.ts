import { DEFAULT_KAS_RUN_URL } from "@/lib/kas-ids";

export { DEFAULT_KAS_RUN_URL, KAS_JOURNEY, extractExecutionId, newCorrelationId } from "@/lib/kas-ids";

/**
 * @deprecated WF-07 — KAAS saiu do Next.js. Worker .NET consome outbox
 * e envia `X-Flow-Api-Key`. Rotas `/api/kas/*` devolvem 410.
 */
export async function postKasJourney(_body?: unknown): Promise<{
  ok: boolean;
  status: number;
  statusText: string;
  parsed: unknown;
  missingKey: boolean;
}> {
  return {
    ok: false,
    status: 410,
    statusText: "Gone",
    parsed: {
      error:
        "Integração KAAS saiu do Next.js (WF-07). Pipeline: Worker .NET consome outbox. Header X-Flow-Api-Key só no Worker (Kas__ApiKey)."
    },
    missingKey: false
  };
}
