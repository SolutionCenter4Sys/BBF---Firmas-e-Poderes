// Mocks volumosos do MVP — substituídos por chamadas REST reais no Step 12

export type DocStatus =
  | "pendente"
  | "processando_ocr"
  | "processando_iagen"
  | "processando_ner"
  | "canonico_pronto"
  | "validacao_oficial"
  | "decidido"
  | "revisao_humana"
  | "falha";

export type DecisionStatus = "APROVADO" | "REPROVADO" | "MANUAL";

export interface Person {
  personId: string;
  nome: string;
  cpf: string;
  qualificacao: string;
  cargo: string;
  status: "ativo" | "inativo";
}

export interface Power {
  powerId: string;
  pessoa: string;
  operacao: string;
  limite: { currency: "BRL"; value: number; expression: string };
  modoAssinatura: { tipo: "isolada" | "conjunta"; n?: number; m?: number; qualificacoes?: string[] };
  vigencia: { validFrom: string; validTo?: string };
  sourceTrace: { page: number; offsetStart: number; offsetEnd: number; snippet: string };
}

export interface Document {
  documentId: string;
  fileName: string;
  cnpj: string;
  razaoSocial: string;
  tipoSocietario: "LTDA" | "S.A." | "EIRELI";
  uploadedAt: string;
  uploadedBy: string;
  status: DocStatus;
  hash: string;
  paginas: number;
  confianca: { ocr: number; iagen: number; ner: number };
  socios: Person[];
  poderes: Power[];
}

export interface DecisionEvidence {
  type: "documento" | "fonte_oficial";
  trace?: { page: number; offsetStart: number; offsetEnd: number; snippet: string };
  fonte?: string;
  detalhe: string;
}

export interface DecisionRecord {
  decisionId: string;
  documentId: string;
  cnpj: string;
  operacao: string;
  signatariosSolicitados: string[];
  status: DecisionStatus;
  motivos: string[];
  evidencias: DecisionEvidence[];
  versions: { rules: string; canonical: string; aiPrompt: string; aiModel: string };
  evaluatedAt: string;
  latencyMs: number;
}

export interface AuditEvent {
  eventId: string;
  correlationId: string;
  documentId?: string;
  decisionId?: string;
  type: string;
  actor: string;
  timestamp: string;
  details: string;
}

// 12 documentos mockados representando volume realista
export const documents: Document[] = [
  {
    documentId: "doc_001",
    fileName: "contrato-social-acme.pdf",
    cnpj: "12.345.678/0001-90",
    razaoSocial: "ACME Indústrias LTDA",
    tipoSocietario: "LTDA",
    uploadedAt: "2026-04-29T14:22:00Z",
    uploadedBy: "ana.silva@bbf.com.br",
    status: "decidido",
    hash: "a3f2c4...e9d1",
    paginas: 12,
    confianca: { ocr: 0.97, iagen: 0.92, ner: 0.94 },
    socios: [
      { personId: "p1", nome: "João da Silva", cpf: "111.222.***-44", qualificacao: "Sócio-administrador", cargo: "Diretor", status: "ativo" },
      { personId: "p2", nome: "Maria Souza", cpf: "222.333.***-55", qualificacao: "Sócia", cargo: "Procuradora", status: "ativo" },
      { personId: "p3", nome: "Carlos Pereira", cpf: "333.444.***-66", qualificacao: "Sócio", cargo: "Conselheiro", status: "inativo" }
    ],
    poderes: [
      { powerId: "pw1", pessoa: "Diretor", operacao: "Movimentação financeira", limite: { currency: "BRL", value: 500000, expression: "até R$ 500.000,00" }, modoAssinatura: { tipo: "isolada" }, vigencia: { validFrom: "2024-01-01" }, sourceTrace: { page: 4, offsetStart: 1280, offsetEnd: 1480, snippet: "...o Diretor poderá assinar isoladamente movimentações até R$ 500.000,00..." } },
      { powerId: "pw2", pessoa: "Diretor + Procurador", operacao: "Movimentação financeira", limite: { currency: "BRL", value: 5000000, expression: "acima de R$ 500.000,00" }, modoAssinatura: { tipo: "conjunta", n: 2, m: 2, qualificacoes: ["Diretor", "Procurador"] }, vigencia: { validFrom: "2024-01-01" }, sourceTrace: { page: 4, offsetStart: 1500, offsetEnd: 1750, snippet: "...acima desse valor, exigir-se-á assinatura conjunta de Diretor e Procurador..." } }
    ]
  },
  { documentId: "doc_002", fileName: "alteracao-contratual-beta.pdf", cnpj: "98.765.432/0001-11", razaoSocial: "Beta Comercial S.A.", tipoSocietario: "S.A.", uploadedAt: "2026-04-29T11:15:00Z", uploadedBy: "carlos.l@bbf.com.br", status: "validacao_oficial", hash: "b7e9...1a2c", paginas: 8, confianca: { ocr: 0.95, iagen: 0.88, ner: 0.91 }, socios: [], poderes: [] },
  { documentId: "doc_003", fileName: "procuracao-gama.pdf", cnpj: "55.666.777/0001-22", razaoSocial: "Gama Investimentos LTDA", tipoSocietario: "LTDA", uploadedAt: "2026-04-29T09:48:00Z", uploadedBy: "ana.silva@bbf.com.br", status: "revisao_humana", hash: "c1d2...8e9f", paginas: 4, confianca: { ocr: 0.78, iagen: 0.71, ner: 0.66 }, socios: [], poderes: [] },
  { documentId: "doc_004", fileName: "contrato-delta.pdf", cnpj: "11.222.333/0001-44", razaoSocial: "Delta EIRELI", tipoSocietario: "EIRELI", uploadedAt: "2026-04-28T17:30:00Z", uploadedBy: "joao.t@bbf.com.br", status: "decidido", hash: "d4e5...0f1a", paginas: 6, confianca: { ocr: 0.96, iagen: 0.93, ner: 0.95 }, socios: [], poderes: [] },
  { documentId: "doc_005", fileName: "contrato-epsilon.pdf", cnpj: "22.333.444/0001-55", razaoSocial: "Epsilon Tech LTDA", tipoSocietario: "LTDA", uploadedAt: "2026-04-28T16:12:00Z", uploadedBy: "maria.p@bbf.com.br", status: "processando_iagen", hash: "e6f7...2b3c", paginas: 14, confianca: { ocr: 0.94, iagen: 0, ner: 0 }, socios: [], poderes: [] },
  { documentId: "doc_006", fileName: "contrato-zeta.pdf", cnpj: "33.444.555/0001-66", razaoSocial: "Zeta Logística S.A.", tipoSocietario: "S.A.", uploadedAt: "2026-04-28T14:05:00Z", uploadedBy: "carlos.l@bbf.com.br", status: "decidido", hash: "f8a9...4d5e", paginas: 22, confianca: { ocr: 0.99, iagen: 0.96, ner: 0.97 }, socios: [], poderes: [] },
  { documentId: "doc_007", fileName: "contrato-eta.pdf", cnpj: "44.555.666/0001-77", razaoSocial: "Eta Consultoria LTDA", tipoSocietario: "LTDA", uploadedAt: "2026-04-28T11:48:00Z", uploadedBy: "ana.silva@bbf.com.br", status: "falha", hash: "—", paginas: 0, confianca: { ocr: 0, iagen: 0, ner: 0 }, socios: [], poderes: [] },
  { documentId: "doc_008", fileName: "contrato-theta.pdf", cnpj: "55.666.777/0001-88", razaoSocial: "Theta Holdings S.A.", tipoSocietario: "S.A.", uploadedAt: "2026-04-28T10:21:00Z", uploadedBy: "joao.t@bbf.com.br", status: "decidido", hash: "0a1b...6e7f", paginas: 30, confianca: { ocr: 0.98, iagen: 0.95, ner: 0.96 }, socios: [], poderes: [] },
  { documentId: "doc_009", fileName: "contrato-iota.pdf", cnpj: "66.777.888/0001-99", razaoSocial: "Iota EIRELI", tipoSocietario: "EIRELI", uploadedAt: "2026-04-27T19:14:00Z", uploadedBy: "maria.p@bbf.com.br", status: "decidido", hash: "2c3d...8a9b", paginas: 5, confianca: { ocr: 0.97, iagen: 0.94, ner: 0.96 }, socios: [], poderes: [] },
  { documentId: "doc_010", fileName: "contrato-kappa.pdf", cnpj: "77.888.999/0001-00", razaoSocial: "Kappa Comércio LTDA", tipoSocietario: "LTDA", uploadedAt: "2026-04-27T15:32:00Z", uploadedBy: "ana.silva@bbf.com.br", status: "canonico_pronto", hash: "4e5f...0c1d", paginas: 9, confianca: { ocr: 0.93, iagen: 0.89, ner: 0.91 }, socios: [], poderes: [] },
  { documentId: "doc_011", fileName: "contrato-lambda.pdf", cnpj: "88.999.000/0001-11", razaoSocial: "Lambda Indústria S.A.", tipoSocietario: "S.A.", uploadedAt: "2026-04-27T13:48:00Z", uploadedBy: "carlos.l@bbf.com.br", status: "decidido", hash: "6e7f...2d3e", paginas: 18, confianca: { ocr: 0.96, iagen: 0.92, ner: 0.93 }, socios: [], poderes: [] },
  { documentId: "doc_012", fileName: "contrato-mu.pdf", cnpj: "99.000.111/0001-22", razaoSocial: "Mu Serviços LTDA", tipoSocietario: "LTDA", uploadedAt: "2026-04-27T10:05:00Z", uploadedBy: "joao.t@bbf.com.br", status: "decidido", hash: "8a9b...4e5f", paginas: 7, confianca: { ocr: 0.95, iagen: 0.91, ner: 0.93 }, socios: [], poderes: [] }
];

