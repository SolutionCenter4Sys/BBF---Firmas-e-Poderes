# WF-13 — Front fundação (API client)

**Workflow:** WF-13/22  
**Agente:** Sofia (`Agents DownStream/dev-reactjs-esp.md`)  
**Branch:** `wf-13-front-client`  
**Data:** 2026-08-27  
**Depende:** WF-12 (API no ar) ou WF-05 + WF-06 (`GET /health/live` anônimo)

## Entrega

- `src/lib/api.ts` — `fetch` em `NEXT_PUBLIC_API_URL` (default `http://localhost:8080`)
- `src/lib/auth.ts` — JWT em `sessionStorage` (`bbf.access_token`); 401 limpa token e dispara `bbf:unauthorized`
- `src/domain/` — interfaces extraídas de `mocks.ts` (sem seed)
- `mocks.ts` — seed visual mantido; reexporta tipos de `@/domain`
- `.env.local.example` — só `NEXT_PUBLIC_API_URL`. Sem chave KAAS
- CORS `http://localhost:3000` na API (browser → 8080)
- Rewrite Next `/health/*` → API (probe no address bar do dev)
- `ApiHealthProbe` em layout (dev, sem UI). Sidebar e telas intactos

## Aceite

| Check | Resultado |
|---|---|
| `GET /health/live` (API 8080) | 200 anônimo |
| `GET /health/live` via Next rewrite (`localhost:3000`) | 200 se API no ar |
| Telas / Sidebar | inalterados |
| `mocks.ts` | seed permanece |
| Vite / MUI / Redux | não reescritos |

## Fora de escopo (OS)

Migrar telas para REST. Apagar `mocks.ts`. Reescrever stack. Chave KAAS no front.
