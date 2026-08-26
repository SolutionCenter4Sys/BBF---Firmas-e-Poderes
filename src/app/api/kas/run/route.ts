import { NextRequest, NextResponse } from "next/server";
import {
  KAS_JOURNEY,
  extractExecutionId,
  newCorrelationId,
  postKasJourney
} from "@/lib/kas-client";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";
export const maxDuration = 300;

function fileToDataUrl(file: File, bytes: Buffer): string {
  const mime = file.type || "application/octet-stream";
  return `data:${mime};base64,${bytes.toString("base64")}`;
}

export async function POST(req: NextRequest) {
  const form = await req.formData();
  const file = form.get("file");
  const fromClient = form.get("correlationId");
  const correlationId =
    typeof fromClient === "string" && fromClient.trim()
      ? fromClient.trim()
      : newCorrelationId();

  if (!(file instanceof File) || file.size === 0) {
    return NextResponse.json({ error: "Nenhum arquivo enviado." }, { status: 400 });
  }

  const bytes = Buffer.from(await file.arrayBuffer());
  const body = {
    document_url: fileToDataUrl(file, bytes),
    mode: "sync",
    payload: {
      action: "ingest",
      correlationId
    }
  };

  const started = Date.now();
  let kas;
  try {
    kas = await postKasJourney(body);
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

  const executionId = extractExecutionId(kas.parsed);

  return NextResponse.json(
    {
      ok: kas.ok,
      kasStatus: kas.status,
      kasStatusText: kas.statusText,
      durationMs: Date.now() - started,
      fileName: file.name,
      fileSize: file.size,
      fileType: file.type,
      journey: KAS_JOURNEY,
      mode: "sync",
      action: "ingest",
      correlationId,
      executionId,
      body: kas.parsed
    },
    { status: kas.ok ? 200 : 502 }
  );
}
