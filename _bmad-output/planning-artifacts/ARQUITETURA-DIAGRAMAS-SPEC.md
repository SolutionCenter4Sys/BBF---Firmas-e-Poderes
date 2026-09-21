# Especificação lógica — Diagramas de Arquitetura
## Plataforma BBF — Firmas e Poderes

| Campo | Valor |
|---|---|
| Agente | Winston (Arquiteto) — Etapa 1 |
| Destino | Apresentação (público misto técnico + executivo) |
| Data | 2026-08-28 |
| Fonte | Código do repositório + `compose.yaml` (WF-19) |
| Próximo passo | Validado 2026-08-28 → Agente Artista (3 HTML 16:9) |
| Validação | (1) regras separadas do KAAS (2) só 4 containers no D1 (3) rótulo KAAS (Foursys) (4) omitir admin/consumer |

**Instrução ao Artista (após validação):** fidelidade total a esta spec. Não adicionar, remover ou renomear nós. Não descer a classes. Paleta por categoria (ver seção Paleta).

---

## 0. Leitura do repositório (base factual)

### Topologia de execução (4 containers)

Rede Docker `bbf-internal` (`compose.yaml`). Volumes: `bbf_pgdata`, `bbf_docs`.

| Container | Imagem / build | Porta no host | Responsabilidade |
|---|---|---|---|
| `web` (`bbf-web`) | Next.js 14 standalone | `3002:3000` neste host | UI: upload, dashboard, filas, decisão, auditoria |
| `api` (`bbf-api`) | .NET 8 ASP.NET Core | `8080:8080` | HTTP público, JWT, persistência, decisão |
| `worker` (`bbf-worker`) | .NET 8 BackgroundService | health interno `:8081` (não publicado) | Consome outbox e chama KAAS |
| `db` (`bbf-db`) | PostgreSQL 16 | **sem bind no host** | Protocolo, status, canônico, decisões, outbox, trilha |

### Projetos .NET

```
backend/src/
  BbfFirmasPoderes.Api            → entrada do container api
  BbfFirmasPoderes.Worker         → entrada do container worker
  BbfFirmasPoderes.Domain         → entidades, regras, portas (sem I/O)
  BbfFirmasPoderes.Infrastructure → EF/Npgsql, blob, KAAS, serviços
```

A API **não** registra `IKasClient`. Só o Worker chama `AddKaasPipeline`.

### Front

Next.js 14 (`bbf-fp-frontend`). Browser chama a API em `NEXT_PUBLIC_API_URL` (`http://localhost:8080`). Rotas Next `/api/kas/*` estão **deprecadas** (HTTP 410) — KAAS não passa pelo frontend.

---

## 1. Classificação por responsabilidade

| Camada | Componentes |
|---|---|
| Apresentação | Next.js (`envio`, `documents`, `decision`, `audit`, `review-queue`, `manual-queue`, dashboard) |
| Aplicação (API) | Endpoints `/v1/documents`, `/v1/decision`, `/v1/authority/decision`, `/v1/audit/trail`, `/v1/auth/login` |
| Aplicação (Worker) | `OutboxWorker` → `OutboxKaasProcessor` |
| Domínio | Documento, Pessoa, Poder, Decisão, Outbox, KasRun, AuditEvent; `DecisionEngine` (RN01–RN04, TH01); portas `IKasClient`, `IDocumentBlobStore` |
| Dados | PostgreSQL 16 (tabelas de domínio) + volume `bbf_docs` (blob do arquivo, não bytea) |
| Integração externa | KAAS (jornada `testes-firmas-e-poderes`, header `X-Flow-Api-Key`) |

**Perfis JWT no código:** `operador`, `auditor`, `admin`, `consumer` (alias `analista` → `operador`). Diagramas desta spec usam só **Operador** e **Auditor**, como pedido.

---

## 2. Paleta obrigatória (Artista)

Consistência entre os 3 HTML. Texto sempre em navy (`#222239`) sobre fundos claros; texto branco só sobre navy. Laranja só como acento de destaque (fluxo ativo / Worker), nunca como fundo de bloco com texto branco.

