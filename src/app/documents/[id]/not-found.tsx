import Link from "next/link";

export default function DocumentNotFound() {
  return (
    <div className="card">
      <h2>Documento não encontrado</h2>
      <p>
        <code>GET /v1/documents/{"{id}"}/status</code> retornou 404. Documento não existe na API (Postgres).
        Recarregar a aba não recria upload — fonte é Postgres, não o browser.
      </p>
      <Link href="/" className="btn btn--ghost">← Dashboard</Link>
    </div>
  );
}
