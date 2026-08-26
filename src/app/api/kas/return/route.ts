import { NextRequest, NextResponse } from "next/server";
import { KAS_JOURNEY, postKasJourney } from "@/lib/kas-client";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";
export const maxDuration = 300;

export async function POST(req: NextRequest) {
  let input: {
    correlationId?: unknown;
    executionId?: unknown;
    fileName?: unknown;
    result?: unknown;
  };

  try {
    input = await req.json();
  } catch {
    return NextResponse.json({ error: "JSON inválido." }, { status: 400 });
  }

  const correlationId =
    typeof input.correlationId === "string" ? input.correlationId.trim() : "";
  if (!correlationId) {
    return NextResponse.json({ error: "correlationId obrigatório." }, { status: 400 });
  }
  if (input.result === undefined) {
    return NextResponse.json({ error: "result obrigatório." }, { status: 400 });
  }

  const executionId =
    typeof input.executionId === "string" && input.executionId.trim()
      ? input.executionId.trim()
      : null;
  const fileName = typeof input.fileName === "string" ? input.fileName : "";

  const payload: Record<string, unknown> = {
    action: "result",
    correlationId,
    fileName,
    result: input.result
  };
  if (executionId) payload.executionId = executionId;

  const started = Date.now();
  let kas;
  try {
    kas = await postKasJourney({
      mode: "sync",
      payload
    });
  } catch (err) {
    const message = err instanceof Error ? err.message : "Falha de rede ao chamar a KAAS.";
    return NextResponse.json(
      { error: "Não foi possível conectar à API KAAS.", detail: message },
      { status: 502 }
    );
  }

  if (kas.missingKey) {
    return NextResponse.json(kas.parsed, { status: 500 });
  }

  return NextResponse.json(
    {
      ok: kas.ok,
      kasStatus: kas.status,
      kasStatusText: kas.statusText,
      durationMs: Date.now() - started,
      journey: KAS_JOURNEY,
      mode: "sync",
      action: "result",
      correlationId,
      executionId,
      body: kas.parsed
    },
    { status: kas.ok ? 200 : 502 }
  );
}
