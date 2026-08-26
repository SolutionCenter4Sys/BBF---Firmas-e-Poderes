import { DEFAULT_KAS_RUN_URL } from "@/lib/kas-ids";

export { DEFAULT_KAS_RUN_URL, KAS_JOURNEY, extractExecutionId, newCorrelationId } from "@/lib/kas-ids";

export async function postKasJourney(body: unknown): Promise<{
  ok: boolean;
  status: number;
  statusText: string;
  parsed: unknown;
  missingKey: boolean;
}> {
  const apiKey = process.env.KAS_API_KEY;
  const runUrl = process.env.KAS_RUN_URL || DEFAULT_KAS_RUN_URL;

  if (!apiKey) {
    return {
      ok: false,
      status: 500,
      statusText: "KAS_API_KEY ausente",
      parsed: {
        error: "KAS_API_KEY ausente. Defina a chave em .env.local e reinicie o npm run dev."
      },
      missingKey: true
    };
  }

  const kasResponse = await fetch(runUrl, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "X-Flow-Api-Key": apiKey
    },
    body: JSON.stringify(body)
  });

  const rawText = await kasResponse.text();
  let parsed: unknown = rawText;
  if (rawText) {
    try {
      parsed = JSON.parse(rawText);
    } catch {
      parsed = rawText;
    }
  } else {
    parsed = null;
  }

  return {
    ok: kasResponse.ok,
    status: kasResponse.status,
    statusText: kasResponse.statusText,
    parsed,
    missingKey: false
  };
}
