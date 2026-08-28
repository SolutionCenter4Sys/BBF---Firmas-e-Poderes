# WF-08 — Modelo canônico

**Workflow:** WF-08/22  
**Agente:** Ricardo  
**Branch:** `wf-08-canonical`  
**Data:** 2026-08-26  
**Depende:** WF-04, WF-07

## Entrega

- `CanonicalMapper` lê JSON KAAS (`pessoas|socios` + `poderes`, raiz ou envelope `body|canonical|result|data|payload`) → `Person[]` / `Power[]`.
- Worker persiste sócios, poderes e `sourceTrace` (página, offset, snippet) após o POST `result`.
- JSON sem arrays estruturados → `documents.status = revisao_humana` (não grava pessoas/poderes).
- `GET /v1/documents/{id}/canonical` (RBAC `documents:read`) devolve schema `canonicalSchemaPreview`: `{ documentId, cnpj, pessoas, poderes }` com `sourceTrace` aninhado.

## Aceite

| Check | Resultado |
|---|---|
| `dotnet test backend/` | 41/41 |
| `GET /v1/documents/doc_001/canonical` | 3 sócios (p1–p3) + 2 poderes (pw1–pw2) iguais ao mock ACME |
| `sourceTrace` pw1 | page 4, offset 1280–1480, snippet da cláusula isolada |
| KAAS sem `pessoas`/`poderes` | status `revisao_humana` |
| KAAS estruturado ACME (WireMock) | status `canonico_pronto`, 3+2 persistidos |

## Fora de escopo (OS)

Motor de regras de decisão (WF-09).
