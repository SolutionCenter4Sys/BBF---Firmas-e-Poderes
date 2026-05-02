# BBF Firmas e Poderes — Frontend MVP (mock)

Frontend Next.js 14 (App Router) + React 18 + TypeScript com **dados mockados** para o MVP do Orquestrador de Firmas e Poderes.

> **Step 6 — Frontend MVP com Mock**, gerado pelos agentes `@the-artista` + `@dev-reactjs-esp`. Os mocks em `src/lib/mocks.ts` serão substituídos por chamadas REST reais no Step 12.

## Como rodar

```bash
cd frontend
npm install
npm run dev
# abrir http://localhost:3000
```

## Telas disponíveis

| Rota | Descrição | Estágio do Flow |
| ---- | --------- | --------------- |
| `/` | Dashboard: KPIs, upload, lista de documentos com status | Transversal |
| `/documents/[id]` | Detalhe: pipeline, sócios, poderes (canônico), trace de origem | 1, 2, 3, 4 |
| `/decision` | Decisão: banner, motivos, evidências, versões para replay | 4 |
| `/audit` | Trilha imutável agrupada por correlationId | Transversal |
| `/api-helper` | Playground do endpoint `GET /v1/authority/decision` + snippets cURL/JS/Java | 5 |

## Design System

Tokens importados de `docs/design-system/tokens.css` (espelhados em `src/app/globals.css`).

## Próximos passos (Step 7)

- Teste de usabilidade com `@the-critico` + `@the-arquiteta-estrategia-digital` + ajustes via `@dev-reactjs-esp`.
- Score-alvo ≥ 80 para liberar Step 8.
