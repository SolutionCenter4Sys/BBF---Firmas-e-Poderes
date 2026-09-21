"use client";
import { useEffect, useState } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import {
  AUTH_SESSION_EVENT,
  AUTH_UNAUTHORIZED_EVENT,
  clearAccessToken,
  getAuthProfile,
  type AuthProfile
} from "@/lib/auth";

interface NavItem {
  href: string;
  label: string;
}
interface NavGroup {
  title: string;
  items: NavItem[];
}

const groups: NavGroup[] = [
  {
    title: "Operação",
    items: [
      { href: "/", label: "Dashboard" },
      { href: "/review-queue", label: "Revisão humana" },
      { href: "/manual-queue", label: "Análise manual" }
    ]
  },
  {
    title: "Decisão",
    items: [{ href: "/decision", label: "Decisão" }]
  },
  {
    title: "Compliance",
    items: [
      { href: "/audit", label: "Auditoria" },
      { href: "/dpo", label: "Console DPO" },
      { href: "/data-health", label: "Saúde de Dados" },
      { href: "/admin/roles", label: "Perfis (RBAC)" }
    ]
  },
  {
    title: "Domínio",
    items: [
      { href: "/admin/operations", label: "Operações" },
      { href: "/admin/rules", label: "Regras de decisão" }
    ]
  },
  {
    title: "Planejamento",
    items: [
      { href: "/plano.html", label: "Plano de execução (HTML)" }
    ]
  },
  {
    title: "Plataforma",
    items: [
      { href: "/observability", label: "Observabilidade" },
      { href: "/api-consumers", label: "Consumidores da API" },
      { href: "/api-helper", label: "API Helper" },
      { href: "/docs-api", label: "API Docs (Swagger)" }
    ]
  }
];

export default function Sidebar() {
  const pathname = usePathname();
  const router = useRouter();
  const [profile, setProfile] = useState<AuthProfile | null>(null);
  const showEnvBadge =
    process.env.NEXT_PUBLIC_APP_ENV === "development"
    || process.env.NEXT_PUBLIC_APP_ENV === "staging";

  useEffect(() => {
    const sync = () => setProfile(getAuthProfile());
    sync();
    window.addEventListener(AUTH_SESSION_EVENT, sync);
    window.addEventListener(AUTH_UNAUTHORIZED_EVENT, sync);
    return () => {
      window.removeEventListener(AUTH_SESSION_EVENT, sync);
      window.removeEventListener(AUTH_UNAUTHORIZED_EVENT, sync);
    };
  }, []);

  const logout = () => {
    clearAccessToken();
    router.replace("/login");
  };

  return (
    <aside className="sidebar" aria-label="Navegação principal">
      <h1>🛡️ BBF Firmas &amp; Poderes</h1>
      {showEnvBadge && (
        <p style={{ fontSize: 12, color: "var(--color-text-muted)", margin: "0 0 24px 0" }}>
          MVP mock · v0.1
        </p>
      )}
      <nav>
        {groups.map((g) => (
          <div key={g.title} style={{ marginBottom: 16 }}>
            <div style={{ fontSize: 11, textTransform: "uppercase", color: "var(--color-text-muted)", padding: "0 12px 6px 12px", letterSpacing: 0.5, fontWeight: 600 }}>
              {g.title}
            </div>
            {g.items.map((it) =>
              it.href.endsWith(".html") ? (
                <a key={it.href} href={it.href} target="_blank" rel="noreferrer" style={{ display: "block" }}>
                  {it.label}
                </a>
              ) : (
                <Link
                  key={it.href}
                  href={it.href}
                  className={pathname === it.href || (it.href !== "/" && pathname.startsWith(it.href)) ? "active" : ""}
                  style={{ display: "block" }}
                >
                  {it.label}
                </Link>
              )
            )}
          </div>
        ))}
      </nav>
      <div className="sidebar-footer">
        {profile && (
          <div className="sidebar-user" aria-label="Usuário autenticado">
            <strong>{profile.name}</strong>
            <span>{profile.roleLabel}</span>
          </div>
        )}
        <button type="button" className="btn btn--primary sidebar-logout" onClick={logout}>
          Sair
        </button>
      </div>
    </aside>
  );
}
