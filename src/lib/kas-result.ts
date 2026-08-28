import type { DocStatus } from "@/domain";
import {
  getDocumentCanonical,
  getDocumentStatus,
  isProcessingStatus,
  type CanonicalDto,
  type DocumentStatusDto
} from "@/app/documents/_lib/document-api";

export interface KasRunView {
  documentId: string;
  fileName: string;
  status: DocStatus;
  correlationId: string | null;
  uploadedAt: string;
  ok: boolean;
  polling: boolean;
  canonical: CanonicalDto | null;
  statusDto: DocumentStatusDto;
}

export function resultAlreadyPosted(status: DocStatus): boolean {
  return status !== "pendente"
    && status !== "falha"
    && !isProcessingStatus(status);
}

export async function loadKasRunView(documentId: string): Promise<KasRunView> {
  const statusDto = await getDocumentStatus(documentId);
  let canonical: CanonicalDto | null = null;
  try {
    canonical = await getDocumentCanonical(documentId);
  } catch {
    canonical = null;
  }

  return {
    documentId: statusDto.documentId,
    fileName: statusDto.fileName,
    status: statusDto.status,
    correlationId: statusDto.correlationId,
    uploadedAt: statusDto.uploadedAt,
    ok: statusDto.status !== "falha",
    polling: isProcessingStatus(statusDto.status),
    canonical,
    statusDto
  };
}

export function kasJsonBody(view: KasRunView): unknown {
  if (view.canonical) return view.canonical;

  return {
    status: view.status,
    correlationId: view.correlationId,
    fileName: view.fileName
  };
}