| Categoria | Preenchimento | Borda | Texto |
|---|---|---|---|
| Atores humanos (Operador, Auditor) | `#FFE2A9` (baunilha) | `#222239` | `#222239` |
| Frontend Next.js | `#89BAB1` (menta) | `#222239` | `#222239` |
| Backend API .NET | `#2e2e4a` (navy-soft) | `#222239` | `#FFFFFF` |
| Worker .NET | `#FF5315` (laranja) | `#181828` | `#FFFFFF` |
| Dados PostgreSQL | `#222239` (navy) | `#181828` | `#FFFFFF` |
| Volume / arquivos | `#F7F6F2` (off-white) | `#555463` | `#222239` |
| Integração externa KAAS | `#181828` (navy-deep) | `#FF5315` | `#FFFFFF` |
| Domínio (só Diagrama 2) | `#FFFFFF` | `#222239` | `#222239` |
| Infraestrutura (só Diagrama 2) | `#E8E7E2` | `#222239` | `#222239` |

**Tipos de linha**

| Tipo | Traço | Uso |
|---|---|---|
| HTTP síncrono | contínuo, seta | Browser/Web → API; Worker → KAAS |
| Persistência | contínuo, seta | API/Worker → PostgreSQL ou volume |
| Poll / outbox | tracejado, seta | Worker lê fila (tabela outbox) |
| Auditoria / replay | pontilhado, seta | Auditor → trilha e replay |

Fundo da página: `#F7F6F2`. Título e rodapé em navy. Fonte sans-serif (Nunito ou equivalente embutida/system). Proporção 16:9.

**Destaque visual:** Worker + fluxo de processamento do documento maiores que nós periféricos.

---

## Diagrama 1 — Visão macro da plataforma

**Título:** BBF Firmas e Poderes — Visão macro da plataforma  
**Objetivo:** 4 containers, dois perfis humanos, KAAS. Sem classes.

### Agrupamentos

| Id | Nome | Contém |
|---|---|---|
| G1 | Atores | Operador, Auditor |
| G2 | Rede interna Docker (`bbf-internal`) | Frontend Web, API, Worker, PostgreSQL |
| G3 | Integração externa | KAAS (Foursys) |

### Nós (7 caixas + 2 sub-rótulos)

Validação 2026-08-28: **somente os 4 containers**. Volume `bbf_docs` não é nó.

| Id | Nome | Categoria | Descrição curta |
|---|---|---|---|
| N1 | Operador | ator | Upload, consulta de status/canônico, checagem de poderes |
| N2 | Auditor | ator | Trilha WORM e replay de decisão |
| N3 | Frontend Web | frontend | Next.js 14 — dashboard, envio, filas, decisão, auditoria |
| N4 | API | backend | .NET 8 — JWT, validação, orquestração HTTP, persistência |
| N5 | Worker | worker | .NET 8 — consome outbox e aciona KAAS |
| N6 | PostgreSQL | dados | Postgres 16 — protocolo, status, canônico, decisão, outbox, auditoria |
| N8 | KAAS (Foursys) | externo | Workflow Foursys — ingestão/análise do documento (sócios e poderes) |
| N9 | Login JWT | backend | Emissão/validação de token na API (`POST /v1/auth/login`) |
| N10 | Consumidor interno | frontend | Telas de filas e resultado — mesma UI, outro recorte de uso |

> **Nota de densidade:** N9 e N10 são o mesmo processo API/Web. No desenho, N9 fica **dentro** do agrupamento da API (caixa interna). N10 fica **dentro** do Frontend (rótulo “Filas / resultado”). Não são containers extras.

**Contagem efetiva de caixas de 1º nível:** 7 (N1–N6, N8). N9 e N10 são sub-rótulos.

### Conexões

| Id | Origem | Destino | Tipo | Rótulo |
|---|---|---|---|---|
| C1 | Operador | Frontend Web | HTTP síncrono | Usa a interface |
| C2 | Auditor | Frontend Web | HTTP síncrono | Usa a interface |
| C3 | Frontend Web | API | HTTP síncrono | HTTP + JWT |
| C4 | API | PostgreSQL | Persistência | EF Core / Npgsql |
| C6 | Worker | PostgreSQL | Poll / outbox | Lê outbox `document.uploaded` |
| C8 | Worker | KAAS (Foursys) | HTTP síncrono | POST jornada + `X-Flow-Api-Key` |
| C9 | KAAS (Foursys) | Worker | HTTP síncrono | Resposta sync (pessoas/poderes/status) |
| C10 | Worker | PostgreSQL | Persistência | Grava `kas_runs`, canônico, status, auditoria |

