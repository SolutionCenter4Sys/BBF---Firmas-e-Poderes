import type { DocStatus, DocumentVersion, Person, Power } from "@/domain";
import { ApiError, apiFetch } from "@/lib/api";

const DOC_STATUSES: readonly DocStatus[] = [
  "pendente",
  "processando_ocr",
  "processando_iagen",
  "processando_ner",
  "canonico_pronto",
  "validacao_oficial",
  "decidido",
  "revisao_humana",
  "falha"
];

export const STATUS_POLL_MS = 2500;

export function routeDocumentId(value: string | string[] | undefined): string | undefined {
  if (typeof value === "string" && value.length > 0) return value;
  if (Array.isArray(value) && value[0]) return value[0];
  return undefined;
}

export interface DocumentStatusDto {
  documentId: string;
  status: DocStatus;
  fileName: string;
  uploadedAt: string;
  correlationId: string | null;
  paginas: number;
  hash: string | null;
  confianca: { ocr: number; iagen: number; ner: number };
  lastError: string | null;
}

export interface CanonicalDto {
  documentId: string;
  cnpj: string;
  pessoas: Person[];
  poderes: Power[];
}

export interface DocumentDetail {
  documentId: string;
  fileName: string;
  cnpj: string;
  status: DocStatus;
  uploadedAt: string;
  correlationId: string | null;
  paginas: number;
  hash: string | null;
  confianca: { ocr: number; iagen: number; ner: number };
  lastError: string | null;
  socios: Person[];
  poderes: Power[];
}

export function isDocStatus(value: string): value is DocStatus {
  return (DOC_STATUSES as readonly string[]).includes(value);
}

export function isProcessingStatus(status: DocStatus): boolean {
  return status === "pendente" || status.startsWith("processando_");
}

export function parseProblem(body: unknown, fallback: string): string {
  if (body && typeof body === "object") {
    const rec = body as { detail?: unknown; title?: unknown };
    if (typeof rec.detail === "string" && rec.detail.trim()) return rec.detail;
    if (typeof rec.title === "string" && rec.title.trim()) return rec.title;
  }
  return fallback;
}

async function readBody(response: Response): Promise<unknown> {
  const contentType = response.headers.get("content-type") ?? "";
  if (contentType.includes("application/json") || contentType.includes("application/problem+json")) {
    return response.json().catch(() => null);
  }
  const text = await response.text();
  return text.length > 0 ? text : null;
}

function num(value: unknown, fallback = 0): number {
  if (typeof value === "number" && Number.isFinite(value)) return value;
  if (typeof value === "string" && value.trim()) {
    const n = Number(value);
    if (Number.isFinite(n)) return n;
  }
  return fallback;
}

function str(value: unknown, fallback = ""): string {
  return typeof value === "string" ? value : fallback;
}

function optionalStr(value: unknown): string | null {
  return typeof value === "string" && value.length > 0 ? value : null;
}

function parseConfianca(raw: unknown): { ocr: number; iagen: number; ner: number } {
  if (!raw || typeof raw !== "object") return { ocr: 0, iagen: 0, ner: 0 };
  const row = raw as Record<string, unknown>;
  return { ocr: num(row.ocr), iagen: num(row.iagen), ner: num(row.ner) };
}

function parsePerson(raw: unknown): Person | null {
  if (!raw || typeof raw !== "object") return null;
  const row = raw as Record<string, unknown>;
  const personId = str(row.personId);
  if (!personId) return null;
  const statusRaw = str(row.status, "ativo");
  return {
    personId,
    nome: str(row.nome, "—"),
    cpf: str(row.cpf),
    documento: str(row.documento) || str(row.cpf),
    rg: str(row.rg),
    personType: str(row.personType, "pf").toLowerCase() === "pj" ? "pj" : "pf",
    quotas: row.quotas == null ? undefined : num(row.quotas),
    mandateStart: optionalStr(row.mandateStart)?.slice(0, 10) || undefined,
    mandateEnd: optionalStr(row.mandateEnd)?.slice(0, 10) || undefined,
    qualificacao: str(row.qualificacao),
    cargo: str(row.cargo),
    status: statusRaw === "inativo" ? "inativo" : "ativo"
  };
}

