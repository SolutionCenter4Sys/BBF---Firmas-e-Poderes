"use client";

import { useCallback, useEffect, useState } from "react";
import { JwtTokenForm } from "@/components/JwtTokenForm";
import { SourceHealthPanel } from "@/components/SourceHealthPanel";
import type { SourceHealth } from "@/domain";
import { ApiError } from "@/lib/api";
import { AUTH_UNAUTHORIZED_EVENT } from "@/lib/auth";
import { getVerificationHealth } from "@/lib/verification";

export default function SourcesHealthPage() {
  const [sources, setSources] = useState<SourceHealth[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [unauthorized, setUnauthorized] = useState(false);

  const refresh = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const items = await getVerificationHealth();
      setSources(items);
      setUnauthorized(false);
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        setUnauthorized(true);
        setSources([]);
      }
      setError(err instanceof Error ? err.message : "Falha ao consultar fontes.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  useEffect(() => {
    const onUnauthorized = () => setUnauthorized(true);
    window.addEventListener(AUTH_UNAUTHORIZED_EVENT, onUnauthorized);
    return () => window.removeEventListener(AUTH_UNAUTHORIZED_EVENT, onUnauthorized);
  }, []);

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Saúde das Fontes Oficiais</h2>
          <div className="subtitle">GET <code>/v1/verification/health</code> · stub in-memory (sem HTTP Junta)</div>
        </div>
        <button type="button" className="btn btn--secondary" onClick={() => void refresh()} disabled={loading}>
          {loading ? "Atualizando…" : "Atualizar agora"}
        </button>
      </div>

      {unauthorized && (
        <JwtTokenForm
          roleLabel="operador"
          hint="GET /v1/verification/health exige Bearer operador/admin."
          onSaved={() => void refresh()}
        />
      )}

      {error && !unauthorized && (
        <div className="banner banner--err" role="alert">✕ {error}</div>
      )}

      {loading && sources.length === 0 && !unauthorized && (
        <div className="card">
          <p style={{ margin: 0, color: "var(--color-text-secondary)" }}>Carregando GET /v1/verification/health…</p>
        </div>
      )}

      {sources.length > 0 && <SourceHealthPanel sources={sources} />}

      <div className="card">
        <h3>Política de fallback</h3>
        <ul style={{ lineHeight: 1.8 }}>
          <li>Quando uma fonte está <strong>indisponível</strong>, o motor de decisão (EP-04) retorna <strong>MANUAL</strong> com motivo &quot;validação indisponível&quot;.</li>
          <li>O <strong>circuit breaker</strong> abre após 5 falhas consecutivas em janela de 1 minuto.</li>
          <li>Em estado <strong>meio-aberto</strong>, o conector envia 1 requisição de teste a cada 30s para verificar recuperação.</li>
          <li>Alertas <strong>P1</strong> são disparados quando uma fonte fica indisponível por mais de 15 minutos.</li>
        </ul>
      </div>
    </>
  );
}
