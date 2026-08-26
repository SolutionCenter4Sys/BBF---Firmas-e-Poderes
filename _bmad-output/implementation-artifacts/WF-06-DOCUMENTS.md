# WF-06 — Módulo Documents

**Workflow:** WF-06/22  
**Agente:** Ricardo  
**Branch:** `wf-06-documents`  
**Data:** 2026-08-26  
**Depende:** WF-04, WF-05

## Entrega

- `POST /v1/documents` multipart campo `file` → **202** `{ documentId, status: pendente, correlationId, uploadedAt }` + header `Location`.
- Blob no volume filesystem `data/docs/{documentId}` (não bytea).
- `GET /v1/documents` (lista) e `GET /v1/documents/{id}/status`.
- Outbox `outbox_messages` tipo `document.uploaded`, `processed_at` nulo. Sem OCR/KAAS no request.
- HTTP: **415** MIME; **422** tamanho (OS; teto 50 MB).

## Aceite

| Check | Resultado |
|---|---|
| `dotnet test backend/` | 23/23 |
| POST PDF válido (JWT operador) | 202 `status=pendente` |
| GET status | 200 `pendente` |
| POST `.txt` | 415 |
| POST acima do teto | 422 |
| Outbox | 1 linha `document.uploaded` não processada |

Verificação PG (`curl` + `\dt`): aplicar migration `AddDocumentStorageAndOutbox` com Docker (mesmo pré-requisito WSL dos WF-02/04). Testes HTTP usam InMemory + temp dir.

## Fora de escopo (OS)

KAAS (WF-07), decisão, front.
