"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { loginWithPassword } from "@/lib/api";
import { setAccessToken } from "@/lib/auth";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const session = await loginWithPassword(email, password);
      setAccessToken(session.accessToken, { email: session.email, role: session.role });
      router.replace("/");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Não foi possível entrar.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="login-screen">
      <div className="login-brand">
        <p className="login-eyebrow">Bradesco · BBF</p>
        <h1>Firmas e Poderes</h1>
        <p>Envie o contrato e acompanhe a análise de firmas e poderes até o resultado, em tempo real.</p>
      </div>
      <form className="login-card" onSubmit={onSubmit}>
        <h2>Entrar</h2>
        <label className="login-label" htmlFor="login-email">
          E-mail
        </label>
        <input
          id="login-email"
          className="input"
          type="email"
          autoComplete="username"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />
        <label className="login-label" htmlFor="login-password">
          Senha
        </label>
        <input
          id="login-password"
          className="input"
          type="password"
          autoComplete="current-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />
        {error && (
          <p className="login-error" role="alert">
            {error}
          </p>
        )}
        <button className="btn btn--primary" type="submit" disabled={busy || !email.trim() || !password}>
          {busy ? "Entrando…" : "Entrar"}
        </button>
      </form>
    </div>
  );
}
