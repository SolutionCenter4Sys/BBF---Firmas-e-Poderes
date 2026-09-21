# Architecture Review — BBF Firmas e Poderes

- **Agente:** Pierre (AR)
- **Data:** 2026-08-26
- **Escopo:** repo atual (Next.js mock + proxy KAAS) → alvo .NET 8 + React + PostgreSQL + Docker → AWS
- **Plano de execução (OS por chat):** `docs/plano-execucao-refatoracao.html`

## Stack atual (evidência)

| Camada | Realidade |
|---|---|
| UI | Next.js 14 App Router, 22 páginas, 4 componentes |
| Dados | `src/lib/mocks.ts` (~750 linhas) = domínio + OpenAPI + seed |
| API | só `/api/kas/run` e `/api/kas/return` (Node proxy) |
| Backend .NET | inexistente |
| PostgreSQL | inexistente |
| Testes | inexistentes |
| Specs/Sonar | inexistentes |

## C1 Arquitetura e estrutura — **3.5 / 10** (peso 15%)

Esperado (alvo React/Next + .NET Clean): `pages/` + `components/` + `hooks/` + `services/` + `domain/` no front; no back Domain → Application → Infrastructure → Api.

Encontrado:

- ❌ Sem camadas. Páginas importam mocks direto.
- ❌ Sem porta de persistência. Estado de upload/KAAS em `sessionStorage`.
- ❌ Next.js mistura UI e integração KAAS (`src/app/api/kas/*`).
- ⚠️ Contratos REST existem só como catálogo visual em `openApiEndpoints` — nenhum handler implementa `/v1/*`.
- ✅ Rotas de tela já mapeiam o produto (dashboard, documents, decision, audit, envio KAAS).

## C2 SOLID / padrões — **3.0 / 10** (peso 12%)

- ❌ SRP: `mocks.ts` é god object (entidades, seed, métricas, OpenAPI, schema canônico).
- ❌ DIP: UI depende de dados estáticos, não de interfaces.
- ❌ Pipeline OCR/decisão não existe como worker — README Step 12 nunca escrito.
- ⚠️ Adapter KAAS isolado (`kas-client.ts`) é o único trecho próximo de um port.

## Decisão de aprovação (só AR)

Score C1+C2 ponderado ≈ **3.3**. **BLOQUEADO** como produção. **Aprovado como PoC/MVP de UI.**

Refatoração correta = **reescrita do backend + desligar mocks no front**, não polish de `page.tsx`.

## Alvo (ratificar no plano HTML)

Monólito modular .NET 8 (Api + Worker + Domain + Infrastructure + Tests) + Next.js 14 (manter, já é React) + PostgreSQL 16 + compose local (db, api, worker, web). KAAS externo. AWS só depois do compose verde.

Próximo chat: copiar **WF-02** do HTML (Ricardo, walking skeleton).
