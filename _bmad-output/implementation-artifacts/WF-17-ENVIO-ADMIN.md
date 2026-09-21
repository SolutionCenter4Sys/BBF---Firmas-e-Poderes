# WF-17 — Envio KAAS via API .NET + admin restante

**Workflow:** WF-17/22  
**Agente:** Sofia (`Agents DownStream/dev-reactjs-esp.md`)  
**Branch:** `wf-17-envio-admin`  
**Data:** 2026-08-27  
**Depende:** WF-07 (worker), WF-14 (`POST /v1/documents`), WF-15 (poll status + canônico)

## Entrega

- `/envio` chama `POST /v1/documents` (202 + outbox). Não chama `/api/kas/run`.
- Poll `GET /v1/documents/{id}/status` + canônico. Worker já faz `action: result`.
- Botão **Forçar result** só se `falha` (nova jornada / mesmo arquivo). Sucesso: desabilitado “Result já enviado pelo worker”.
- `/kas-result?documentId=` lê API. Sem `sessionStorage` (`kas:last-run` / uploads de sessão apagados).
- `GET /v1/verification/health` em `/sources/health` e bloco persistido de `/observability`.
- Admin, DPO, data-health, consumers, diff, catálogo OpenAPI: mock com banner **Não persistido**. Docs-api aponta Swagger .NET.
- JWT continua em `sessionStorage` (`bbf.access_token`) — auth, não fonte de documentos.

## Aceite

| Check | Resultado |
|---|---|
| Envio | `POST {NEXT_PUBLIC_API_URL}/v1/documents` — sem `/api/kas/run` |
| JSON | status + canônico após worker; result não sai do browser |
| Forçar result | habilitado só em `falha` |
| kas-result F5 / outra aba | lê GET por `documentId` |
| Admin / SLO FinOps | banner “Não persistido” |
| Fontes | `GET /v1/verification/health` |
| Vite / AWS | intocados |

## Fora de escopo (OS)

AWS, Vite. Novos endpoints C# (`GET kas_runs`, force-result no worker). WF-16 (decision/audit/filas) já ligado — não reaberto.