**Não desenhar:** seta API → KAAS (código: API não registra cliente KAAS).  
**Não desenhar:** broker (Rabbit/SQS) — outbox é tabela no Postgres.  
**Não desenhar:** volume `bbf_docs` (validação: só 4 containers).

---

## Diagrama 2 — Camadas do backend .NET

**Título:** BBF Firmas e Poderes — Camadas do backend (.NET 8)  
**Objetivo:** organização lógica API + Worker. Sem classes individuais.

### Agrupamentos

| Id | Nome | Contém |
|---|---|---|
| G1 | Host API | Endpoints HTTP, Auth JWT/RBAC |
| G2 | Host Worker | Outbox Worker |
| G3 | Domain | Modelo e regras, Portas |
| G4 | Infrastructure | Ingestão, Processador Outbox/KAAS, Serviço de decisão, Auditoria WORM, Cliente HTTP KAAS, Blob store, AppDbContext |
| G5 | Dados | PostgreSQL |
| G6 | Externo | KAAS (Foursys) |

### Nós (14)

| Id | Nome | Categoria | Descrição curta |
|---|---|---|---|
| N1 | Endpoints HTTP | backend | `/v1/documents`, `/v1/decision`, `/v1/authority/decision`, `/v1/audit/trail` |
| N2 | Auth JWT / RBAC | backend | Bearer HS256; perfis operador e auditor (admin/consumer omitidos no desenho) |
| N3 | Outbox Worker | worker | `BackgroundService` — poll periódico da outbox |
| N4 | Modelo e regras | domínio | Documento, pessoas, poderes, decisão; RN01–RN04 e TH01 |
| N5 | Portas | domínio | Contratos: cliente KAAS e armazenamento de arquivo |
| N6 | Ingestão de documentos | infra | Upload → valida MIME/tamanho → blob + registro + outbox; HTTP 202 |
| N7 | Processador Outbox/KAAS | infra | Consome mensagem, chama KAAS, mapeia canônico, avança status |
| N8 | Serviço de decisão | infra | Avalia poderes no canônico e persiste snapshot (APROVADO / REPROVADO / MANUAL) |
| N9 | Auditoria WORM | infra | Eventos append-only (interceptor recusa update/delete da trilha) |
| N10 | Cliente HTTP KAAS | infra | POST sync; chave só no Worker |
| N11 | Blob store | infra | Arquivo em `/data/docs` (volume compartilhado API+Worker) |
| N12 | AppDbContext | infra | Persistência EF Core → PostgreSQL |
| N13 | PostgreSQL | dados | Banco `bbf_firmas` |
| N14 | KAAS (Foursys) | externo | Jornada `testes-firmas-e-poderes` |

### Conexões

| Id | Origem | Destino | Tipo | Rótulo |
|---|---|---|---|---|
| C1 | Endpoints HTTP | Auth JWT / RBAC | HTTP síncrono | Exige Bearer |
| C2 | Endpoints HTTP | Ingestão de documentos | HTTP síncrono | POST /v1/documents |
| C3 | Endpoints HTTP | Serviço de decisão | HTTP síncrono | evaluate / authority / replay |
| C4 | Endpoints HTTP | Auditoria WORM | HTTP síncrono | GET /v1/audit/trail |
| C5 | Ingestão de documentos | Modelo e regras | Persistência | Cria documento `pendente` |
| C6 | Ingestão de documentos | Blob store | Persistência | Grava arquivo |
| C7 | Ingestão de documentos | AppDbContext | Persistência | Documento + outbox |
| C8 | Outbox Worker | Processador Outbox/KAAS | Poll / outbox | ProcessNext |
| C9 | Processador Outbox/KAAS | Portas | HTTP síncrono | Usa IKasClient / IDocumentBlobStore |
| C10 | Cliente HTTP KAAS | KAAS (Foursys) | HTTP síncrono | POST run |
| C11 | Processador Outbox/KAAS | AppDbContext | Persistência | kas_runs + canônico + status |
| C12 | Serviço de decisão | Modelo e regras | HTTP síncrono | DecisionEngine (sem I/O) |
| C13 | Serviço de decisão | AppDbContext | Persistência | Snapshot da decisão |
| C14 | AppDbContext | PostgreSQL | Persistência | Npgsql |
| C16 | AppDbContext | Auditoria WORM | Persistência | Interceptor append-only |