export const decisions: DecisionRecord[] = [
  {
    decisionId: "dec_001",
    documentId: "doc_001",
    cnpj: "12.345.678/0001-90",
    operacao: "Movimentação financeira — R$ 500.000",
    signatariosSolicitados: ["João da Silva (Diretor)", "Maria Souza (Procuradora)"],
    status: "APROVADO",
    motivos: [
      "Operação dentro do limite de R$ 500.000 com modo conjunto Diretor + Procurador (Regra RN02 + RN03 v1.2.0).",
      "Sócios João da Silva e Maria Souza confirmados como ATIVOS na Junta Comercial em 2026-04-29 14:22 UTC.",
      "Procuração vigente (validFrom 2024-01-01, sem revogação)."
    ],
    evidencias: [
      { type: "documento", trace: { page: 4, offsetStart: 1500, offsetEnd: 1750, snippet: "...acima desse valor, exigir-se-á assinatura conjunta de Diretor e Procurador..." }, detalhe: "Cláusula 8 do Contrato Social" },
      { type: "fonte_oficial", fonte: "Junta Comercial SP", detalhe: "Quadro societário consultado em 2026-04-29; ambos os sócios constam como ATIVOS." }
    ],
    versions: { rules: "1.2.0", canonical: "1.0.0", aiPrompt: "leitura-contrato-social@2.1.0", aiModel: "gemini-1.5-pro" },
    evaluatedAt: "2026-04-29T14:25:32Z",
    latencyMs: 1287
  },
  {
    decisionId: "dec_002",
    documentId: "doc_004",
    cnpj: "11.222.333/0001-44",
    operacao: "Contratação de crédito — R$ 200.000",
    signatariosSolicitados: ["Carlos Pereira (Procurador)"],
    status: "REPROVADO",
    motivos: [
      "Sócio Carlos Pereira consta como INATIVO na Junta Comercial desde 2025-09-12 (Regra RN01 v1.2.0).",
      "Procuração apresentada está revogada (validTo 2025-08-30)."
    ],
    evidencias: [
      { type: "fonte_oficial", fonte: "Junta Comercial SP", detalhe: "Status ATIVO=false desde 2025-09-12" },
      { type: "documento", trace: { page: 2, offsetStart: 320, offsetEnd: 480, snippet: "...procuração com validade até 30/08/2025..." }, detalhe: "Cláusula 3 da Procuração" }
    ],
    versions: { rules: "1.2.0", canonical: "1.0.0", aiPrompt: "leitura-contrato-social@2.1.0", aiModel: "gemini-1.5-pro" },
    evaluatedAt: "2026-04-28T17:33:14Z",
    latencyMs: 1542
  },
  {
    decisionId: "dec_003",
    documentId: "doc_003",
    cnpj: "55.666.777/0001-22",
    operacao: "Abertura de conta",
    signatariosSolicitados: ["Pedro Henrique (Procurador)"],
    status: "MANUAL",
    motivos: [
      "Confiança da extração NER abaixo do threshold (66% < 75%) — Regra de Threshold v1.0.0.",
      "Cláusula de poderes ambígua na página 3 — recomenda revisão jurídica."
    ],
    evidencias: [
      { type: "documento", trace: { page: 3, offsetStart: 800, offsetEnd: 1100, snippet: "...os procuradores poderão, em conjunto ou isoladamente conforme deliberação..." }, detalhe: "Cláusula ambígua — modo de assinatura não determinístico" }
    ],
    versions: { rules: "1.2.0", canonical: "1.0.0", aiPrompt: "leitura-contrato-social@2.1.0", aiModel: "gemini-1.5-pro" },
    evaluatedAt: "2026-04-29T09:51:08Z",
    latencyMs: 1102
  }
];

export const audit: AuditEvent[] = [
  { eventId: "ev_0001", correlationId: "corr_a1b2c3", documentId: "doc_001", type: "document.uploaded", actor: "ana.silva@bbf.com.br", timestamp: "2026-04-29T14:22:00Z", details: "Upload de contrato-social-acme.pdf (12 páginas, 2.3MB)" },
  { eventId: "ev_0002", correlationId: "corr_a1b2c3", documentId: "doc_001", type: "ocr.completed", actor: "system", timestamp: "2026-04-29T14:22:14Z", details: "OCR concluído (CER estimado 3.1%)" },
  { eventId: "ev_0003", correlationId: "corr_a1b2c3", documentId: "doc_001", type: "iagen.completed", actor: "system", timestamp: "2026-04-29T14:22:48Z", details: "Leitura semiestruturada (8 seções identificadas)" },
  { eventId: "ev_0004", correlationId: "corr_a1b2c3", documentId: "doc_001", type: "canonical.ready", actor: "system", timestamp: "2026-04-29T14:23:12Z", details: "Modelo canônico montado (3 sócios, 2 poderes)" },
  { eventId: "ev_0005", correlationId: "corr_a1b2c3", documentId: "doc_001", type: "official.queried", actor: "system", timestamp: "2026-04-29T14:24:55Z", details: "Junta Comercial consultada (latência 1.2s, sem divergência crítica)" },
  { eventId: "ev_0006", correlationId: "corr_a1b2c3", documentId: "doc_001", decisionId: "dec_001", type: "decision.evaluated", actor: "system", timestamp: "2026-04-29T14:25:32Z", details: "Decisão APROVADO (latência 1287ms)" },
  { eventId: "ev_0007", correlationId: "corr_a1b2c3", decisionId: "dec_001", type: "audit.persisted", actor: "system", timestamp: "2026-04-29T14:25:32Z", details: "Snapshot WORM persistido para replay" },
  { eventId: "ev_0008", correlationId: "corr_d4e5f6", documentId: "doc_004", decisionId: "dec_002", type: "decision.evaluated", actor: "system", timestamp: "2026-04-28T17:33:14Z", details: "Decisão REPROVADO (sócio inativo)" },
  { eventId: "ev_0009", correlationId: "corr_g7h8i9", documentId: "doc_003", decisionId: "dec_003", type: "decision.evaluated", actor: "system", timestamp: "2026-04-29T09:51:08Z", details: "Decisão MANUAL (baixa confiança)" }
];

