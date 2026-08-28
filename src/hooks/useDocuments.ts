"use client";

import { useCallback, useEffect, useState } from "react";
import type { DocStatus } from "@/domain";
import { ApiError, apiFetch, parseApiProblem, readApiBody } from "@/lib/api";
import { AUTH_UNAUTHORIZED_EVENT } from "@/lib/auth";

export interface DocumentListItem {
  documentId: string;
  fileName: string;
  status: DocStatus;
  uploadedAt: string;
  correlationId?: string | null;
  cnpj?: string;
  razaoSocial?: string;
  tipoSocietario?: string;
  confianca?: { ocr: number; iagen: number; ner: number };
}

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

function isDocStatus(value: string): value is DocStatus {
  return (DOC_STATUSES as readonly string[]).includes(value);
}

function parseProblem(body: unknown, fallback: string): string {
  return parseApiProblem(body, fallback);
}

async function readBody(response: Response): Promise<unknown> {
  return readApiBody(response);
}

function parseConfidence(raw: unknown): { ocr: number; iagen: number; ner: number } | undefined {
  if (!raw || typeof raw !== "object") return undefined;
  const row = raw as Record<string, unknown>;
  const ocr = typeof row.ocr === "number" ? row.ocr : undefined;
  const iagen = typeof row.iagen === "number" ? row.iagen : undefined;
  const ner = typeof row.ner === "number" ? row.ner : undefined;
  if (ocr === undefined && iagen === undefined && ner === undefined) return undefined;
  return { ocr: ocr ?? 0, iagen: iagen ?? 0, ner: ner ?? 0 };
}

function normalizeListItem(raw: unknown): DocumentListItem | null {
  if (!raw || typeof raw !== "object") return null;
  const row = raw as Record<string, unknown>;
  if (typeof row.documentId !== "string" || row.documentId.length === 0) return null;
  const statusRaw = typeof row.status === "string" ? row.status : "pendente";
  return {
    documentId: row.documentId,
    fileName: typeof row.fileName === "string" && row.fileName.length > 0 ? row.fileName : "—",
    status: isDocStatus(statusRaw) ? statusRaw : "pendente",
    uploadedAt: typeof row.uploadedAt === "string" && row.uploadedAt.length > 0
      ? row.uploadedAt
      : new Date().toISOString(),
    correlationId: typeof row.correlationId === "string" ? row.correlationId : null,
    cnpj: typeof row.cnpj === "string" && row.cnpj.trim() ? row.cnpj : undefined,
    razaoSocial: typeof row.razaoSocial === "string" && row.razaoSocial.trim() ? row.razaoSocial : undefined,
    tipoSocietario: typeof row.tipoSocietario === "string" && row.tipoSocietario.trim()
      ? row.tipoSocietario
      : undefined,
    confianca: parseConfidence(row.confianca)
  };
}

export async function listDocuments(status?: DocStatus): Promise<DocumentListItem[]> {
  const suffix = status ? `?status=${encodeURIComponent(status)}` : "";
  const response = await apiFetch(`/v1/documents${suffix}`, { method: "GET", cache: "no-store" });
  const body = await readBody(response);
  if (!response.ok) {
    throw new ApiError(response.status, body, parseProblem(body, `GET /v1/documents → ${response.status}`));
  }
  if (!Array.isArray(body)) {
    throw new ApiError(response.status, body, "Lista de documentos inválida.");
  }
  return body.map(normalizeListItem).filter((item): item is DocumentListItem => item !== null);
}

export async function uploadDocument(
  file: File,
  correlationId?: string
): Promise<{ documentId: string; status: string; correlationId: string | null }> {
  const form = new FormData();
  form.append("file", file);
  const headers = new Headers();
  if (correlationId) headers.set("X-Correlation-Id", correlationId);
  const response = await apiFetch("/v1/documents", { method: "POST", body: form, headers });
  const body = await readBody(response);
  if (response.status !== 202) {
    throw new ApiError(response.status, body, parseProblem(body, `POST /v1/documents → ${response.status}`));
  }
  const payload = body && typeof body === "object"
    ? (body as { documentId?: unknown; status?: unknown; correlationId?: unknown })
    : {};
  const documentId = typeof payload.documentId === "string" ? payload.documentId : "";
  if (!documentId) {
    throw new ApiError(response.status, body, "Upload aceito sem documentId.");
  }
  const status = typeof payload.status === "string" ? payload.status : "pendente";
  const returnedCorr = typeof payload.correlationId === "string" ? payload.correlationId : correlationId ?? null;
  return { documentId, status, correlationId: returnedCorr };
}

export function useDocuments(status?: DocStatus) {
  const [items, setItems] = useState<DocumentListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [unauthorized, setUnauthorized] = useState(false);
  const [forbidden, setForbidden] = useState(false);

  const refresh = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const docs = await listDocuments(status);
      setItems(docs);
      setUnauthorized(false);
      setForbidden(false);
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        setItems([]);
        setUnauthorized(true);
        setForbidden(false);
        setError(err.message);
        return;
      }
      if (err instanceof ApiError && err.status === 403) {
        setItems([]);
        setForbidden(true);
        setError(err.message);
        return;
      }
      setItems([]);
      const message = err instanceof Error ? err.message : "Falha ao listar documentos.";
      setError(message);
    } finally {
      setLoading(false);
    }
  }, [status]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  useEffect(() => {
    const onUnauthorized = () => {
      setUnauthorized(true);
      setItems([]);
    };
    window.addEventListener(AUTH_UNAUTHORIZED_EVENT, onUnauthorized);
    return () => window.removeEventListener(AUTH_UNAUTHORIZED_EVENT, onUnauthorized);
  }, []);

  return { items, loading, error, unauthorized, forbidden, refresh };
}
