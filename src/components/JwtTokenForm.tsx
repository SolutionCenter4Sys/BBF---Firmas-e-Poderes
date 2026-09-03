"use client";

import { FormEvent, useState } from "react";
import { loginWithPassword } from "@/lib/api";
import { setAccessToken } from "@/lib/auth";

const DEMO_OPERADOR_EMAIL = "ana.silva@bbf.com.br";

export function JwtTokenForm({
  roleLabel,
  hint,
  onSaved
}: {
  roleLabel: string;
  hint: string;
  onSaved: () => void | Promise<void>;
}) {
  const [email, setEmail] = useState(DEMO_OPERADOR_EMAIL);
  const [password, setPassword] = useState("");
  const [tokenDraft, setTokenDraft] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const applyLogin = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const session = await loginWithPassword(email, password);
      setAccessToken(session.accessToken, { email: session.email, role: session.role });
      setPassword("");
      await onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Falha no login.");
    } finally {
      setBusy(false);
    }
  };

  const applyToken = async (event: FormEvent) => {
    event.preventDefault();
    setAccessToken(tokenDraft);
    setTokenDraft("");
    await onSaved();
  };

  return (
    <div className="card" style={{ display: "grid", gap: 16 }}>
      <form onSubmit={applyLogin} style={{ display: "flex", gap: 12, alignItems: "end", flexWrap: "wrap" }}>
        <div style={{ flex: "1 1 240px" }}>
          <h3 style={{ marginBottom: 8 }}>Entrar ({roleLabel})</h3>
          <p style={{ margin: "0 0 8px", fontSize: 13, color: "var(--color-text-secondary)" }}>
            {hint} Demo operador: <code>ana.silva@bbf.com.br</code> / senha no README.
          </p>
          <input
            className="input"
            type="email"
            autoComplete="username"
            placeholder="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            aria-label="Email"
            style={{ marginBottom: 8 }}
          />
          <input
            className="input"
            type="password"
            autoComplete="current-password"
            placeholder="senha demo"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            aria-label="Senha"
          />
        </div>
        <button type="submit" className="btn btn--primary" disabled={busy || !email.trim() || !password}>
          {busy ? "Entrando…" : "Entrar"}
        </button>
      </form>
      {error && (
        <p style={{ margin: 0, color: "var(--color-status-rejected)", fontSize: 13 }} role="alert">
          {error}
        </p>
      )}
      <form onSubmit={applyToken} style={{ display: "flex", gap: 12, alignItems: "end", flexWrap: "wrap" }}>
        <div style={{ flex: "1 1 320px" }}>
          <p style={{ margin: "0 0 8px", fontSize: 12, color: "var(--color-text-secondary)" }}>
            Ou cole JWT {roleLabel} (sessionStorage <code>bbf.access_token</code>).
          </p>
          <input
            className="input"
            type="password"
            autoComplete="off"
            placeholder="eyJ…"
            value={tokenDraft}
            onChange={(e) => setTokenDraft(e.target.value)}
            aria-label={`JWT ${roleLabel}`}
          />
        </div>
        <button type="submit" className="btn" disabled={!tokenDraft.trim()}>
          Usar token
        </button>
      </form>
    </div>
  );
}
