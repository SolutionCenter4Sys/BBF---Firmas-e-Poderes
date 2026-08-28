# Backend — BBF Firmas e Poderes

ASP.NET Core 8 + EF Core + Npgsql + PostgreSQL 16.

WF-02: walking skeleton (health).  
WF-04: modelo de domínio + migration + seed ACME (`doc_001`).  
WF-05: JWT (issuer/audience), RBAC, ProblemDetails, Serilog + máscara CPF/CNPJ, `X-Correlation-Id`, Swagger, interceptor de audit append-only.  
WF-06: `POST /v1/documents` (202 + blob em `data/docs` + outbox). Sem KAAS/OCR no request (worker = WF-07).  
WF-07: Worker consome outbox, POST KAAS `ingest` + `result` (mesma jornada), persiste `kas_runs`, avança `DocStatus`. Header `X-Flow-Api-Key` só no Worker. Timeout 300s.  
WF-08: JSON KAAS → `Person[]`/`Power[]` + `sourceTrace`. `GET /v1/documents/{id}/canonical`. KAAS não estruturado → `revisao_humana`.  
WF-09: `POST /v1/decision/evaluate` (RN01–RN04 + TH01, snapshot de versões) e `POST /v1/decision/{id}/replay` (mesmo snapshot). Sem Junta real (WF-11).  
WF-11: `GET /v1/authority/decision` (consumer + Idempotency-Key + 429) e `GET /v1/verification/health` (stub circuit breaker). Sem HTTP Junta.  
WF-20: seed ACME + Delta REPROVADO + Gama MANUAL; `POST /v1/auth/login` (senhas só em `.env.example`).

## Estrutura

```
backend/
  BbfFirmasPoderes.sln
  src/BbfFirmasPoderes.Api
  src/BbfFirmasPoderes.Worker
  src/BbfFirmasPoderes.Domain
  src/BbfFirmasPoderes.Infrastructure
  tests/BbfFirmasPoderes.Tests
```

A API escuta **http://localhost:8080**.

## Pré-requisitos

- .NET SDK 8
- Docker Desktop com **WSL 2** (engine Linux). Sem WSL, `postgres:16` não sobe e `/health/ready` fica 503.

## Connection string

Padrão em `appsettings.json`. Override por ambiente:

```powershell
$env:ConnectionStrings__Postgres = "Host=localhost;Port=5432;Database=bbf_firmas;Username=bbf;Password=bbf"
```

## JWT (WF-05)

Env (obrigatório fora de Development):

| Variável | Exemplo |
|---|---|
| `Jwt__Issuer` | `https://bbf.local` |
| `Jwt__Audience` | `bbf-firmas-poderes` |
| `Jwt__SigningKey` | mínimo 32 bytes (HS256) |

Perfis (`role` claim): `operador`, `auditor`, `admin`, `consumer`. Alias `analista` → `operador`.

| Rota | Auth |
|---|---|
| `POST /v1/auth/login` | anônimo (demo operador/auditor; senhas em env) |
| `GET /health/live` | anônimo (probe) |
| `GET /health/ready` | anônimo (probe DB) |
| `GET /health` | JWT `auditor` ou `admin` |
| `GET /swagger` | anônimo |
| `POST /v1/documents` | JWT `operador`, `consumer` ou `admin` |
| `GET /v1/documents` e `GET /v1/documents/{id}/status` | JWT `operador`, `auditor`, `consumer` ou `admin` |
| `GET /v1/documents/{id}/canonical` | JWT `operador`, `auditor`, `consumer` ou `admin` |
| `POST /v1/decision/evaluate` | JWT `operador` ou `admin` |
| `POST /v1/decision/{id}/replay` | JWT `auditor`, `consumer` ou `admin` |
| `GET /v1/authority/decision` | JWT `consumer`, `operador` ou `admin` |
| `GET /v1/verification/health` | JWT `operador` ou `admin` |
| `GET /v1/audit/trail` | JWT `auditor` ou `admin` |
| demais endpoints | JWT (fallback) |

401 = sem token / token inválido (ProblemDetails). 403 = perfil sem permissão.

## Login demo (WF-20)

`POST /v1/auth/login` anônimo. Senhas só em `.env.example` (`DemoUsers__OperadorPassword` / `DemoUsers__AuditorPassword`).

```powershell
curl -s http://localhost:8080/v1/auth/login -H "Content-Type: application/json" -d "{\"email\":\"ana.silva@bbf.com.br\",\"password\":\"OperadorDemo!2026\"}"
```

Seed extra: `doc_004` REPROVADO, `doc_003` MANUAL. Boot: `Database__MigrateOnStartup=true`.

## Compose 4 containers (WF-19)

Na raiz. Sem ECS/RDS. `env_file`: `.env.example`.

```powershell
docker compose up -d --build
docker compose ps
curl http://localhost:8080/health/ready
curl -I http://localhost:3000
```

Serviços: `db` (Postgres 16, volume `bbf_pgdata`, sem porta no host), `api` (:8080), `worker` (health :8081 interno), `web` (Next standalone :3000). Blobs em volume `bbf_docs` (`/data/docs`). Rede `bbf-internal`.

Só o banco (SDK na máquina): `docker compose -f compose.dev.yaml up -d db`.

## Comandos

Na raiz do repositório:

