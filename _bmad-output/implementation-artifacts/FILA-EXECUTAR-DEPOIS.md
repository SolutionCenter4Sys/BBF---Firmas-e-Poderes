# Fila — executar depois

Chat novo (copie um bloco):

- Docker / Postgres: *"executa a pendência Docker do WF-02 e WF-04"* + este arquivo
- Branch/commit WF-03: *"fecha git do WF-03 na branch wf-03-matriz-api"* + este arquivo

---

## WF-03 — contrato (já feito, sem Docker)

**Entrega:** `docs/arquitetura/WF-03_MAPA_PARIDADE_API.md`  
**OS:** contrato apenas. Sem C#, React, Docker.  
**Falta só git** (nesta máquina `git.exe` não estava no PATH):

```powershell
# achar git (GitHub Desktop / Program Files) e então:
git checkout -b wf-03-matriz-api
git add docs/arquitetura/WF-03_MAPA_PARIDADE_API.md
git status
# commit só se o usuário pedir
```

WF-03 **não** precisa de Docker, WSL nem Postgres.

---

## Docker — quando é obrigatório?

**Não.** Não é pré-requisito de tudo. Só quando o trabalho **fala com Postgres** ou **fecha evidência de health ready / compose**.

| Trabalho | Docker? | Por quê |
|---|---|---|
| WF-01 AR / ADRs | Não | só docs |
| WF-03 matriz API | Não | só contrato Markdown |
| Compilar .NET / `dotnet test` | Não | in-process |
| `GET /health/live` = 200 | Não | liveness sem DB |
| **Aplicar migration WF-02** (`InitialCreate`) | **Sim** | Postgres 16 em `compose.dev.yaml` |
| **`GET /health/ready` = 200** | **Sim** | readiness consulta o DB |
| **Fechar OS WF-02 (passo 1 VERIFICAÇÃO)** | **Sim** | evidência `db` healthy + ready 200 |
| **Aplicar migration WF-04** (`AddDomainModel` + seed ACME) | **Sim** | 6 tabelas + `\dt` |
| WF-05 JWT/RBAC/Swagger (código) | Não para escrever; **Sim** para testar contra DB |
| WF-06 Documents (202 + poll status) | **Sim** para evidência PG; testes HTTP **não** | `dotnet test` cobre 415/422/202. curl+PG precisa do engine |
| WF-07+ worker/KAAS/decisão | **Sim** | DB + (depois) compose maior |
| WF-19 compose 4 containers | **Sim** | PG + API + worker + Next |

Engine atual: Docker Desktop Linux em falha (`dockerDesktopLinuxEngine`). Causa: **WSL 2 não instalado**.

Pré-requisito **uma vez** (Admin), depois reboot:

```powershell
wsl --install
```

Abrir Docker Desktop → engine **verde**. Só então os comandos abaixo.

Detalhe por workflow:

- `_bmad-output/implementation-artifacts/WF-02-PENDENTE-DOCKER.md`
- `_bmad-output/implementation-artifacts/WF-04-PENDENTE-DOCKER.md`

---

## Comandos Docker (depois do engine verde)

Raiz do repo. Ordem: WF-02 (schema vazio) → WF-04 (domínio + seed).

```powershell
docker compose -f compose.dev.yaml up -d db

# PATH .NET se o terminal novo não achar:
$env:Path = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:Path"

cd backend
dotnet tool restore
dotnet ef database update --project src/BbfFirmasPoderes.Infrastructure --startup-project src/BbfFirmasPoderes.Api
cd ..

docker compose -f compose.dev.yaml ps
curl -i http://localhost:8080/health/ready
docker compose -f compose.dev.yaml exec db psql -U bbf -d bbf_firmas -c "\dt"
```

Aceite:

1. `db` healthy
2. `/health/ready` → **200 Healthy**
3. `\dt` → `documents`, `people`, `powers`, `decisions`, `audit_events`, `kas_runs` + `__EFMigrationsHistory`
