# WF-19 — Docker Compose (4 containers)

**Workflow:** WF-19/22  
**Agente:** DevOps Master  
**Branch:** `wf-19-compose`  
**Data:** 2026-08-27  
**Depende:** WF-07 (worker), WF-14 (web/dashboard)

## Entrega

- `compose.yaml`: `db`, `api`, `worker`, `web`.
- `backend/Dockerfile` (targets `api` + `worker`) e `Dockerfile` (Next `output: "standalone"`).
- Healthchecks nos 4 serviços. Rede `bbf-internal`. Volumes `bbf_pgdata` + `bbf_docs` (`/data/docs` na API e no worker).
- `env_file: .env.example` (JWT, Postgres, Kas, Documents, migrate).
- API: `Database__MigrateOnStartup=true` (EF migrate no boot; testes InMemory não disparam).
- Worker: probe HTTP `:8081` `/health/ready` (DbContext). Porta não publicada no host.
- Postgres **sem** bind 5432 no host (só rede interna). API `:8080` e web `:3000` no localhost.

## Aceite

| Check | Resultado |
|---|---|
| `dotnet build` Worker (SDK Web + `/health/ready`) | OK 0 erros |
| `next build` `output: "standalone"` | `STANDALONE_OK` (`.next/standalone/server.js`) |
| `docker compose up -d --build` | **bloqueado nesta máquina** — engine Linux 500. `wsl -l` → WSL **não instalado** (mesmo gap WF-02) |
| `docker compose ps` / curls | pendente WSL 2 + engine verde |

## Fora de escopo (OS)

ECS, RDS, push para registry AWS.

## Como fechar o aceite (Admin, uma vez)

```powershell
wsl --install
```

Reboot. Abrir Docker Desktop (engine verde). Na raiz, branch `wf-19-compose`:

```powershell
docker compose up -d --build
docker compose ps
curl http://localhost:8080/health/ready
curl -I http://localhost:3000
```