**Blob store (N11)** representa o arquivo em disco — não criar nó extra “volume” (D1 também não desenha o 5º container). C15 da spec original (blob → volume) **não se desenha**.

---

## Diagrama 3 — Fluxo end-to-end do documento

**Título:** BBF Firmas e Poderes — Fluxo do documento (upload → decisão → auditoria)  
**Objetivo:** jornada do operador até o auditor. Motor de decisão e Worker em destaque.

### Agrupamentos

| Id | Nome | Contém |
|---|---|---|
| G1 | Canal humano | Operador, Auditor, Interface Web |
| G2 | Plataforma BBF | API de ingestão, Persistência (Postgres + outbox + blob), Worker, Motor de decisão, Trilha WORM |
| G3 | Análise externa | KAAS (Foursys) |
| G4 | Resultado | Canônico, Decisão |

### Nós (14)

| Id | Nome | Categoria | Descrição curta |
|---|---|---|---|
| N1 | Operador | ator | Inicia o fluxo (login, upload, consulta, checagem) |
| N2 | Interface Web | frontend | Telas de envio, status, decisão e auditoria |
| N3 | API de ingestão | backend | `POST /v1/documents` → 202 + `documentId` / status `pendente` |
| N4 | Persistência | dados | Postgres (protocolo/outbox) + volume do arquivo |
| N5 | Worker | worker | Poll da outbox `document.uploaded` |
| N6 | KAAS (Foursys) | externo | Análise do documento (OCR/IA na jornada); devolve estrutura |
| N7 | Canônico | dados | Pessoas e poderes persistidos; status `canonico_pronto` ou `revisao_humana` |
| N8 | Consulta na tela | frontend | Poll `GET /v1/documents/{id}/status` e canônico |
| N9 | Checagem de poderes | backend | `POST /v1/decision/evaluate` ou `GET /v1/authority/decision` |
| N10 | Motor de decisão | domínio | RN01–RN04 + TH01 sobre o canônico (não reconsulta KAAS) |
| N11 | Decisão | dados | `APROVADO` / `REPROVADO` / `MANUAL` + snapshot de versões |
| N12 | Auditor | ator | Consulta trilha e replay |
| N13 | Trilha WORM | dados | `GET /v1/audit/trail` — eventos imutáveis |
| N14 | Replay | backend | `POST /v1/decision/{id}/replay` — devolve o snapshot persistido |

### Conexões (numeradas no desenho — ordem do fluxo)

| Id | Origem | Destino | Tipo | Rótulo |
|---|---|---|---|---|
| C1 | Operador | Interface Web | HTTP síncrono | 1. Login JWT + upload |
| C2 | Interface Web | API de ingestão | HTTP síncrono | 2. POST /v1/documents |
| C3 | API de ingestão | Persistência | Persistência | 3. Blob + registro + outbox |
| C4 | Worker | Persistência | Poll / outbox | 4. Consome outbox |
| C5 | Worker | KAAS (Foursys) | HTTP síncrono | 5. POST jornada (sync) |
| C6 | KAAS (Foursys) | Worker | HTTP síncrono | 6. Resultado da análise |
| C7 | Worker | Canônico | Persistência | 7. Pessoas, poderes, status |
| C8 | Interface Web | Consulta na tela | HTTP síncrono | 8. Poll de status |
| C9 | Consulta na tela | Canônico | HTTP síncrono | 8b. GET …/canonical |
| C10 | Operador | Checagem de poderes | HTTP síncrono | 9. Pedido de decisão |
| C11 | Checagem de poderes | Motor de decisão | HTTP síncrono | 10. Aplica regras |
| C12 | Motor de decisão | Decisão | Persistência | 11. APROVADO / REPROVADO / MANUAL |
| C13 | Auditor | Trilha WORM | Auditoria / replay | 12. GET /v1/audit/trail |
| C14 | Auditor | Replay | Auditoria / replay | 13. Replay do snapshot |

C10 passa pela Interface Web na prática (Operador → Web → API). No desenho, para não cruzar demais: **C10 sai da Interface Web para Checagem de poderes**, e o Operador permanece ligado à Web via C1. O rótulo de C10 fica “9. Checagem de poderes (evaluate / authority)”.

**Ajuste de conexão (canônico):** C10 origem = **Interface Web**, destino = **Checagem de poderes**. Operador não liga direto na API.

