import type { DocStatus, DecisionStatus } from "@/lib/mocks";

const docMap: Record<DocStatus, { label: string; className: string }> = {
  pendente: { label: "Pendente", className: "badge--neutral" },
  processando_ocr: { label: "Processando (OCR)", className: "badge--info" },
  processando_iagen: { label: "Processando (IA Gen)", className: "badge--info" },
  processando_ner: { label: "Processando (NER)", className: "badge--info" },
  canonico_pronto: { label: "Canônico pronto", className: "badge--info" },
  validacao_oficial: { label: "Validação oficial", className: "badge--info" },
  decidido: { label: "Decidido", className: "badge--ok" },
  revisao_humana: { label: "Revisão humana", className: "badge--warn" },
  falha: { label: "Falha", className: "badge--err" }
};

const decisionMap: Record<DecisionStatus, { label: string; className: string }> = {
  APROVADO: { label: "APROVADO", className: "badge--ok" },
  REPROVADO: { label: "REPROVADO", className: "badge--err" },
  MANUAL: { label: "MANUAL", className: "badge--warn" }
};

export function DocStatusBadge({ status }: { status: DocStatus }) {
  const m = docMap[status];
  return <span className={`badge ${m.className}`}>{m.label}</span>;
}

export function DecisionStatusBadge({ status }: { status: DecisionStatus }) {
  const m = decisionMap[status];
  return <span className={`badge ${m.className}`}>{m.label}</span>;
}
