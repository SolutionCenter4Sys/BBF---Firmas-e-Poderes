# WF-14 — Dashboard + upload

**Workflow:** WF-14/22  
**Agente:** Sofia (`Agents DownStream/dev-reactjs-esp.md`)  
**Branch:** `wf-14-dashboard`  
**Data:** 2026-08-27  
**Depende:** WF-13, WF-06

## Entrega

- Lista do dashboard via `GET /v1/documents` (Postgres). Sem `sessionStorage` / seed de `mocks.ts` na tabela.
- Upload via `POST /v1/documents` (Api .NET, 202). Não chama `/api/kas/run`.
- Dropzone + teto 50 MB mantidos.
- KPI continua mock (`metrics` em `mocks.ts`) — `GET /v1/metrics` não existe.
- 401: colar JWT operador em `sessionStorage` (`bbf.access_token`).

## Aceite

| Check | Resultado |
|---|---|
| Lista | `GET {NEXT_PUBLIC_API_URL}/v1/documents` |
| Upload | `POST` multipart campo `file` → 202 `pendente` |
| F5 | documento reaparece (PG, não sessionStorage) |
| KAAS | não enviado neste WF |

## Fora de escopo (OS)

Pages de detalhe, admin, envio KAAS.
