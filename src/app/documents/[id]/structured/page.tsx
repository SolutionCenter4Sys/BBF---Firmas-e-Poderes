"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { routeDocumentId } from "../../_lib/document-api";
import { DocumentQueryView } from "../../_lib/DocumentQueryView";
import { useDocumentDetail } from "../../_lib/useDocumentDetail";

export default function StructuredPage() {
  const params = useParams<{ id: string }>();
  const { query } = useDocumentDetail(routeDocumentId(params.id));

  return (
    <DocumentQueryView query={query}>
      {(doc) => (
        <>
          <div className="page-header">
            <div>
              <h2>Visão semiestruturada</h2>
              <div className="subtitle">
                <code>{doc.documentId}</code> · {doc.fileName}
                {doc.paginas > 0 ? <> · {doc.paginas} páginas</> : null}
              </div>
            </div>
            <div style={{ display: "flex", gap: 8 }}>
              <Link href={`/documents/${doc.documentId}`} className="btn btn--ghost">← Voltar ao documento</Link>
              <button className="btn btn--secondary" type="button">Reprocessar com prompt v2.2.0 (beta)</button>
            </div>
          </div>

          <div className="card">
            <p>
              Sem seções identificadas. API não expõe visão semiestruturada; 404 só ocorre se{" "}
              <code>GET /v1/documents/{"{id}"}/status</code> não achar o documento.
            </p>
          </div>
        </>
      )}
    </DocumentQueryView>
  );
}
