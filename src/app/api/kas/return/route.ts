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
        "action=result corre no Worker, na mesma jornada do ingest. Rotas /api/kas estão deprecadas.",
      journey: KAS_JOURNEY,
      deprecated: true
    },
    { status: 410 }
  );
}