function parsePower(raw: unknown): Power | null {
  if (!raw || typeof raw !== "object") return null;
  const row = raw as Record<string, unknown>;
  const powerId = str(row.powerId);
  if (!powerId) return null;

  const limiteRaw = row.limite && typeof row.limite === "object" ? (row.limite as Record<string, unknown>) : {};
  const modoRaw =
    row.modoAssinatura && typeof row.modoAssinatura === "object"
      ? (row.modoAssinatura as Record<string, unknown>)
      : {};
  const vigenciaRaw =
    row.vigencia && typeof row.vigencia === "object" ? (row.vigencia as Record<string, unknown>) : {};
  const traceRaw =
    row.sourceTrace && typeof row.sourceTrace === "object" ? (row.sourceTrace as Record<string, unknown>) : {};

  const tipoRaw = str(modoRaw.tipo, "isolada");
  const n = modoRaw.n == null ? undefined : num(modoRaw.n);
  const m = modoRaw.m == null ? undefined : num(modoRaw.m);
  const qualificacoes = Array.isArray(modoRaw.qualificacoes)
    ? modoRaw.qualificacoes.filter((q): q is string => typeof q === "string")
    : undefined;

  return {
    powerId,
    pessoa: str(row.pessoa),
    operacao: str(row.operacao, "—"),
    limite: {
      currency: "BRL",
      value: num(limiteRaw.value),
      expression: str(limiteRaw.expression)
    },
    modoAssinatura: {
      tipo: tipoRaw === "conjunta" ? "conjunta" : "isolada",
      n,
      m,
      qualificacoes: qualificacoes && qualificacoes.length > 0 ? qualificacoes : undefined
    },
    vigencia: {
      validFrom: str(vigenciaRaw.validFrom).slice(0, 10),
      validTo: optionalStr(vigenciaRaw.validTo)?.slice(0, 10) || undefined
    },
    sourceTrace: {
      page: num(traceRaw.page),
      offsetStart: num(traceRaw.offsetStart),
      offsetEnd: num(traceRaw.offsetEnd),
      snippet: str(traceRaw.snippet)
    }
  };
}

export function parseStatus(body: unknown): DocumentStatusDto | null {
  if (!body || typeof body !== "object") return null;
  const row = body as Record<string, unknown>;
  const documentId = str(row.documentId);
  if (!documentId) return null;
  const statusRaw = str(row.status, "pendente");
  return {
    documentId,
    status: isDocStatus(statusRaw) ? statusRaw : "pendente",
    fileName: str(row.fileName, "—"),
    uploadedAt: str(row.uploadedAt) || new Date().toISOString(),
    correlationId: optionalStr(row.correlationId),
    paginas: num(row.paginas),
    hash: optionalStr(row.hash),
    confianca: parseConfianca(row.confianca),
    lastError: optionalStr(row.lastError)
  };
}

export function parseCanonical(body: unknown): CanonicalDto | null {
  if (!body || typeof body !== "object") return null;
  const row = body as Record<string, unknown>;
  const documentId = str(row.documentId);
  if (!documentId) return null;
  const pessoasRaw = Array.isArray(row.pessoas) ? row.pessoas : [];
  const poderesRaw = Array.isArray(row.poderes) ? row.poderes : [];
  return {
    documentId,
    cnpj: str(row.cnpj),
    pessoas: pessoasRaw.map(parsePerson).filter((p): p is Person => p !== null),
    poderes: poderesRaw.map(parsePower).filter((p): p is Power => p !== null)
  };
}

