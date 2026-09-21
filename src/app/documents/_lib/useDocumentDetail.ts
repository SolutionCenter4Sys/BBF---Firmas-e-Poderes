"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { ApiError } from "@/lib/api";
import { AUTH_UNAUTHORIZED_EVENT } from "@/lib/auth";
import {
  getDocumentCanonical,
  getDocumentStatus,
  isProcessingStatus,
  mergeDetail,
  STATUS_POLL_MS,
  type CanonicalDto,
  type DocumentDetail
} from "./document-api";

export type DocumentQuery =
  | { phase: "loading" }
  | { phase: "ok"; doc: DocumentDetail; polling: boolean }
  | { phase: "notfound" }
  | { phase: "unauthorized"; message: string }
  | { phase: "error"; message: string };

export function useDocumentDetail(documentId: string | undefined) {
  const [query, setQuery] = useState<DocumentQuery>({ phase: "loading" });
  const canonicalRef = useRef<CanonicalDto | null>(null);

  const load = useCallback(async (id: string, silent: boolean) => {
    if (!silent) setQuery({ phase: "loading" });
    try {
      const status = await getDocumentStatus(id);
      let canonical = canonicalRef.current;
      const shouldRefreshCanonical = !silent || !isProcessingStatus(status.status) || canonical === null;
      if (shouldRefreshCanonical) {
        try {
          canonical = await getDocumentCanonical(id);
          canonicalRef.current = canonical;
        } catch (err) {
          if (err instanceof ApiError && err.status === 404) {
            canonical = null;
            canonicalRef.current = null;
          } else if (err instanceof ApiError && err.status === 401) {
            setQuery({ phase: "unauthorized", message: err.message });
            return;
          } else if (!canonical) {
            const message = err instanceof Error ? err.message : "Falha ao carregar canônico.";
            setQuery({ phase: "error", message });
            return;
          }
        }
      }

      setQuery({
        phase: "ok",
        doc: mergeDetail(status, canonical),
        polling: isProcessingStatus(status.status)
      });
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setQuery({ phase: "notfound" });
        return;
      }
      if (err instanceof ApiError && err.status === 401) {
        setQuery({ phase: "unauthorized", message: err.message });
        return;
      }
      const message = err instanceof Error ? err.message : "Falha ao carregar documento.";
      setQuery({ phase: "error", message });
    }
  }, []);

  useEffect(() => {
    if (!documentId) {
      setQuery({ phase: "notfound" });
      return;
    }
    canonicalRef.current = null;
    void load(documentId, false);
  }, [documentId, load]);

  const polling = query.phase === "ok" && query.polling;

  useEffect(() => {
    if (!polling || !documentId) return;
    const timer = window.setInterval(() => {
      void load(documentId, true);
    }, STATUS_POLL_MS);
    return () => window.clearInterval(timer);
  }, [polling, documentId, load]);

  useEffect(() => {
    const onUnauthorized = () => {
      setQuery({ phase: "unauthorized", message: "Sessão expirada. Informe um JWT no Dashboard." });
    };
    window.addEventListener(AUTH_UNAUTHORIZED_EVENT, onUnauthorized);
    return () => window.removeEventListener(AUTH_UNAUTHORIZED_EVENT, onUnauthorized);
  }, []);

  const reload = useCallback(() => {
    if (!documentId) return;
    void load(documentId, false);
  }, [documentId, load]);

  return { query, reload };
}
