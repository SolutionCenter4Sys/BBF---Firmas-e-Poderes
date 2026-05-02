import type { DocStatus } from "@/lib/mocks";

const STAGES = [
  { key: "uploaded", label: "1. Upload" },
  { key: "ocr", label: "2. OCR" },
  { key: "iagen", label: "3. IA Gen" },
  { key: "canonical", label: "4. Canônico" },
  { key: "decision", label: "5. Decisão" }
] as const;

function stageState(stage: typeof STAGES[number]["key"], status: DocStatus): "done" | "active" | "idle" {
  const order: Record<typeof STAGES[number]["key"], number> = { uploaded: 1, ocr: 2, iagen: 3, canonical: 4, decision: 5 };
  const map: Partial<Record<DocStatus, number>> = {
    pendente: 1,
    processando_ocr: 2,
    processando_iagen: 3,
    processando_ner: 3,
    canonico_pronto: 4,
    validacao_oficial: 4,
    decidido: 5,
    revisao_humana: 4,
    falha: 0
  };
  const reached = map[status] ?? 0;
  const target = order[stage];
  if (reached > target) return "done";
  if (reached === target) return "active";
  return "idle";
}

export default function PipelineStatusBar({ status }: { status: DocStatus }) {
  return (
    <div className="pipeline" aria-label={`Status do pipeline: ${status}`}>
      {STAGES.map((s) => (
        <div key={s.key} className={`pipeline-step ${stageState(s.key, status)}`}>{s.label}</div>
      ))}
    </div>
  );
}
