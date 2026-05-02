"use client";
import { useState } from "react";
import { roles, users } from "@/lib/mocks";

const fmtDate = (iso: string) => new Date(iso).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" });

export default function RolesPage() {
  const [tab, setTab] = useState<"roles" | "users">("roles");

  return (
    <>
      <div className="page-header">
        <div>
          <h2>Perfis e Acessos (RBAC)</h2>
          <div className="subtitle">Gestão de perfis, permissões e usuários — integrado com IAM corporativo Bradesco</div>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <button className="btn btn--secondary">Sincronizar com IAM</button>
          <button className="btn btn--primary">+ Novo perfil</button>
        </div>
      </div>

      <div className="card" style={{ padding: 0 }}>
        <div style={{ display: "flex", borderBottom: "1px solid var(--color-border-default)" }}>
          <button
            onClick={() => setTab("roles")}
            className="btn btn--ghost"
            style={{ borderRadius: 0, padding: 16, borderBottom: tab === "roles" ? "2px solid var(--color-brand-primary)" : "2px solid transparent", color: tab === "roles" ? "var(--color-brand-primary)" : undefined }}
          >
            Perfis ({roles.length})
          </button>
          <button
            onClick={() => setTab("users")}
            className="btn btn--ghost"
            style={{ borderRadius: 0, padding: 16, borderBottom: tab === "users" ? "2px solid var(--color-brand-primary)" : "2px solid transparent", color: tab === "users" ? "var(--color-brand-primary)" : undefined }}
          >
            Usuários ({users.length})
          </button>
        </div>

        <div style={{ padding: 24 }}>
          {tab === "roles" && (
            <table>
              <thead>
                <tr><th>Perfil</th><th>Descrição</th><th>Permissões</th><th>Usuários ativos</th><th>Ações</th></tr>
              </thead>
              <tbody>
                {roles.map((r) => (
                  <tr key={r.roleId}>
                    <td><strong>{r.nome}</strong><div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}><code>{r.roleId}</code></div></td>
                    <td style={{ fontSize: 13 }}>{r.descricao}</td>
                    <td>
                      <div style={{ display: "flex", gap: 4, flexWrap: "wrap" }}>
                        {r.permissoes.map((p) => (
                          <span key={p} className="badge badge--neutral" style={{ fontFamily: "var(--font-family-mono)", fontSize: 11 }}>{p}</span>
                        ))}
                      </div>
                    </td>
                    <td>{r.usuariosAtivos}</td>
                    <td><a href="#">Editar →</a></td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}

          {tab === "users" && (
            <table>
              <thead>
                <tr><th>Usuário</th><th>Perfis</th><th>Último acesso</th><th>Status</th><th>Ações</th></tr>
              </thead>
              <tbody>
                {users.map((u) => (
                  <tr key={u.email}>
                    <td>
                      <strong>{u.nome}</strong>
                      <div style={{ fontSize: 12, color: "var(--color-text-secondary)" }}>{u.email}</div>
                    </td>
                    <td>
                      <div style={{ display: "flex", gap: 4, flexWrap: "wrap" }}>
                        {u.roles.map((rId) => {
                          const r = roles.find((x) => x.roleId === rId);
                          return <span key={rId} className="badge badge--info">{r?.nome ?? rId}</span>;
                        })}
                      </div>
                    </td>
                    <td style={{ fontSize: 13 }}>{fmtDate(u.ultimoAcesso)}</td>
                    <td>
                      <span className={`badge ${u.status === "ativo" ? "badge--ok" : "badge--neutral"}`}>{u.status}</span>
                    </td>
                    <td><a href="#">Editar →</a></td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>

      <div className="card">
        <h3>Princípios de governança de acesso</h3>
        <ul style={{ lineHeight: 1.8 }}>
          <li><strong>Menor privilégio:</strong> usuários recebem apenas as permissões necessárias para a função.</li>
          <li><strong>Segregação de funções:</strong> quem propõe regras (curador) é diferente de quem aprova (jurídico).</li>
          <li><strong>Toda decisão de acesso é registrada</strong> na trilha de auditoria (EP-06-F1).</li>
          <li><strong>Sincronização com IAM corporativo</strong> a cada hora; alterações urgentes podem ser feitas manualmente aqui.</li>
        </ul>
      </div>
    </>
  );
}
