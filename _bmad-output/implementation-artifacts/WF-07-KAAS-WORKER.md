# WF-07 — Worker pipeline + KAAS

**Workflow:** WF-07/22  
**Agente:** Ricardo  
**Branch:** `wf-07-kaas-worker`  
**Data:** 2026-08-26  
**Depende:** WF-06

## Entrega

- Projeto `BbfFirmasPoderes.Worker` consome `outbox_messages` (`document.uploaded`, `processed_at` nulo).
- POST jornada `testes-firmas-e-poderes` `mode=sync` `payload.action=ingest` + `correlationId` + `document_url` (data URL do blob).
- JSON bruto em `kas_runs`. Status `processando_ocr` → payload (ingest ok sem status → `processando_iagen`; result ok → `canonico_pronto`; HTTP/exceção → `falha`).
- Segundo POST `action=result` na mesma jornada / mesmo `correlationId`.
- Header `X-Flow-Api-Key` só no Worker (`Kas__ApiKey`). Timeout HTTP 300s.
- Next `/api/kas/run` e `/api/kas/return` devolvem **410**. `kas-client.ts` não envia a chave.

## Aceite

| Check | Resultado |
|---|---|
| `dotnet test backend/` | pipeline WireMock: ingest+result, 2 `KasRun`, status `canonico_pronto` |
| Chave no teste | fake `test-kaas-key-not-real` — sem Railway |
| Header | `X-Flow-Api-Key` só nas chamadas do worker |
| API .NET | não registra `IKasClient` |
| `/api/kas/*` | 410 Gone |

## Fora de escopo (OS)

AWS, tela `/envio` (Sofia WF-17), Dapper, canônico Person/Power (WF-08), compose 4 containers (WF-19).