export function mergeDetail(status: DocumentStatusDto, canonical: CanonicalDto | null): DocumentDetail {
  return {
    documentId: status.documentId,
    fileName: status.fileName,
    cnpj: canonical?.cnpj ?? "",
    status: status.status,
    uploadedAt: status.uploadedAt,
    correlationId: status.correlationId,
    paginas: status.paginas,
    hash: status.hash,
    confianca: status.confianca,
    lastError: status.lastError,
    socios: canonical?.pessoas ?? [],
    poderes: canonical?.poderes ?? []
  };
}

export async function getDocumentStatus(documentId: string): Promise<DocumentStatusDto> {
  const response = await apiFetch(`/v1/documents/${encodeURIComponent(documentId)}/status`, {
    method: "GET",
    cache: "no-store"
  });
  const body = await readBody(response);
  if (!response.ok) {
    throw new ApiError(response.status, body, parseProblem(body, `GET /v1/documents/{id}/status → ${response.status}`));
  }
  const parsed = parseStatus(body);
  if (!parsed) {
    throw new ApiError(response.status, body, "Status de documento inválido.");
  }
  return parsed;
}

export async function getDocumentCanonical(documentId: string): Promise<CanonicalDto> {
  const response = await apiFetch(`/v1/documents/${encodeURIComponent(documentId)}/canonical`, {
    method: "GET",
    cache: "no-store"
  });
  const body = await readBody(response);
  if (!response.ok) {
    throw new ApiError(response.status, body, parseProblem(body, `GET /v1/documents/{id}/canonical → ${response.status}`));
  }
  const parsed = parseCanonical(body);
  if (!parsed) {
    throw new ApiError(response.status, body, "Canônico de documento inválido.");
  }
  return parsed;
}

export function versionsFromDetail(doc: DocumentDetail): DocumentVersion[] {
  const versions: DocumentVersion[] = [
    {
      versionId: "raw",
      tipo: "raw",
      criadoEm: doc.uploadedAt,
      tamanhoBytes: 0,
      hash: doc.hash ?? "—",
      notas: `Upload original (${doc.fileName})`
    }
  ];

  if (doc.confianca.ocr > 0 || isPast(doc.status, "processando_ocr")) {
    versions.push({
      versionId: "ocr",
      tipo: "ocr",
      criadoEm: doc.uploadedAt,
      tamanhoBytes: 0,
      hash: "—",
      notas: `OCR (confiança ${(doc.confianca.ocr * 100).toFixed(0)}%)`
    });
  }
  if (doc.confianca.iagen > 0 || isPast(doc.status, "processando_iagen")) {
    versions.push({
      versionId: "iagen",
      tipo: "iagen",
      criadoEm: doc.uploadedAt,
      tamanhoBytes: 0,
      hash: "—",
      notas: `IA Gen (confiança ${(doc.confianca.iagen * 100).toFixed(0)}%)`
    });
  }
  if (doc.confianca.ner > 0 || isPast(doc.status, "processando_ner")) {
    versions.push({
      versionId: "ner",
      tipo: "ner",
      criadoEm: doc.uploadedAt,
      tamanhoBytes: 0,
      hash: "—",
      notas: `NER (confiança ${(doc.confianca.ner * 100).toFixed(0)}%)`
    });
  }
  if (doc.socios.length > 0 || doc.poderes.length > 0 || isPast(doc.status, "canonico_pronto")) {
    versions.push({
      versionId: "canonical",
      tipo: "canonical",
      criadoEm: doc.uploadedAt,
      tamanhoBytes: 0,
      hash: "—",
      notas: `Canônico v1.0.0 (${doc.socios.length} sócios, ${doc.poderes.length} poderes)`
    });
  }

  return versions;
}

const STATUS_RANK: Record<DocStatus, number> = {
  pendente: 0,
  processando_ocr: 1,
  processando_iagen: 2,
  processando_ner: 3,
  canonico_pronto: 4,
  validacao_oficial: 5,
  decidido: 6,
  revisao_humana: 4,
  falha: 0
};

function isPast(status: DocStatus, stage: DocStatus): boolean {
  return STATUS_RANK[status] > STATUS_RANK[stage];
}
