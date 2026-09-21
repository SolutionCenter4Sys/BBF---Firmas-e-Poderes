# WF-10 — Audit trail

**Workflow:** WF-10/22  
**Agente:** Ricardo  
**Branch:** `wf-10-audit`  
**Data:** 2026-08-27  
**Depende:** WF-05

## Entrega

- `GET /v1/audit/trail` (RBAC `audit:read` = **auditor**/admin). Query opcional: `documentId`, `correlationId`, `from`, `to` (ISO-8601). 400 se `from` > `to`.
- Tabela `audit_events` append-only: interceptor recusa UPDATE/DELETE. Evento tipado no mesmo `SaveChanges` suprime o genérico `command.persisted`.
- Eventos de domínio: `document.uploaded`, `ocr.completed`, `canonical.ready`, `decision.evaluated`, `kas.ingest`, `kas.result`.
- Payload: `AuditEvent[]` agrupado por `correlationId` (campo JSON `timestamp` = `occurred_at`). PII mascarado em `details`.

## Aceite

| Check | Resultado |
|---|---|
| `dotnet test backend/` | trail HTTP + interceptor + pipeline KAAS |
| Operador em `GET /v1/audit/trail` | **403** |
| Auditor após `POST /v1/documents` | **200** com `document.uploaded` |
| Filtros `documentId` / `correlationId` / `from` / `to` | recorte da trilha |

## Fora de escopo (OS)

Tela `/audit` (Sofia WF-16).