export const metrics = {
  decisionsToday: 87,
  approvedRate: 0.72,
  rejectedRate: 0.18,
  manualRate: 0.10,
  p95LatencyMs: 1640,
  uptime30d: 0.9947,
  costPerDecisionBRL: 0.42,
  documentsInQueue: 4
};

export function findDocument(id: string): Document | undefined {
  return documents.find((d) => d.documentId === id);
}

export function findDecisionByDocumentId(documentId: string): DecisionRecord | undefined {
  return decisions.find((d) => d.documentId === documentId);
}

// =====================================================================
// Mocks adicionais para as telas: review queue, manual queue, diff,
// structured view, history, sources health, DPO, data health, RBAC,
// Swagger
// =====================================================================

// --- Fila de Revisão Humana (EP-01-F7) ---
export type ReviewMotivo =
  | "ocr_baixa_confianca"
  | "iagen_baixa_confianca"
  | "ner_baixa_confianca"
  | "clausula_ambigua"
  | "qualidade_documento";

export interface ReviewItem {
  reviewId: string;
  documentId: string;
  cnpj: string;
  razaoSocial: string;
  motivo: ReviewMotivo;
  motivoLegivel: string;
  scoreCritico: number;
  enfileiradoEm: string;
  slaHoras: number;
  prioridade: "alta" | "media" | "baixa";
  responsavel?: string;
}

export const reviewQueue: ReviewItem[] = [
  { reviewId: "rev_001", documentId: "doc_003", cnpj: "55.666.777/0001-22", razaoSocial: "Gama Investimentos LTDA", motivo: "ner_baixa_confianca", motivoLegivel: "Confiança da extração NER abaixo do threshold (66% < 75%)", scoreCritico: 0.66, enfileiradoEm: "2026-04-29T09:48:00Z", slaHoras: 4, prioridade: "alta", responsavel: "ana.silva@bbf.com.br" },
  { reviewId: "rev_002", documentId: "doc_007", cnpj: "44.555.666/0001-77", razaoSocial: "Eta Consultoria LTDA", motivo: "qualidade_documento", motivoLegivel: "Documento PDF protegido / qualidade baixa para OCR", scoreCritico: 0.32, enfileiradoEm: "2026-04-28T11:48:00Z", slaHoras: 8, prioridade: "alta" },
  { reviewId: "rev_003", documentId: "doc_010", cnpj: "77.888.999/0001-00", razaoSocial: "Kappa Comércio LTDA", motivo: "clausula_ambigua", motivoLegivel: "Cláusula 5.2 sobre poderes de procurador interpretada com baixa confiança", scoreCritico: 0.71, enfileiradoEm: "2026-04-27T15:32:00Z", slaHoras: 4, prioridade: "media", responsavel: "joao.t@bbf.com.br" },
  { reviewId: "rev_004", documentId: "doc_002", cnpj: "98.765.432/0001-11", razaoSocial: "Beta Comercial S.A.", motivo: "iagen_baixa_confianca", motivoLegivel: "Score de leitura semiestruturada 88% (threshold 90%)", scoreCritico: 0.88, enfileiradoEm: "2026-04-29T11:15:00Z", slaHoras: 4, prioridade: "media" }
];

// --- Fila de Análise Manual (EP-04-F5) ---
export interface ManualQueueItem {
  manualId: string;
  decisionId?: string;
  documentId: string;
  cnpj: string;
  razaoSocial: string;
  operacao: string;
  motivo: string;
  enfileiradoEm: string;
  slaHoras: number;
  prioridade: "alta" | "media" | "baixa";
  valorOperacao?: number;
  responsavel?: string;
}

export const manualQueue: ManualQueueItem[] = [
  { manualId: "man_001", decisionId: "dec_003", documentId: "doc_003", cnpj: "55.666.777/0001-22", razaoSocial: "Gama Investimentos LTDA", operacao: "Abertura de conta", motivo: "Cláusula de poderes ambígua + baixa confiança NER", enfileiradoEm: "2026-04-29T09:51:08Z", slaHoras: 4, prioridade: "alta", responsavel: "ana.silva@bbf.com.br" },
  { manualId: "man_002", documentId: "doc_005", cnpj: "22.333.444/0001-55", razaoSocial: "Epsilon Tech LTDA", operacao: "Contratação de crédito", motivo: "Validação Junta Comercial indisponível (timeout 3x)", enfileiradoEm: "2026-04-28T16:45:00Z", slaHoras: 8, prioridade: "media", valorOperacao: 850000 },
  { manualId: "man_003", documentId: "doc_010", cnpj: "77.888.999/0001-00", razaoSocial: "Kappa Comércio LTDA", operacao: "Movimentação financeira", motivo: "Combinação de signatários não cobre limite solicitado (regra ambígua)", enfileiradoEm: "2026-04-27T16:00:00Z", slaHoras: 4, prioridade: "alta", valorOperacao: 1200000 },
  { manualId: "man_004", documentId: "doc_002", cnpj: "98.765.432/0001-11", razaoSocial: "Beta Comercial S.A.", operacao: "Emissão de procuração", motivo: "Conflito entre canônico e dados oficiais (sócio com qualificação divergente)", enfileiradoEm: "2026-04-29T11:30:00Z", slaHoras: 8, prioridade: "media" }
];

// --- Diff Documento × Oficial (EP-03-F1+F4) ---
export interface DiffItem {
  campo: string;
  valorDocumento: string;
  valorOficial: string;
  fonte: string;
  severidade: "alta" | "media" | "baixa" | "ok";
  observacao?: string;
}

export interface DocumentDiff {
  documentId: string;
  cnpj: string;
  fonteConsultada: string;
  consultadoEm: string;
  latenciaMs: number;
  cacheHit: boolean;
  itens: DiffItem[];
}

export const diffs: Record<string, DocumentDiff> = {
  doc_001: {
    documentId: "doc_001",
    cnpj: "12.345.678/0001-90",
    fonteConsultada: "Junta Comercial SP",
    consultadoEm: "2026-04-29T14:24:55Z",
    latenciaMs: 1180,
    cacheHit: false,
    itens: [
      { campo: "Razão Social", valorDocumento: "ACME Indústrias LTDA", valorOficial: "ACME Indústrias LTDA", fonte: "JUCESP", severidade: "ok" },
      { campo: "CNPJ", valorDocumento: "12.345.678/0001-90", valorOficial: "12.345.678/0001-90", fonte: "JUCESP", severidade: "ok" },
      { campo: "Endereço sede", valorDocumento: "Av. Paulista, 1000 - SP", valorOficial: "Av. Paulista, 1000 - SP", fonte: "JUCESP", severidade: "ok" },
      { campo: "Capital social", valorDocumento: "R$ 1.000.000,00", valorOficial: "R$ 1.000.000,00", fonte: "JUCESP", severidade: "ok" },
      { campo: "Sócio: João da Silva", valorDocumento: "Sócio-administrador, Ativo", valorOficial: "Sócio-administrador, Ativo", fonte: "JUCESP", severidade: "ok" },
      { campo: "Sócio: Maria Souza", valorDocumento: "Sócia, Procuradora", valorOficial: "Sócia, Procuradora", fonte: "JUCESP", severidade: "ok" },
      { campo: "Sócio: Carlos Pereira", valorDocumento: "Sócio (sem informação adicional)", valorOficial: "Sócio INATIVO desde 2025-09-12", fonte: "JUCESP", severidade: "media", observacao: "Documento não menciona inatividade — pode ser desatualizado" }
    ]
  },
  doc_004: {
    documentId: "doc_004",
    cnpj: "11.222.333/0001-44",
    fonteConsultada: "Junta Comercial SP",
    consultadoEm: "2026-04-28T17:31:00Z",
    latenciaMs: 1310,
    cacheHit: false,
    itens: [
      { campo: "Razão Social", valorDocumento: "Delta EIRELI", valorOficial: "Delta EIRELI", fonte: "JUCESP", severidade: "ok" },
      { campo: "CNPJ", valorDocumento: "11.222.333/0001-44", valorOficial: "11.222.333/0001-44", fonte: "JUCESP", severidade: "ok" },
      { campo: "Capital social", valorDocumento: "R$ 100.000,00", valorOficial: "R$ 250.000,00", fonte: "JUCESP", severidade: "media", observacao: "Houve aumento de capital em 2025-12 não refletido no documento apresentado" },
      { campo: "Titular: Carlos Pereira", valorDocumento: "Titular, Ativo", valorOficial: "Titular INATIVO desde 2025-09-12 (substituído por procuração revogada)", fonte: "JUCESP", severidade: "alta", observacao: "Sócio inativo na fonte oficial — bloqueia operação (RN01)" },
      { campo: "Procuração — vigência", valorDocumento: "01/01/2024 a 30/08/2025", valorOficial: "Procuração revogada em 30/08/2025", fonte: "JUCESP", severidade: "alta", observacao: "Procuração apresentada está revogada" }
    ]
  }
};

