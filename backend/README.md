# Backend — BBF Firmas e Poderes

ASP.NET Core 8 + EF Core + Npgsql + PostgreSQL 16.

WF-02: walking skeleton (health).  
WF-04: modelo de domínio + migration + seed ACME (`doc_001`).  
WF-05: JWT (issuer/audience), RBAC, ProblemDetails, Serilog + máscara CPF/CNPJ, `X-Correlation-Id`, Swagger, interceptor de audit append-only.  
WF-06: `POST /v1/documents` (202 + blob em `data/docs` + outbox). Sem KAAS/OCR no request (worker = WF-07).

## Estrutura

```
backend/
  BbfFirmasPoderes.sln
  src/BbfFirmasPoderes.Api
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
| `GET /health/live` | anônimo (probe) |
| `GET /health/ready` | anônimo (probe DB) |
| `GET /health` | JWT `auditor` ou `admin` |
| `GET /swagger` | anônimo |
| `POST /v1/documents` | JWT `operador`, `consumer` ou `admin` |
| `GET /v1/documents` e `GET /v1/documents/{id}/status` | JWT `operador`, `auditor`, `consumer` ou `admin` |
| demais endpoints | JWT (fallback) |

401 = sem token / token inválido (ProblemDetails). 403 = perfil sem permissão.

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
