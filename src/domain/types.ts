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
  documento?: string;
  rg?: string;
  personType?: "pf" | "pj";
  quotas?: number;
  mandateStart?: string;
  mandateEnd?: string;
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

export interface CreditReadiness {
  score: number;
  classification: "alto" | "medio" | "baixo";
  recommendation: "aprovado" | "revisao_manual" | "reprovado";
  justification: string;
  breakdown: Array<{ name: string; score: number; weight: number }>;
  criticalBlockers: string[];
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
  creditReadiness?: CreditReadiness;
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

export interface DocumentSection {
  titulo: string;
  paginaInicio: number;
  paginaFim: number;
  confianca: number;
  resumo: string;
}

export interface DocumentVersion {
  versionId: string;
  tipo: "raw" | "ocr" | "iagen" | "ner" | "canonical";
  criadoEm: string;
  tamanhoBytes: number;
  hash: string;
  notas: string;
}

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

export interface RoleDefinition {
  roleId: string;
  nome: string;
  descricao: string;
  usuariosAtivos: number;
  permissoes: string[];
}

export interface UserMock {
  email: string;
  nome: string;
  roles: string[];
  ultimoAcesso: string;
  status: "ativo" | "inativo";
}

export interface OpenApiEndpoint {
  method: "GET" | "POST" | "PUT" | "DELETE";
  path: string;
  summary: string;
  description: string;
  tag: string;
  params?: { name: string; in: "query" | "path" | "header" | "body"; type: string; required: boolean; description: string }[];
  responses: { code: number; description: string; example?: unknown }[];
}

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

export interface CostBreakdown {
  componente: string;
  custoUnitarioBRL: number;
  participacaoPct: number;
  tendencia30d: "estavel" | "alta" | "queda";
  observacao?: string;
}

export interface CostByConsumer {
  consumerId: string;
  name: string;
  decisoesMes: number;
  custoTotalMesBRL: number;
  custoMedioBRL: number;
}