// --- Visão semiestruturada (EP-01-F3) ---
export interface DocumentSection {
  titulo: string;
  paginaInicio: number;
  paginaFim: number;
  confianca: number;
  resumo: string;
}

export const documentSections: Record<string, DocumentSection[]> = {
  doc_001: [
    { titulo: "Qualificação das partes", paginaInicio: 1, paginaFim: 2, confianca: 0.97, resumo: "Identificação dos sócios João da Silva, Maria Souza e Carlos Pereira (CPF, qualificação, residência)." },
    { titulo: "Objeto social e capital", paginaInicio: 2, paginaFim: 3, confianca: 0.95, resumo: "Capital social R$ 1.000.000 dividido em 10.000 quotas; objeto: indústria e comércio de eletrônicos." },
    { titulo: "Administração e poderes", paginaInicio: 3, paginaFim: 4, confianca: 0.93, resumo: "Define quem administra e os limites de assinatura (cláusulas 7 e 8)." },
    { titulo: "Limites de movimentação financeira", paginaInicio: 4, paginaFim: 5, confianca: 0.94, resumo: "Diretor isolado até R$ 500.000; acima disso, conjunta Diretor + Procurador." },
    { titulo: "Procurações conferidas", paginaInicio: 5, paginaFim: 6, confianca: 0.91, resumo: "Procuração para Maria Souza com vigência aberta a partir de 2024-01-01." },
    { titulo: "Vigência e alterações", paginaInicio: 7, paginaFim: 8, confianca: 0.88, resumo: "Cláusula de duração indeterminada; última alteração em 2024-03-15." },
    { titulo: "Foro e disposições gerais", paginaInicio: 9, paginaFim: 10, confianca: 0.99, resumo: "Foro de São Paulo; disposições gerais e assinaturas das partes." },
    { titulo: "Encerramento", paginaInicio: 11, paginaFim: 12, confianca: 0.99, resumo: "Local, data, testemunhas e reconhecimento de firma em cartório." }
  ],
  doc_004: [
    { titulo: "Qualificação do titular", paginaInicio: 1, paginaFim: 1, confianca: 0.96, resumo: "Carlos Pereira como titular único da EIRELI." },
    { titulo: "Capital e objeto", paginaInicio: 2, paginaFim: 3, confianca: 0.93, resumo: "Capital R$ 100.000; objeto: prestação de serviços de consultoria." },
    { titulo: "Administração", paginaInicio: 3, paginaFim: 4, confianca: 0.94, resumo: "Titular administra com poderes plenos." },
    { titulo: "Procuração", paginaInicio: 4, paginaFim: 5, confianca: 0.85, resumo: "Procuração com validade até 30/08/2025." },
    { titulo: "Encerramento", paginaInicio: 5, paginaFim: 6, confianca: 0.97, resumo: "Assinaturas e reconhecimento." }
  ]
};

// --- Histórico de versões (EP-01-F5) ---
export interface DocumentVersion {
  versionId: string;
  tipo: "raw" | "ocr" | "iagen" | "ner" | "canonical";
  criadoEm: string;
  tamanhoBytes: number;
  hash: string;
  notas: string;
}

export const documentHistory: Record<string, DocumentVersion[]> = {
  doc_001: [
    { versionId: "v0", tipo: "raw", criadoEm: "2026-04-29T14:22:00Z", tamanhoBytes: 2_350_000, hash: "a3f2c4e1...e9d1", notas: "Upload original (PDF)" },
    { versionId: "v1", tipo: "ocr", criadoEm: "2026-04-29T14:22:14Z", tamanhoBytes: 480_000, hash: "ocr_8a9b...", notas: "OCR concluído (CER 3.1%)" },
    { versionId: "v2", tipo: "iagen", criadoEm: "2026-04-29T14:22:48Z", tamanhoBytes: 312_000, hash: "ia_b1c2...", notas: "Texto semiestruturado (8 seções, prompt v2.1.0)" },
    { versionId: "v3", tipo: "ner", criadoEm: "2026-04-29T14:23:01Z", tamanhoBytes: 78_000, hash: "ner_c3d4...", notas: "Entidades extraídas (modelo v1.2.0, score médio 94%)" },
    { versionId: "v4", tipo: "canonical", criadoEm: "2026-04-29T14:23:12Z", tamanhoBytes: 24_000, hash: "can_d5e6...", notas: "Canônico v1.0.0 (3 sócios, 2 poderes)" }
  ],
  doc_004: [
    { versionId: "v0", tipo: "raw", criadoEm: "2026-04-28T17:30:00Z", tamanhoBytes: 1_120_000, hash: "d4e5f6a1...0f1a", notas: "Upload original" },
    { versionId: "v1", tipo: "ocr", criadoEm: "2026-04-28T17:30:09Z", tamanhoBytes: 220_000, hash: "ocr_e1f2...", notas: "OCR concluído (CER 4.2%)" },
    { versionId: "v2", tipo: "iagen", criadoEm: "2026-04-28T17:30:31Z", tamanhoBytes: 145_000, hash: "ia_f3a4...", notas: "Texto semiestruturado (5 seções)" },
    { versionId: "v3", tipo: "ner", criadoEm: "2026-04-28T17:30:42Z", tamanhoBytes: 41_000, hash: "ner_a5b6...", notas: "Entidades extraídas" },
    { versionId: "v4", tipo: "canonical", criadoEm: "2026-04-28T17:30:55Z", tamanhoBytes: 18_000, hash: "can_b7c8...", notas: "Canônico montado" }
  ]
};

// --- Saúde das fontes externas (EP-03-F7) ---
export interface SourceHealth {
  sourceId: string;
  nome: string;
  status: "operacional" | "degradado" | "indisponivel";
  uptime24h: number;
  latenciaP95Ms: number;
  errorRate: number;
  cacheHitRate: number;
  ultimaConsulta: string;
  circuitBreaker: "fechado" | "meio-aberto" | "aberto";
  observacao?: string;
}

export const sourceHealth: SourceHealth[] = [
  { sourceId: "junta-sp", nome: "Junta Comercial SP", status: "operacional", uptime24h: 0.998, latenciaP95Ms: 1240, errorRate: 0.002, cacheHitRate: 0.42, ultimaConsulta: "2026-04-29T14:24:55Z", circuitBreaker: "fechado" },
  { sourceId: "junta-rj", nome: "Junta Comercial RJ", status: "degradado", uptime24h: 0.962, latenciaP95Ms: 3850, errorRate: 0.043, cacheHitRate: 0.28, ultimaConsulta: "2026-04-29T13:48:12Z", circuitBreaker: "meio-aberto", observacao: "Latência elevada nas últimas 2h — possível instabilidade do provedor" },
  { sourceId: "receita-federal", nome: "Receita Federal — Situação Cadastral", status: "operacional", uptime24h: 0.991, latenciaP95Ms: 880, errorRate: 0.009, cacheHitRate: 0.61, ultimaConsulta: "2026-04-29T14:10:00Z", circuitBreaker: "fechado", observacao: "Conector ativo (Fase 2 antecipada)" },
  { sourceId: "compliance-pep", nome: "Listas Compliance (PEP / Sanções)", status: "indisponivel", uptime24h: 0.821, latenciaP95Ms: 0, errorRate: 1.0, cacheHitRate: 0.0, ultimaConsulta: "2026-04-29T07:12:33Z", circuitBreaker: "aberto", observacao: "Provedor em manutenção desde 07:00 — fallback para AnáliseManual ativo" }
];

