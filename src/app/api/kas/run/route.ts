import { NextResponse } from "next/server";
import { KAS_JOURNEY } from "@/lib/kas-ids";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";

/** @deprecated WF-07 — chave KAAS só no Worker .NET. */
export async function POST() {
  return NextResponse.json(
    {
      error: "Integração KAAS saiu do Next.js (WF-07).",
      detail:
        "Worker .NET consome outbox e chama a jornada testes-firmas-e-poderes com X-Flow-Api-Key. Use POST /v1/documents (202) e poll de status.",
      journey: KAS_JOURNEY,
      deprecated: true
    },
    { status: 410 }
  );
}
