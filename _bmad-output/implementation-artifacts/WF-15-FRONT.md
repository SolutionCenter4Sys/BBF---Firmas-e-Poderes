# WF-15 — Documents detail (API)

**Workflow:** WF-15/22  
**Agente:** Sofia (`Agents DownStream/dev-reactjs-esp.md`)  
**Branch:** `wf-15-doc-detail`  
**Data:** 2026-08-27  
**Depende:** WF-14, WF-06, WF-08

## Entrega

Só `src/app/documents/**`. Sem `decision/`, `audit/`, `admin/`.

- Detalhe, canônico, histórico e semiestruturado leem `GET /v1/documents/{id}/status` + `GET /v1/documents/{id}/canonical`.
- Sócios e poderes vêm de `pessoas` / `poderes` do canônico (não de `mocks.ts` / `sessionStorage`).
- Poll `GET .../status` a cada 2,5 s enquanto `pendente` ou `processando_*`. Para ao chegar em estado terminal.
- 404 só se a API devolver 404 (Next `notFound`). Upload que some no `sessionStorage` deixa de 404 se existir no Postgres.

## Aceite

| Check | Resultado |
|---|---|
| `doc_001` (seed ACME) | status + 3 sócios + 2 poderes + sourceTrace |
| Upload WF-14 + F5 / outra aba | detalhe abre via GET (não `sessionStorage`) |
| id inexistente | 404 (`GET /status` 404) |
| `processando_*` | poll `/status` até sair do prefixo / `pendente` |
| decision / audit / admin | intocados |

## Fora de escopo (OS)

`decision/`, `audit/`, `admin/`. `GET /v1/decision/*`. Apagar `mocks.ts`. Razão social no header (DTO de status/canônico não devolve `razaoSocial`).