// --- Console DPO (EP-06-F5) ---
export type DpoRequestType = "acesso" | "retificacao" | "eliminacao" | "portabilidade" | "informacao";
export type DpoRequestStatus = "novo" | "em_atendimento" | "concluido" | "negado";

export interface DpoRequest {
  requestId: string;
  titularNome: string;
  titularDocumento: string;
  tipo: DpoRequestType;
  status: DpoRequestStatus;
  receivedAt: string;
  prazoLegalDias: number;
  diasRestantes: number;
  responsavel?: string;
  descricao: string;
}

export const dpoRequests: DpoRequest[] = [
  { requestId: "lgpd_001", titularNome: "João da Silva", titularDocumento: "***.222.***-44", tipo: "acesso", status: "em_atendimento", receivedAt: "2026-04-26T10:00:00Z", prazoLegalDias: 15, diasRestantes: 11, responsavel: "dpo@bbf.com.br", descricao: "Solicita relação completa de processamentos do CPF nos últimos 12 meses." },
  { requestId: "lgpd_002", titularNome: "Maria Souza", titularDocumento: "***.333.***-55", tipo: "retificacao", status: "novo", receivedAt: "2026-04-29T09:30:00Z", prazoLegalDias: 15, diasRestantes: 14, descricao: "Solicita correção de qualificação extraída como 'sócia' para 'sócia-administradora'." },
  { requestId: "lgpd_003", titularNome: "Pedro Henrique", titularDocumento: "***.444.***-66", tipo: "eliminacao", status: "concluido", receivedAt: "2026-04-10T14:00:00Z", prazoLegalDias: 15, diasRestantes: 0, responsavel: "dpo@bbf.com.br", descricao: "Eliminação de dados após encerramento do vínculo. Base legal: cumprimento de obrigação legal expirada." },
  { requestId: "lgpd_004", titularNome: "Carlos Pereira", titularDocumento: "***.555.***-77", tipo: "portabilidade", status: "em_atendimento", receivedAt: "2026-04-25T16:20:00Z", prazoLegalDias: 15, diasRestantes: 10, responsavel: "dpo@bbf.com.br", descricao: "Solicita portabilidade dos dados em formato estruturado (JSON)." },
  { requestId: "lgpd_005", titularNome: "Ana Carolina Lima", titularDocumento: "***.666.***-88", tipo: "informacao", status: "negado", receivedAt: "2026-04-22T11:15:00Z", prazoLegalDias: 15, diasRestantes: 0, responsavel: "dpo@bbf.com.br", descricao: "Solicita informações de processamento. Negado: titular não localizado em nossas bases (justificativa registrada)." }
];

// --- Saúde de Dados / PII scans (EP-06-F2) ---
export interface PiiScanResult {
  scanId: string;
  executadoEm: string;
  servico: string;
  amostraLogs: number;
  ocorrenciasDetectadas: number;
  ocorrenciasMascaradas: number;
  cobertura: number;
  status: "ok" | "alerta" | "critico";
}

export const piiScans: PiiScanResult[] = [
  { scanId: "scan_w17", executadoEm: "2026-04-28T22:00:00Z", servico: "orchestration-api", amostraLogs: 50000, ocorrenciasDetectadas: 1240, ocorrenciasMascaradas: 1240, cobertura: 1.0, status: "ok" },
  { scanId: "scan_w17", executadoEm: "2026-04-28T22:00:00Z", servico: "ai-ocr-service", amostraLogs: 30000, ocorrenciasDetectadas: 890, ocorrenciasMascaradas: 887, cobertura: 0.997, status: "alerta" },
  { scanId: "scan_w17", executadoEm: "2026-04-28T22:00:00Z", servico: "decision-engine", amostraLogs: 22000, ocorrenciasDetectadas: 411, ocorrenciasMascaradas: 411, cobertura: 1.0, status: "ok" },
  { scanId: "scan_w17", executadoEm: "2026-04-28T22:00:00Z", servico: "verification-service", amostraLogs: 18000, ocorrenciasDetectadas: 320, ocorrenciasMascaradas: 320, cobertura: 1.0, status: "ok" },
  { scanId: "scan_w17", executadoEm: "2026-04-28T22:00:00Z", servico: "audit-service", amostraLogs: 15000, ocorrenciasDetectadas: 612, ocorrenciasMascaradas: 612, cobertura: 1.0, status: "ok" }
];

export const piiTendencia30d = [0.998, 0.999, 1.0, 0.997, 1.0, 1.0, 0.999, 1.0, 0.996, 1.0, 1.0, 1.0, 0.999, 1.0, 0.998, 1.0, 0.997, 1.0, 1.0, 0.999, 1.0, 1.0, 0.998, 1.0, 0.999, 1.0, 0.997, 1.0, 0.999, 0.997];

// --- Admin de perfis RBAC (EP-06-F4) ---
export interface RoleDefinition {
  roleId: string;
  nome: string;
  descricao: string;
  usuariosAtivos: number;
  permissoes: string[];
}

export const roles: RoleDefinition[] = [
  { roleId: "analista", nome: "Analista Jurídico", descricao: "Faz upload, revisa documentos e aprova decisões manuais", usuariosAtivos: 24, permissoes: ["documents:read", "documents:upload", "decision:read", "review:write", "manual:write"] },
  { roleId: "auditor", nome: "Auditor Interno", descricao: "Acesso somente-leitura à trilha de auditoria e replay", usuariosAtivos: 6, permissoes: ["audit:read", "decision:replay", "documents:read"] },
  { roleId: "dpo", nome: "DPO (Encarregado LGPD)", descricao: "Atende requisições de titulares e gerencia retenção", usuariosAtivos: 2, permissoes: ["lgpd:write", "retention:write", "audit:read", "data-health:read"] },
  { roleId: "curador-regras", nome: "Curador de Regras", descricao: "Propõe e gerencia regras de decisão (DMN)", usuariosAtivos: 3, permissoes: ["rules:read", "rules:propose", "rules:simulate"] },
  { roleId: "aprovador-regras", nome: "Aprovador de Regras", descricao: "Aprova promoção de regras (dupla aprovação com Jurídico)", usuariosAtivos: 4, permissoes: ["rules:approve"] },
  { roleId: "admin", nome: "Administrador da Plataforma", descricao: "Gerencia usuários, perfis e configurações", usuariosAtivos: 2, permissoes: ["roles:write", "users:write", "config:write", "*:read"] },
  { roleId: "consumer", nome: "Consumer (Jornada externa)", descricao: "Sistemas consumidores que chamam a API de decisão", usuariosAtivos: 7, permissoes: ["api:decision:read"] }
];

export interface UserMock {
  email: string;
  nome: string;
  roles: string[];
  ultimoAcesso: string;
  status: "ativo" | "inativo";
}

export const users: UserMock[] = [
  { email: "ana.silva@bbf.com.br", nome: "Ana Silva", roles: ["analista"], ultimoAcesso: "2026-04-29T14:22:00Z", status: "ativo" },
  { email: "carlos.l@bbf.com.br", nome: "Carlos Lima", roles: ["analista"], ultimoAcesso: "2026-04-29T11:15:00Z", status: "ativo" },
  { email: "joao.t@bbf.com.br", nome: "João Teixeira", roles: ["analista", "curador-regras"], ultimoAcesso: "2026-04-29T10:00:00Z", status: "ativo" },
  { email: "maria.p@bbf.com.br", nome: "Maria Pereira", roles: ["analista"], ultimoAcesso: "2026-04-28T17:30:00Z", status: "ativo" },
  { email: "auditor.interno@bbf.com.br", nome: "Roberto Auditor", roles: ["auditor"], ultimoAcesso: "2026-04-28T09:00:00Z", status: "ativo" },
  { email: "dpo@bbf.com.br", nome: "Beatriz DPO", roles: ["dpo"], ultimoAcesso: "2026-04-29T08:30:00Z", status: "ativo" },
  { email: "juridico.aprovador@bbf.com.br", nome: "Sérgio Jurídico", roles: ["aprovador-regras"], ultimoAcesso: "2026-04-25T16:00:00Z", status: "ativo" }
];

