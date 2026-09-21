# WF-11 — API pública de autoridade + health fontes

**Workflow:** WF-11/22  
**Agente:** Ricardo  
**Branch:** `wf-11-authority`  
**Data:** 2026-08-27  
**Depende:** WF-09

## Entrega

- `GET /v1/authority/decision?cnpj&operation&signers` (RBAC `api:decision:read` = consumer/operador/admin). Headers `X-Correlation-Id` (gera se ausente) e `Idempotency-Key` (janela 24 h, mesmo `decisionId`).
- Query opcional `valor`. `signers` aceita IDs canônicos (`p1`) ou nome/cargo.
- Rate limit placeholder (`AddRateLimiter` fixed window; `RateLimiting:PermitLimit` default 200/min por `sub`) → **429** + `Retry-After`.
- `GET /v1/verification/health` (RBAC operador/admin) devolve as 4 fontes do mock com `circuitBreaker` `fechado` | `meio-aberto` | `aberto`. Stub in-memory. **Sem HTTP Junta.**
- Junta SP `aberto` → decisão **200 MANUAL** (fallback). Não 503.

## Aceite

| Check | Resultado |
|---|---|
| `dotnet test backend/` | authority ACME + health stub |
| GET ACME `cnpj=12.345.678/0001-90&operation=movimentacao_financeira&signers=p1` | **200** `APROVADO` |
| Params ausentes / CNPJ inválido | **400** ProblemDetails |
| Sem token | **401** |
| Junta Comercial HTTP real | **não** implementada |

## Fora de escopo (OS)

HTTP real Junta Comercial / Receita / PEP. Conector live fica para fase posterior.
