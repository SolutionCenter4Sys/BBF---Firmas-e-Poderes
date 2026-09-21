# WF-04 — pendência Docker (executar depois)

**Branch:** `wf-04-ef-model`  
**Status (27/08/2026):** **FECHADO neste host.** `\dt` lista 8 tabelas. Seed `doc_001` ACME / `doc_004` Delta / `doc_003` Gama no Postgres.

**Retomar neste chat (ou novo):**  
`executa a pendência Docker do WF-04` + este arquivo.

---

## Docker é obrigatório?

| Trabalho | Precisa Docker? |
|----------|-----------------|
| Entidades, `PiiMask`, ER.md, migration gerada | **Não** — já feito |
| `dotnet build` / `dotnet test` (6 testes) | **Não** |
| Rodar API + `GET /health/live` | **Não** |
| `dotnet ef database update` | **Sim** — Postgres 16 tem que existir |
| `\dt` no psql (aceite da OS) | **Sim** |
| `GET /health/ready` = 200 | **Sim** |
| KAAS live / AWS | Fora deste recorte |

Postgres do plano vive em `compose.dev.yaml` (`postgres:16`, porta 5432). Sem Docker, só substitui se instalar Postgres 16 nativo com a mesma connection string — **não** é o caminho da OS.

Connection string:

```
Host=localhost;Port=5432;Database=bbf_firmas;Username=bbf;Password=bbf
```

---

## Pré-requisito (uma vez, Admin)

```powershell
wsl --install
```

Reiniciar o Windows. Abrir Docker Desktop. Esperar engine **verde**.

SDK .NET 8 (se o terminal novo não achar `dotnet`):

```powershell
$env:Path = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:Path"
```

---

## Comandos (raiz do repo, branch `wf-04-ef-model`)

Aplica WF-02 (`InitialCreate`) **e** WF-04 (`AddDomainModel` + seed ACME) de uma vez.

```powershell
docker compose -f compose.dev.yaml up -d db
docker compose -f compose.dev.yaml ps

cd backend
dotnet tool restore
dotnet ef database update --project src/BbfFirmasPoderes.Infrastructure --startup-project src/BbfFirmasPoderes.Api
cd ..

docker compose -f compose.dev.yaml exec db psql -U bbf -d bbf_firmas -c "\dt"
docker compose -f compose.dev.yaml exec db psql -U bbf -d bbf_firmas -c "SELECT document_id, razao_social, status FROM documents;"
```

### Aceite

- `db` **healthy**
- `\dt` lista: `documents`, `people`, `powers`, `decisions`, `audit_events`, `kas_runs` (+ `__EFMigrationsHistory`)
- `SELECT` devolve `doc_001` / `ACME Indústrias LTDA` / `decidido`

Opcional: `dotnet run --project backend/src/BbfFirmasPoderes.Api` → `curl -i http://localhost:8080/health/ready` → **200**.