// --- Swagger / OpenAPI (EP-05-F5) ---
export interface OpenApiEndpoint {
  method: "GET" | "POST" | "PUT" | "DELETE";
  path: string;
  summary: string;
  description: string;
  tag: string;
  params?: { name: string; in: "query" | "path" | "header" | "body"; type: string; required: boolean; description: string }[];
  responses: { code: number; description: string; example?: unknown }[];
}

export const openApiEndpoints: OpenApiEndpoint[] = [
  {
    method: "GET",
    path: "/v1/authority/decision",
    summary: "Avaliar autoridade de assinatura",
    description: "Retorna a decisão (APROVADO / REPROVADO / MANUAL) para uma operação solicitada por uma jornada consumidora, com motivos e evidências.",
    tag: "Decisão",
    params: [
      { name: "cnpj", in: "query", type: "string", required: true, description: "CNPJ no formato 00.000.000/0000-00" },
      { name: "operation", in: "query", type: "string", required: true, description: "Identificador da operação (ex.: movimentacao_financeira)" },
      { name: "signers", in: "query", type: "string", required: true, description: "Lista de IDs de signatários separados por vírgula" },
      { name: "X-Correlation-Id", in: "header", type: "string", required: false, description: "Identificador de correlação para rastreabilidade" },
      { name: "Idempotency-Key", in: "header", type: "string", required: false, description: "Chave de idempotência (24h de janela)" }
    ],
    responses: [
      { code: 200, description: "Decisão avaliada com sucesso", example: { status: "APROVADO", motivos: ["..."], evidencias: ["..."], decisionId: "dec_001" } },
      { code: 400, description: "Parâmetros inválidos" },
      { code: 401, description: "Token OAuth2 inválido ou ausente" },
      { code: 403, description: "Escopo insuficiente" },
      { code: 429, description: "Rate limit excedido" },
      { code: 503, description: "Fonte oficial indisponível (decisão MANUAL pode ser retornada como fallback)" }
    ]
  },
  {
    method: "POST",
    path: "/v1/documents",
    summary: "Upload de documento societário",
    description: "Recebe upload multipart/form-data de contrato social, alteração contratual ou procuração. Inicia pipeline assíncrono.",
    tag: "Documentos",
    params: [
      { name: "file", in: "body", type: "file (multipart)", required: true, description: "Arquivo PDF ou imagem (até 50 MB)" },
      { name: "X-Correlation-Id", in: "header", type: "string", required: false, description: "Identificador de correlação" }
    ],
    responses: [
      { code: 202, description: "Aceito para processamento", example: { documentId: "doc_xxx", status: "pendente", uploadedAt: "2026-04-30T..." } },
      { code: 415, description: "Tipo de arquivo não suportado" },
      { code: 422, description: "Arquivo inválido (antivírus, tamanho, páginas)" }
    ]
  },
  {
    method: "GET",
    path: "/v1/documents/{documentId}/status",
    summary: "Consultar status do pipeline",
    tag: "Documentos",
    description: "Retorna o estado atual do documento no pipeline (pendente, processando_*, canonico_pronto, decidido, falha).",
    params: [{ name: "documentId", in: "path", type: "string", required: true, description: "ID do documento" }],
    responses: [{ code: 200, description: "Status retornado" }, { code: 404, description: "Documento não encontrado" }]
  },
  {
    method: "POST",
    path: "/v1/decision/evaluate",
    summary: "Avaliar decisão (uso interno)",
    tag: "Decisão",
    description: "Endpoint detalhado para uso da UI interna — recebe contexto completo e retorna decisão, motivos, evidências e snapshot de versões.",
    params: [{ name: "body", in: "body", type: "EvaluationRequest", required: true, description: "Contexto completo da operação" }],
    responses: [{ code: 200, description: "Decisão avaliada" }]
  },
  {
    method: "POST",
    path: "/v1/decision/{decisionId}/replay",
    summary: "Replay determinístico de decisão",
    tag: "Decisão",
    description: "Reexecuta a decisão usando o snapshot original (versões de regras, canônico, dados oficiais). Resposta deve ser idêntica à original.",
    params: [{ name: "decisionId", in: "path", type: "string", required: true, description: "ID da decisão" }],
    responses: [{ code: 200, description: "Replay executado" }, { code: 404, description: "Decisão não encontrada" }]
  },
  {
    method: "GET",
    path: "/v1/audit/trail",
    summary: "Trilha de auditoria",
    tag: "Auditoria",
    description: "Lista eventos da trilha imutável agrupados por correlationId. Restrito ao perfil 'auditor'.",
    params: [
      { name: "documentId", in: "query", type: "string", required: false, description: "Filtra por documento" },
      { name: "correlationId", in: "query", type: "string", required: false, description: "Filtra por correlation" },
      { name: "from", in: "query", type: "ISO8601", required: false, description: "Data inicial" },
      { name: "to", in: "query", type: "ISO8601", required: false, description: "Data final" }
    ],
    responses: [{ code: 200, description: "Trilha retornada" }, { code: 403, description: "Sem permissão" }]
  },
  {
    method: "GET",
    path: "/v1/verification/health",
    summary: "Saúde das fontes oficiais",
    tag: "Validação Oficial",
    description: "Status, latência, error-rate e estado do circuit breaker de cada fonte oficial conectada.",
    responses: [{ code: 200, description: "Saúde das fontes" }]
  }
];

// --- Métricas adicionais ---
export const slo = {
  p95LatencyMs: { current: 1640, target: 2000, status: "ok" },
  uptime30d: { current: 0.9947, target: 0.99, status: "ok" },
  errorBudgetConsumed: 0.31,
  errorBudgetRemainingDays: 17
};

export const dashboardsByStage = [
  { stage: "1 — Leitura", p95Ms: 720, throughputPerMin: 84, errorRate: 0.012 },
  { stage: "2 — Estruturação", p95Ms: 380, throughputPerMin: 81, errorRate: 0.004 },
  { stage: "3 — Validação Oficial", p95Ms: 1180, throughputPerMin: 79, errorRate: 0.018 },
  { stage: "4 — Decisão", p95Ms: 290, throughputPerMin: 77, errorRate: 0.002 },
  { stage: "5 — API", p95Ms: 95, throughputPerMin: 142, errorRate: 0.001 }
];

// --- Canonical schema preview (EP-02-F1) ---
export const canonicalSchemaPreview = {
  $schema: "https://json-schema.org/draft/2020-12/schema",
  $id: "https://api.bbf.bradesco.com.br/schemas/canonical-powers/v1",
  title: "BBF Firmas e Poderes — Modelo Canônico v1.0.0",
  type: "object",
  required: ["documentId", "cnpj", "pessoas", "poderes"],
  properties: {
    documentId: { type: "string", description: "ID único do documento de origem" },
    cnpj: { type: "string", pattern: "^\\d{2}\\.\\d{3}\\.\\d{3}/\\d{4}-\\d{2}$" },
    pessoas: { type: "array", items: { $ref: "#/definitions/Pessoa" } },
    poderes: { type: "array", items: { $ref: "#/definitions/Poder" } }
  }
};

// --- Catálogo de Operações (EP-02-F3) ---
export interface Operation {
  code: string;
  displayName: string;
  description: string;
  category: "movimentacao" | "credito" | "garantia" | "cambio" | "societaria";
  version: string;
  owner: string;
  status: "ativa" | "rascunho" | "depreciada";
  createdAt: string;
  lastModified: string;
}