Tabela final C10:

| Id | Origem | Destino | Tipo | Rótulo |
|---|---|---|---|---|
| C10 | Interface Web | Checagem de poderes | HTTP síncrono | 9. evaluate / authority |

---

## 3. Integrações externas (inventário)

| Integração | Presente no código? | Nos diagramas? |
|---|---|---|
| **KAAS** (Foursys) | Sim. URL default Railway `kaas-core-dev…/kas/triggers/journeys/testes-firmas-e-poderes/run`. Header `X-Flow-Api-Key`. Timeout 300s. Sync. | Sim — único sistema externo desenhado |
| Junta Comercial / fontes oficiais | Stub (`GET /v1/verification/health`). Sem HTTP real. | **Não** |
| Broker de mensagens | Não. Outbox = tabela PostgreSQL | **Não** |
| IdP corporativo (SSO) | Não. Login demo JWT HS256 na própria API | **Não** (JWT é caixa interna da API) |
| Rotas Next `/api/kas` | Existem, retornam 410 Gone | **Não** |

---

## 4. Inferências e pontos para validação humana

Marcar no rodapé do HTML como “Validar” só se a liderança pedir. Nesta spec, o Artista **não** desenha badges de inferência — só usa os nós acima.

| # | Tema | O que o briefing assumia | O que o código faz | Recomendação |
|---|---|---|---|---|
| I1 | Quem decide Aprovado/Reprovado | KAAS devolve a decisão | KAAS estrutura sócios/poderes. A decisão sai do **motor de regras na API** (`DecisionEngine`). KAAS não estruturado → status de documento `revisao_humana` (não é `DecisionStatus`) | Manter a separação nos 3 diagramas |
| I2 | “Fila” | Fila externa | Tabela `OutboxMessages` no Postgres; Worker faz poll (~5s) | Chamar de outbox, não de fila MQ |
| I3 | KAAS = Kubernetes as a Service | Topologia K8s Foursys | Endpoint visível é **Railway** (`kaas-core-dev`). Jornada nomeada, não cluster | No rótulo usar **KAAS (Foursys)** — não desenhar Kubernetes |
| I4 | WORM | Storage imutável dedicado | Interceptor EF: recusa UPDATE/DELETE em `AuditEvent` | Rótulo “Trilha WORM (append-only)” |
| I5 | Replay | Reprocessa KAAS/regras | Lê o **snapshot persistido**; não reconsulta KAAS nem Junta | Rótulo “Replay do snapshot” |
| I6 | `/v1/authority/decision` | Só consulta | **Avalia** (pode persistir decisão) com `Idempotency-Key` e rate limit | Incluído em “Checagem de poderes” |
| I7 | Blob | Tudo no Postgres | Arquivo no volume `bbf_docs`; metadados no Postgres | Diagrama 1 tem o volume; D2 no Blob store |
| I8 | Perfis extra | Só operador e auditor | Também `admin` e `consumer` | Omitidos nos diagramas (macro) |
| I9 | Porta do web | :3000 | Compose deste host: **3002:3000** | Irrelevante no desenho |
| I10 | Chamada browser → API | Via BFF Next | Browser fala **direto** com `:8080` | Seta Web → API, não Web → Worker |

---

## 5. Critérios para o Artista (Etapa 2)

1. Um HTML autocontido por diagrama (CSS + SVG embutidos; sem CDN obrigatória — Nunito via Google Fonts é aceitável se fallback system-ui existir).
2. Viewport 16:9 (`1920×1080` lógico).
3. Paleta e tipos de linha da seção 2, iguais nos três arquivos.
4. Título no topo, legenda de cores + tipos de linha, rodapé `BBF Firmas e Poderes · 28/08/2026`.
5. Linhas ortogonais, setas de direção, rótulos legíveis em projeção.
6. Hierarquia: Worker (D1/D3) e Motor de decisão (D3) maiores.
7. Nomes dos nós **exatamente** como nesta spec.

### Arquivos de saída (após validação)

```
_bmad-output/planning-artifacts/diagramas/
  01-visao-macro.html
  02-camadas-backend.html
  03-fluxo-documento.html
```

---

## 6. Pausar aqui

Etapa 1 encerrada. Etapa 2 (Artista / HTML) **não executa** até validação explícita desta spec.
