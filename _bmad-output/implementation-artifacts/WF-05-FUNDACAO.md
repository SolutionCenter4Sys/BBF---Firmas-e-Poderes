# WF-05 — Fundação transversal

**Workflow:** WF-05/22  
**Agente:** Ricardo  
**Branch:** `wf-05-foundation`  
**Data:** 2026-08-26

## Entrega

JWT (issuer/audience/signing key via `Jwt__*`), RBAC `operador|auditor|admin|consumer`, ProblemDetails, Serilog com máscara CPF/CNPJ, middleware `X-Correlation-Id`, Swagger só das rotas mapeadas, interceptor EF append-only em `audit_events`.

## Aceite

| Check | Resultado |
|---|---|
| `GET /swagger` | anônimo 200 |
| chamada sem token (`GET /health`) | 401 ProblemDetails |
| auditor em `GET /health` | 200 |
| `GET /health/live` | anônimo 200 (probe WF-02) |
| `dotnet test backend/` | 19/19 |

## Fora de escopo (OS)

Upload, KAAS, telas Next.