export const operationsCatalog: Operation[] = [
  { code: "movimentacao_financeira", displayName: "Movimentação Financeira", description: "TED, DOC, transferências, pagamentos com débito em conta", category: "movimentacao", version: "1.2.0", owner: "ana.silva@bbf.com.br", status: "ativa", createdAt: "2024-01-15T10:00:00Z", lastModified: "2025-08-22T14:30:00Z" },
  { code: "contratacao_credito", displayName: "Contratação de Crédito", description: "Crédito rotativo, capital de giro e financiamentos PJ", category: "credito", version: "1.1.0", owner: "joao.t@bbf.com.br", status: "ativa", createdAt: "2024-02-10T09:00:00Z", lastModified: "2025-09-10T16:00:00Z" },
  { code: "abertura_conta", displayName: "Abertura de Conta", description: "Abertura de conta corrente PJ", category: "societaria", version: "1.0.0", owner: "maria.p@bbf.com.br", status: "ativa", createdAt: "2024-01-20T11:30:00Z", lastModified: "2024-12-01T08:45:00Z" },
  { code: "emissao_procuracao", displayName: "Emissão de Procuração", description: "Outorga de poderes em procuração societária", category: "societaria", version: "1.0.1", owner: "ana.silva@bbf.com.br", status: "ativa", createdAt: "2024-02-25T14:15:00Z", lastModified: "2025-04-18T10:20:00Z" },
  { code: "constituicao_garantia", displayName: "Constituição de Garantia", description: "Penhor, alienação fiduciária, hipoteca PJ", category: "garantia", version: "1.0.0", owner: "carlos.l@bbf.com.br", status: "ativa", createdAt: "2024-03-05T08:30:00Z", lastModified: "2024-10-15T13:00:00Z" },
  { code: "operacao_cambio", displayName: "Operação de Câmbio", description: "Compra/venda de moeda estrangeira para empresas", category: "cambio", version: "1.0.0", owner: "carlos.l@bbf.com.br", status: "ativa", createdAt: "2024-04-01T09:45:00Z", lastModified: "2024-11-20T15:30:00Z" },
  { code: "alteracao_quadro_societario", displayName: "Alteração de Quadro Societário", description: "Ingresso/saída de sócios, alteração de quotas", category: "societaria", version: "0.9.0", owner: "joao.t@bbf.com.br", status: "rascunho", createdAt: "2026-04-10T16:00:00Z", lastModified: "2026-04-25T11:30:00Z" },
  { code: "emissao_garantia_internacional", displayName: "Emissão de Garantia Internacional", description: "Carta de fiança internacional, standby letter of credit", category: "garantia", version: "0.5.0", owner: "carlos.l@bbf.com.br", status: "rascunho", createdAt: "2026-04-15T13:20:00Z", lastModified: "2026-04-28T09:00:00Z" },
  { code: "redesconto_titulos", displayName: "Redesconto de Títulos", description: "Operação descontinuada — sem novos casos desde 2025", category: "credito", version: "0.8.0", owner: "carlos.l@bbf.com.br", status: "depreciada", createdAt: "2024-06-10T10:00:00Z", lastModified: "2025-12-30T17:00:00Z" }
];

// --- Catálogo de Regras DMN (EP-04-F2) ---
export type RuleStatus = "ativa" | "proposta" | "em_revisao" | "depreciada";

export interface DmnRule {
  ruleId: string;
  name: string;
  description: string;
  origemRegulatoria: string;
  version: string;
  status: RuleStatus;
  owner: string;
  approvers: string[];
  expressionSummary: string;
  appliesTo: string[];
  lastModified: string;
  promotedAt?: string;
  decisoesAfetadas30d: number;
}

export const dmnRules: DmnRule[] = [
  { ruleId: "RN01", name: "Sócio deve estar ATIVO na fonte oficial", description: "Verifica se todos os signatários informados constam como ATIVOS na Junta Comercial", origemRegulatoria: "BACEN/Compliance interno", version: "1.2.0", status: "ativa", owner: "ana.silva@bbf.com.br", approvers: ["juridico.aprovador@bbf.com.br", "compliance@bbf.com.br"], expressionSummary: "for each signer in signers: assert official.status == ATIVO", appliesTo: ["movimentacao_financeira", "contratacao_credito", "abertura_conta", "constituicao_garantia"], lastModified: "2025-08-22T14:30:00Z", promotedAt: "2025-08-25T10:00:00Z", decisoesAfetadas30d: 1247 },
  { ruleId: "RN02", name: "Modo de assinatura deve cobrir a operação", description: "Combinação de signatários informados deve atender ao modo (isolada/conjunta) definido no canônico para a operação solicitada", origemRegulatoria: "Cláusula contratual + RN BACEN", version: "1.2.0", status: "ativa", owner: "ana.silva@bbf.com.br", approvers: ["juridico.aprovador@bbf.com.br"], expressionSummary: "evaluate(canonical.signatureMode, signers, operation) == VALID", appliesTo: ["movimentacao_financeira", "contratacao_credito", "constituicao_garantia"], lastModified: "2025-08-22T14:30:00Z", promotedAt: "2025-08-25T10:00:00Z", decisoesAfetadas30d: 1247 },
  { ruleId: "RN03", name: "Valor da operação deve respeitar o limite", description: "Valor solicitado não pode ultrapassar o limite definido no canônico para o modo de assinatura aplicado", origemRegulatoria: "Cláusula contratual", version: "1.2.0", status: "ativa", owner: "joao.t@bbf.com.br", approvers: ["juridico.aprovador@bbf.com.br"], expressionSummary: "operation.value <= canonical.power.limit (avaliando expression: BRL absoluto, % capital social, etc.)", appliesTo: ["movimentacao_financeira", "contratacao_credito", "constituicao_garantia", "operacao_cambio"], lastModified: "2025-08-22T14:30:00Z", promotedAt: "2025-08-25T10:00:00Z", decisoesAfetadas30d: 1158 },
  { ruleId: "RN04", name: "Procuração deve estar vigente", description: "Procurações apresentadas devem estar dentro de validFrom/validTo e não revogadas", origemRegulatoria: "Cláusula + Código Civil", version: "1.2.0", status: "ativa", owner: "ana.silva@bbf.com.br", approvers: ["juridico.aprovador@bbf.com.br"], expressionSummary: "for each procurador in signers: isValid(procuracao, asOf=now)", appliesTo: ["movimentacao_financeira", "contratacao_credito", "emissao_procuracao", "abertura_conta"], lastModified: "2025-08-22T14:30:00Z", promotedAt: "2025-08-25T10:00:00Z", decisoesAfetadas30d: 689 },
  { ruleId: "TH01", name: "Threshold de confiança da extração", description: "Encaminha para AnáliseManual se confiança de qualquer estágio (OCR/IAGen/NER) estiver abaixo do threshold configurado", origemRegulatoria: "Política interna de qualidade", version: "1.0.0", status: "ativa", owner: "joao.t@bbf.com.br", approvers: ["compliance@bbf.com.br"], expressionSummary: "min(scores.ocr, scores.iagen, scores.ner) < threshold[env] => MANUAL", appliesTo: ["*"], lastModified: "2025-09-30T16:00:00Z", promotedAt: "2025-10-02T09:00:00Z", decisoesAfetadas30d: 187 },
  { ruleId: "RN05", name: "Bloqueio para PEP em listas restritivas", description: "Bloqueia operações se algum sócio/procurador estiver em lista PEP, sanções ou restritivas", origemRegulatoria: "BACEN PLD/FT + Compliance", version: "0.8.0", status: "em_revisao", owner: "carlos.l@bbf.com.br", approvers: ["juridico.aprovador@bbf.com.br", "compliance@bbf.com.br"], expressionSummary: "compliance.flags includes ('PEP' or 'SANCTION' or 'RESTRICTIVE') => REPROVADO", appliesTo: ["*"], lastModified: "2026-04-22T11:00:00Z", decisoesAfetadas30d: 0 },
  { ruleId: "RN06", name: "Combinação preferencial Diretor + Procurador", description: "Para movimentações acima de R$ 1MM, exigir combinação Diretor + Procurador (regra preferencial, complementar à RN02)", origemRegulatoria: "Política de gestão de risco interno", version: "0.3.0", status: "proposta", owner: "joao.t@bbf.com.br", approvers: [], expressionSummary: "if operation.value > 1_000_000 then signature.combination must include {Diretor, Procurador}", appliesTo: ["movimentacao_financeira"], lastModified: "2026-04-28T15:30:00Z", decisoesAfetadas30d: 0 }
];

