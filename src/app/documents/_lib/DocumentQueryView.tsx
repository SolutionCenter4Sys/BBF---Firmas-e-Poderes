"use client";

import Link from "next/link";
import { notFound } from "next/navigation";
import type { ReactNode } from "react";
import type { DocumentDetail } from "./document-api";
import type { DocumentQuery } from "./useDocumentDetail";

export function DocumentQueryView({
  query,
  children
}: {
  query: DocumentQuery;
  children: (doc: DocumentDetail, polling: boolean) => ReactNode;
}) {
  if (query.phase === "loading") {
    return (
      <div className="card">
        <p style={{ color: "var(--color-text-secondary)", margin: 0 }}>Carregando GET /v1/documents/…</p>
      </div>
    );
  }

  if (query.phase === "unauthorized") {
    return (
      <div className="banner banner--err" role="alert">
        401 — cole JWT operador em sessionStorage <code>bbf.access_token</code> no{" "}
        <Link href="/">Dashboard</Link>. {query.message}
      </div>
    );
  }

  if (query.phase === "error") {
    return (
      <div className="banner banner--err" role="alert">
        {query.message}{" "}
        <Link href="/">← Voltar</Link>
      </div>
    );
  }

  if (query.phase === "notfound") {
    notFound();
  }

  return <>{children(query.doc, query.polling)}</>;
}