```powershell
# 1. Banco
docker compose -f compose.dev.yaml up -d db

# 2. Build
dotnet build backend/BbfFirmasPoderes.sln

# 3. Testes
dotnet test backend/

# 4. Migration (tabelas de domínio + seed ACME)
cd backend
dotnet tool restore
dotnet ef database update --project src/BbfFirmasPoderes.Infrastructure --startup-project src/BbfFirmasPoderes.Api
cd ..

# 5. Conferir tabelas
docker compose -f compose.dev.yaml exec db psql -U bbf -d bbf_firmas -c "\dt"

# 6. API (porta 8080, Development já tem Jwt de laboratório)
dotnet run --project backend/src/BbfFirmasPoderes.Api
```

Diagrama ER: `docs/arquitetura/ER.md`. Contrato API: `docs/arquitetura/WF-03_MAPA_PARIDADE_API.md`.

## Health e Swagger

```powershell
curl -i http://localhost:8080/health/live
curl -i http://localhost:8080/health/ready
curl -i http://localhost:8080/swagger/index.html
curl -i http://localhost:8080/health
# sem token → 401

# auditor (Development: issuer/audience/key de appsettings.Development.json)
# mint JWT no teste TestAuth ou no IdP; header:
curl -i http://localhost:8080/health -H "Authorization: Bearer <jwt-auditor>"
```

`X-Correlation-Id`: cliente envia ou API gera `corr_<uuid>` e ecoa na resposta.

## Documents (WF-06)

Upload **assíncrono**. Blob no volume `data/docs/{documentId}` (não bytea). Outbox `document.uploaded` com `processed_at` nulo — worker WF-07 consome. Request **não** chama KAAS nem OCR.

| Rota | Sucesso | Erros |
|---|---|---|
| `POST /v1/documents` multipart campo `file` | **202** `{ documentId, status: pendente, correlationId, uploadedAt }` + `Location` | 401, 403, **415** MIME, **422** tamanho/vazio |
| `GET /v1/documents` | 200 lista | 401, 403 |
| `GET /v1/documents/{id}/status` | 200 `{ documentId, status, ... }` | 401, 403, 404 |

MIME: PDF / PNG / JPEG / WebP / TIFF. Teto: `Documents:MaxUploadBytes` (50 MB). Tamanho acima do teto → **422** (OS WF-06; mock também lista 422).

```powershell
# mint JWT operador (Development)
# use TestAuth do projeto de testes ou um JWT HS256 com issuer/audience/key de appsettings.Development.json

curl -i http://localhost:8080/v1/documents -H "Authorization: Bearer <jwt-operador>" -F "file=@contrato.pdf;type=application/pdf"
curl -i http://localhost:8080/v1/documents/<documentId>/status -H "Authorization: Bearer <jwt-operador>"
```

## Worker KAAS (WF-07)

Consome `outbox_messages` (`document.uploaded`, `processed_at` nulo). Dois POST na jornada `testes-firmas-e-poderes`: `action=ingest` (com `document_url`) e `action=result` (mesmo `correlationId`). JSON bruto em `kas_runs`. Status: `processando_ocr` → payload (ingest ok sem status → `processando_iagen`; result ok → `canonico_pronto`; erro → `falha`).

```powershell
$env:Kas__ApiKey = "<chave-somente-worker>"
$env:Kas__RunUrl = "https://kaas-core-dev.up.railway.app/kas/triggers/journeys/testes-firmas-e-poderes/run"
$env:Kas__TimeoutSeconds = "300"
dotnet run --project backend/src/BbfFirmasPoderes.Worker
```

Proibido `KAS_API_KEY` / `X-Flow-Api-Key` no Next.js. Rotas `/api/kas/*` devolvem **410**. Testes usam WireMock (chave fake `test-kaas-key-not-real`).

## Decisão (WF-09)

Motor no Domain (`DecisionEngine` + `DecisionRuleCatalog`). Isolada vs conjunta no canônico ACME. Replay devolve o snapshot persistido (não reconsulta Junta).

| Rota | Sucesso | Erros |
|---|---|---|
| `POST /v1/decision/evaluate` JSON `EvaluationRequest` | **200** `DecisionRecord` (`APROVADO` \| `REPROVADO` \| `MANUAL`) + motivos + evidências + `versions` | 401, 403, **400** input, **404** documento, **422** sem canônico |
| `POST /v1/decision/{decisionId}/replay` | **200** mesmo `DecisionRecord` | 401, 403, **404** |

ACME: `doc_001`, `movimentacao_financeira`, Diretor, R$ 500.000 → **APROVADO** (poder isolada `pw1`). Replay do `decisionId` devolvido é byte-a-byte igual.

## Autoridade + health de fontes (WF-11)

API pública do consumidor. Motor = mesmo `DecisionEngine` (canônico). **Sem HTTP Junta.** Circuit breaker é stub in-memory (`fechado` / `meio-aberto` / `aberto`). Se Junta SP está `aberto` → **200 MANUAL** (fallback), não 503.

| Rota | Auth | Sucesso | Erros |
|---|---|---|---|
| `GET /v1/authority/decision?cnpj&operation&signers` | JWT `consumer` (`api:decision:read`), `operador` ou `admin` | **200** `DecisionRecord` | 400 params, 401, 403, **429** rate limit |
| `GET /v1/verification/health` | JWT `operador` ou `admin` | **200** `SourceHealth[]` (4 fontes do mock) | 401, 403 |

Headers: `X-Correlation-Id` (gera `corr_<uuid>` se ausente), `Idempotency-Key` (opcional, janela 24 h — mesmo `decisionId`). Query opcional `valor`. `signers` = IDs (`p1,p2`) ou nomes.

ACME: `cnpj=12.345.678/0001-90&operation=movimentacao_financeira&signers=p1` → **200 APROVADO**. Sem token → **401**. Params faltando → **400**.

Rate limit placeholder: `RateLimiting:PermitLimit` (default 200/min por `sub`). 429 + `Retry-After`.