// --- Consumidores da API (EP-05-F2 + F7) ---
export type ConsumerStatus = "ativo" | "suspenso" | "em_homologacao";

export interface ApiConsumer {
  consumerId: string;
  name: string;
  description: string;
  owner: string;
  scopes: string[];
  rateLimitPerMin: number;
  status: ConsumerStatus;
  requestsToday: number;
  requestsMonth: number;
  errorRate: number;
  p95LatencyMs: number;
  successRate: number;
  lastCall: string;
  homologadoEm?: string;
}

export const apiConsumers: ApiConsumer[] = [
  { consumerId: "onboarding-pj", name: "Onboarding PJ", description: "Jornada principal de abertura de conta para pessoas jurídicas", owner: "squad-onboarding@bbf.com.br", scopes: ["api:decision:read", "api:documents:upload"], rateLimitPerMin: 200, status: "ativo", requestsToday: 1487, requestsMonth: 38_421, errorRate: 0.003, p95LatencyMs: 1480, successRate: 0.997, lastCall: "2026-04-29T14:25:32Z", homologadoEm: "2026-02-01T10:00:00Z" },
  { consumerId: "credito-pj", name: "Crédito PJ", description: "Plataforma de concessão de crédito para empresas", owner: "squad-credito@bbf.com.br", scopes: ["api:decision:read"], rateLimitPerMin: 100, status: "ativo", requestsToday: 642, requestsMonth: 18_312, errorRate: 0.008, p95LatencyMs: 1620, successRate: 0.992, lastCall: "2026-04-29T14:18:11Z", homologadoEm: "2026-03-15T11:30:00Z" },
  { consumerId: "garantias-pj", name: "Garantias PJ", description: "Constituição e gestão de garantias (penhor, alienação)", owner: "squad-garantias@bbf.com.br", scopes: ["api:decision:read"], rateLimitPerMin: 60, status: "ativo", requestsToday: 184, requestsMonth: 5_421, errorRate: 0.011, p95LatencyMs: 1740, successRate: 0.989, lastCall: "2026-04-29T13:55:44Z", homologadoEm: "2026-04-01T09:00:00Z" },
  { consumerId: "cambio-pj", name: "Câmbio PJ", description: "Operações de câmbio para clientes corporativos", owner: "squad-cambio@bbf.com.br", scopes: ["api:decision:read"], rateLimitPerMin: 40, status: "em_homologacao", requestsToday: 28, requestsMonth: 412, errorRate: 0.024, p95LatencyMs: 1880, successRate: 0.976, lastCall: "2026-04-29T11:20:00Z" },
  { consumerId: "back-office-aprovador", name: "Back-Office Aprovador", description: "Console interno usado por aprovadores manuais", owner: "squad-backoffice@bbf.com.br", scopes: ["api:decision:read", "api:documents:read", "api:audit:read"], rateLimitPerMin: 80, status: "ativo", requestsToday: 312, requestsMonth: 9_104, errorRate: 0.001, p95LatencyMs: 980, successRate: 0.999, lastCall: "2026-04-29T14:24:01Z", homologadoEm: "2026-02-01T10:00:00Z" },
  { consumerId: "auditoria-corp", name: "Auditoria Corporativa", description: "Sistema de auditoria interna que consulta a trilha", owner: "auditoria-interna@bbf.com.br", scopes: ["api:audit:read", "api:decision:replay"], rateLimitPerMin: 30, status: "ativo", requestsToday: 47, requestsMonth: 1_245, errorRate: 0.0, p95LatencyMs: 720, successRate: 1.0, lastCall: "2026-04-29T09:00:00Z", homologadoEm: "2026-02-15T15:00:00Z" },
  { consumerId: "fintech-parceira-x", name: "Fintech Parceira X", description: "Parceiro externo (POC) — convênio de antecipação de recebíveis", owner: "fintech-x@partner.com", scopes: ["api:decision:read"], rateLimitPerMin: 20, status: "suspenso", requestsToday: 0, requestsMonth: 0, errorRate: 0, p95LatencyMs: 0, successRate: 0, lastCall: "2026-03-15T17:00:00Z", homologadoEm: "2026-03-01T10:00:00Z" }
];

// --- Time-series para Observabilidade (EP-07-F3) ---
// Volumes por hora nas últimas 24h (24 pontos)
export const decisionsLast24h = [42, 38, 31, 22, 18, 15, 12, 18, 35, 58, 81, 94, 102, 112, 118, 121, 115, 108, 96, 84, 72, 61, 53, 47];
export const errorsLast24h = [0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 1, 2, 1, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0];
export const latencyP95Last24h = [1420, 1380, 1350, 1320, 1300, 1290, 1280, 1310, 1450, 1580, 1640, 1720, 1810, 1820, 1780, 1640, 1590, 1620, 1610, 1580, 1540, 1490, 1460, 1430];

// --- FinOps: custo por componente (EP-07-F9) ---
export interface CostBreakdown {
  componente: string;
  custoUnitarioBRL: number;
  participacaoPct: number;
  tendencia30d: "estavel" | "alta" | "queda";
  observacao?: string;
}

export const costPerDecisionBreakdown: CostBreakdown[] = [
  { componente: "LLM (Vertex AI Gemini Pro)", custoUnitarioBRL: 0.18, participacaoPct: 0.43, tendencia30d: "estavel", observacao: "~2.500 tokens médios por documento" },
  { componente: "OCR (Google Document AI)", custoUnitarioBRL: 0.09, participacaoPct: 0.21, tendencia30d: "queda", observacao: "Migração progressiva para Tesseract em casos simples" },
  { componente: "Junta Comercial (custo por consulta)", custoUnitarioBRL: 0.08, participacaoPct: 0.19, tendencia30d: "estavel", observacao: "Cache hit-rate 42% reduz custo" },
  { componente: "Infraestrutura compute (K8s)", custoUnitarioBRL: 0.04, participacaoPct: 0.10, tendencia30d: "alta", observacao: "Crescimento por volume" },
  { componente: "Armazenamento (S3 + PostgreSQL)", custoUnitarioBRL: 0.02, participacaoPct: 0.05, tendencia30d: "alta", observacao: "Retenção 5 anos do raw" },
  { componente: "Outros (Kafka, Redis, OTel)", custoUnitarioBRL: 0.01, participacaoPct: 0.02, tendencia30d: "estavel" }
];

// Custo por jornada consumidora (mensal)
export interface CostByConsumer {
  consumerId: string;
  name: string;
  decisoesMes: number;
  custoTotalMesBRL: number;
  custoMedioBRL: number;
}

export const costByConsumer: CostByConsumer[] = [
  { consumerId: "onboarding-pj", name: "Onboarding PJ", decisoesMes: 38_421, custoTotalMesBRL: 16_136.82, custoMedioBRL: 0.42 },
  { consumerId: "credito-pj", name: "Crédito PJ", decisoesMes: 18_312, custoTotalMesBRL: 8_607.84, custoMedioBRL: 0.47 },
  { consumerId: "back-office-aprovador", name: "Back-Office Aprovador", decisoesMes: 9_104, custoTotalMesBRL: 3_186.40, custoMedioBRL: 0.35 },
  { consumerId: "garantias-pj", name: "Garantias PJ", decisoesMes: 5_421, custoTotalMesBRL: 2_602.08, custoMedioBRL: 0.48 },
  { consumerId: "auditoria-corp", name: "Auditoria Corporativa", decisoesMes: 1_245, custoTotalMesBRL: 248.95, custoMedioBRL: 0.20 },
  { consumerId: "cambio-pj", name: "Câmbio PJ (homol.)", decisoesMes: 412, custoTotalMesBRL: 226.60, custoMedioBRL: 0.55 }
];
