"use client";
import Link from "next/link";
import { usePathname } from "next/navigation";

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
    title: "Plataforma",
    items: [
      { href: "/sources/health", label: "Saúde das fontes" },
      { href: "/observability", label: "Observabilidade" },
      { href: "/api-consumers", label: "Consumidores da API" },
      { href: "/api-helper", label: "API Helper" },
      { href: "/docs-api", label: "API Docs (Swagger)" }
    ]
  }
];

export default function Sidebar() {
  const pathname = usePathname();
  return (
    <aside className="sidebar" aria-label="Navegação principal">
      <h1>🛡️ BBF Firmas &amp; Poderes</h1>
      <p style={{ fontSize: 12, color: "var(--color-text-muted)", margin: "0 0 24px 0" }}>
        MVP mock · v0.1
      </p>
      <nav>
        {groups.map((g) => (
          <div key={g.title} style={{ marginBottom: 16 }}>
            <div style={{ fontSize: 11, textTransform: "uppercase", color: "var(--color-text-muted)", padding: "0 12px 6px 12px", letterSpacing: 0.5, fontWeight: 600 }}>
              {g.title}
            </div>
            {g.items.map((it) => (
              <Link
                key={it.href}
                href={it.href}
                className={pathname === it.href ? "active" : ""}
                style={{ display: "block" }}
              >
                {it.label}
              </Link>
            ))}
          </div>
        ))}
      </nav>
    </aside>
  );
}
