# WF-16 — Decision + audit UI

**Workflow:** WF-16/22  
**Agente:** Sofia (`Agents DownStream/dev-reactjs-esp.md`)  
**Branch:** `wf-16-decision-audit`  
**Data:** 2026-08-27  
**Depende:** WF-09, WF-10, WF-11, WF-13, WF-14

## Entrega

- `/decision` — `POST /v1/decision/evaluate` + `POST /v1/decision/{id}/replay`. Lista recente via `GET /v1/documents?status=decidido|revisao_humana`. Sem `mocks.ts`.
- `/api-helper` — `GET /v1/authority/decision` real (query `cnpj`, `operation`, `signers`, `valor`; header `Idempotency-Key`). cURL aponta para `NEXT_PUBLIC_API_URL`.
- `/audit` — `GET /v1/audit/trail` com `documentId`, `correlationId`, `from`, `to`. 403 para operador. CSV a partir da resposta.
- `/review-queue` e `/manual-queue` — `GET /v1/documents?status=` (default `revisao_humana`). Sem seed de fila.
- API: query `status` em `GET /v1/documents` (400 se inválido). Lista passa a incluir `cnpj`, `razaoSocial`, `confianca`.

## Aceite

| Check | Resultado |
|---|---|
| Decision | POST evaluate / replay; JWT operador |
| Helper | GET `/v1/authority/decision` (ACME `signers=p1` → APROVADO) |
| Audit | GET `/v1/audit/trail`; 403 operador; 200 auditor |
| Filas | `GET /v1/documents?status=revisao_humana` |
| `DocumentsApiTests` | **9/9** (3 novos: decidido inclui ACME, revisao exclui ACME, status inválido 400) |
| `tsc --noEmit` | 0 erros |

## Fora de escopo (OS)

Admin rules engine novo. `/envio`.
