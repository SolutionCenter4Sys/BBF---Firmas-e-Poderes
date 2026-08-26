# WF-03 — Mapa de paridade API

**Workflow:** WF-03/22 — Matriz de paridade API  
**Agente:** Escritor Back (`Agents UpStream/the-writer-back.md`)  
**Branch pretendida:** `wf-03-matriz-api`  
**Escopo:** contrato apenas. Sem C#, React, Docker.  
**Data:** 2026-08-26

**Fontes de verdade (nesta ordem):**

| Prioridade | Arquivo | O que extrai |
|---|---|---|
| 1 | `src/lib/mocks.ts` | `openApiEndpoints` (7 rotas `/v1/*`), tipos `Document`, `DecisionRecord`, `AuditEvent`, `roles`, `DocStatus` |
| 2 | `src/app/api/kas/run/route.ts` | POST interno ingest (proxy KAAS) |
| 3 | `src/app/api/kas/return/route.ts` | POST interno result (proxy KAAS) |
| 4 | `src/app/envio/page.tsx` | fluxo UI arquivo → ingest → JSON → result; limite 50 MB; MIME |
| 5 | `src/lib/kas-ids.ts`, `src/lib/kas-client.ts` | `correlationId` (`corr_${uuid}`), jornada `testes-firmas-e-poderes`, header `X-Flow-Api-Key` |
| 6 | `src/app/docs-api/page.tsx` | base URL, auth OAuth2 + mTLS |

**Paridade hoje:** .NET expõe só `/health/live` e `/health/ready`. As 7 rotas `/v1/*` existem no mock OpenAPI; ainda não existem no backend. Os POST KAAS existem no BFF Next.js (`/api/kas/*`) e **não** substituem `POST /v1/documents`.

---

## 1. Convenções globais

| Item | Contrato |
|---|---|
| Base pública | `https://api.bbf.bradesco.com.br/v1` |
| Auth pública | OAuth2 Bearer + mTLS |
| Auth interna KAAS | header `X-Flow-Api-Key` (server-side, `KAS_API_KEY`) |
| Content-Type JSON | `application/json` |
| Content-Type upload | `multipart/form-data` |
| Versionamento | path `/v1/` |

Envelope de erro **proposto** (mock não define schema; Escritor Back exige consistência):

```json
{
  "timestamp": "2026-04-29T14:25:32Z",
  "status": 400,
  "error": "Bad Request",
  "message": "Parâmetros inválidos",
  "path": "/v1/authority/decision",
  "correlationId": "corr_a1b2c3",
  "details": []
}
```

---

## 2. `correlationId` e `Idempotency-Key`

### 2.1 `correlationId` / `X-Correlation-Id`

Amarra upload → pipeline → decisão → auditoria WORM. Sem ele, trilha não agrupa.

| Superfície | Nome | Onde | Obrigatório | Formato | Fonte |
|---|---|---|---|---|---|
| API pública `/v1/*` | `X-Correlation-Id` | header | não (gerar no servidor se ausente) | `corr_<uuid>` | `openApiEndpoints` |
| Trilha | `correlationId` | query `GET /v1/audit/trail` | não (filtro) | string | `openApiEndpoints` + `AuditEvent` |
| Entidade auditoria | `correlationId` | campo de `AuditEvent` | sim no evento | string | `mocks.ts` (`audit[]`) |
| BFF KAAS ingest | `correlationId` | `FormData` **e** `payload.correlationId` | não no form (BFF gera); sim no payload enviado ao KAAS | `corr_${crypto.randomUUID()}` | `kas/run/route.ts`, `kas-ids.ts` |
| BFF KAAS result | `correlationId` | JSON body | **sim** (400 se vazio) | string trim | `kas/return/route.ts` |
| UI envio | `correlationId` | estado + POST `/api/kas/run` | gerado antes do fetch | `newCorrelationId()` | `envio/page.tsx` |

Regra de geração (`src/lib/kas-ids.ts`):

```text
corr_${crypto.randomUUID()}
```

Exemplo mock: `corr_a1b2c3` liga 7 eventos de `doc_001` (`document.uploaded` → `audit.persisted`).

**Propagação obrigatória:**

1. Cliente envia `X-Correlation-Id` **ou** servidor gera `corr_<uuid>`.
2. Mesmo valor no `AuditEvent.correlationId` de todo o ciclo de vida do documento.
3. KAAS ingest e KAAS result usam **o mesmo** `correlationId` (jornada `testes-firmas-e-poderes`).
4. Toda resposta de erro inclui `correlationId` no envelope.

Não confundir com `executionId` (id da execução KAAS: `executionId` / `execution_id` / `runId` / `run_id`). `executionId` é opcional no result; `correlationId` não é.

### 2.2 `Idempotency-Key`

| Superfície | Header | Obrigatório | Janela | Fonte |
|---|---|---|---|---|
| `GET /v1/authority/decision` | `Idempotency-Key` | não | 24 h | `openApiEndpoints` |
| `POST /v1/documents` | **não está no mock** | — | — | lacuna (ver §5) |
| `POST /v1/decision/evaluate` | **não está no mock** | — | — | lacuna |
| `POST /v1/decision/{decisionId}/replay` | **não está no mock** | — | — | replay já é determinístico por `decisionId` |
| BFF `/api/kas/*` | **não existe** | — | — | `kas/run`, `kas/return` |

Semântica (contrato alvo da rota de decisão):

- Mesma chave + mesmo consumidor + mesma operação, dentro de 24 h → **mesma resposta** (mesmo `decisionId` se já persistido).
- Chave nova → nova avaliação.
- Header ausente → trata como chamada nova (sem replay de cache de idempotência).

**Lacuna de paridade:** `Idempotency-Key` no mock aparece só no GET de decisão. `POST /v1/documents` (202 assíncrono) é o caso clássico de idempotência de ingestão. Contrato alvo **recomendado** (não implementado no mock): header `Idempotency-Key` obrigatório no POST de documentos, janela 24 h, replay do mesmo `{ documentId, status, uploadedAt }`. Sem isso, retry do cliente cria documento duplicado.

---

## 3. Matriz (visão única)

Colunas pedidas: método, rota, payload in/out, status, perfil RBAC, regra, arquivo de origem.

| # | Método | Rota | Payload in | Payload out (sucesso) | Status | Perfil RBAC | Regra | Origem |
|---|---|---|---|---|---|---|---|---|
| 1 | GET | `/v1/authority/decision` | query: `cnpj`, `operation`, `signers`; headers: `X-Correlation-Id?`, `Idempotency-Key?` | `{ decisionId, status, motivos, evidencias }` | **200**; 400; 401; 403; 429; 503 | `consumer` (`api:decision:read`); `analista` (`decision:read`) | RN01–RN04, TH01; fallback MANUAL se fonte oficial down | `mocks.ts` `openApiEndpoints[0]` |
| 2 | POST | `/v1/documents` | `multipart/form-data` campo `file`; header `X-Correlation-Id?` | `{ documentId, status: "pendente", uploadedAt }` | **202 Accepted** (não 200); 401; 403; 413; 415; 422 | `analista` (`documents:upload`); consumer com `api:documents:upload` | aceita e **enfileira**; não processa síncrono; PDF/imagem ≤ 50 MB | `mocks.ts` `openApiEndpoints[1]` + `envio/page.tsx` (MIME/tamanho) |
| 3 | GET | `/v1/documents/{documentId}/status` | path `documentId`; header `X-Correlation-Id?` | `{ documentId, status: DocStatus, fileName?, confianca?, uploadedAt? }` | **200**; 401; 403; 404 | `analista` (`documents:read`); consumer com `api:documents:read` | máquina de estados `DocStatus`; cliente faz poll após 202 | `mocks.ts` `openApiEndpoints[2]` + tipo `Document` |
| 4 | POST | `/v1/decision/evaluate` | JSON `EvaluationRequest` | `DecisionRecord` completo | **200**; 400; 401; 403 | `analista` (`decision:read`) — UI interna | RN01–RN04, TH01; grava `versions` (rules, canonical, aiPrompt, aiModel) | `mocks.ts` `openApiEndpoints[3]` + tipo `DecisionRecord` |
| 5 | POST | `/v1/decision/{decisionId}/replay` | path `decisionId` | `DecisionRecord` **idêntico** ao original | **200**; 401; 403; 404 | `auditor` (`decision:replay`); consumer `auditoria-corp` (`api:decision:replay`) | replay determinístico do snapshot; sem reconsulta oficial | `mocks.ts` `openApiEndpoints[4]` |
| 6 | GET | `/v1/audit/trail` | query: `documentId?`, `correlationId?`, `from?`, `to?` | `AuditEvent[]` agrupado por `correlationId` | **200**; 401; **403**; 400 | **`auditor`** (`audit:read`). Mock: “restrito ao perfil auditor”. `dpo` também tem `audit:read` no catálogo de roles — conflito, ver §7 | trilha imutável WORM; PII mascarado | `mocks.ts` `openApiEndpoints[5]` + tipo `AuditEvent` |
| 7 | GET | `/v1/verification/health` | — | `SourceHealth[]` | **200**; 401; 403 | `analista` (`*:read` via admin); `admin` | circuit breaker; 503 nas fontes **não** derruba este GET | `mocks.ts` `openApiEndpoints[6]` + `sourceHealth` |
| 8 | POST | `/api/kas/run` *(interno BFF)* | `multipart/form-data`: `file`, `correlationId?` → KAAS `{ document_url, mode:"sync", payload:{ action:"ingest", correlationId } }` | `{ ok, kasStatus, durationMs, fileName, journey, mode, action:"ingest", correlationId, executionId, body }` | **200** se KAAS ok; 400; 500 (`KAS_API_KEY`); 502 | sessão UI (analista); **não** OAuth `/v1`. Segredo: `X-Flow-Api-Key` | **não é** `POST /v1/documents`. Sync KAAS. Não usar 200 como contrato público de upload | `src/app/api/kas/run/route.ts` |
| 9 | POST | `/api/kas/return` *(interno BFF)* | JSON `{ correlationId, result, executionId?, fileName? }` → KAAS `{ mode:"sync", payload:{ action:"result", correlationId, fileName, result, executionId? } }` | `{ ok, kasStatus, durationMs, journey, mode, action:"result", correlationId, executionId, body }` | **200** se KAAS ok; 400; 500; 502 | sessão UI (analista); `X-Flow-Api-Key` | mesmo `correlationId` do ingest; jornada deve ramificar em `payload.action === "result"` | `src/app/api/kas/return/route.ts` + `envio/page.tsx` |

Rotas 1–7 = contrato público `/v1`. Rotas 8–9 = BFF interno. **Não misturar.**

---

## 4. Contratos detalhados — 7 rotas `/v1/*`

Auth comum: `Authorization: Bearer {access_token}` + mTLS.

### 4.1 GET `/v1/authority/decision`

Jornada consumidora (Onboarding PJ, Crédito PJ, Garantias, …).

**In**

| Nome | Em | Tipo | Req | Descrição |
|---|---|---|---|---|
| `cnpj` | query | string | sim | `00.000.000/0000-00` |
| `operation` | query | string | sim | código do catálogo (`movimentacao_financeira`, `contratacao_credito`, `abertura_conta`, …) |
| `signers` | query | string | sim | IDs de signatários separados por vírgula |
| `X-Correlation-Id` | header | string | não | se ausente, gerar `corr_<uuid>` |
| `Idempotency-Key` | header | string | não | janela 24 h |

**Out 200** (exemplo do mock; tipo rico = `DecisionRecord`)

```json
{
  "decisionId": "dec_001",
  "status": "APROVADO",
  "motivos": [
    "Operação dentro do limite de R$ 500.000 com modo conjunto Diretor + Procurador (Regra RN02 + RN03 v1.2.0)."
  ],
  "evidencias": ["..."]
}
```

`DecisionStatus`: `APROVADO` | `REPROVADO` | `MANUAL`.

Schema alvo alinhado a `DecisionRecord` (campos extras da UI interna podem ir em 4.4; consumidor público pode receber o recorte acima **mais** `cnpj`, `operacao`, `evaluatedAt`, `versions`):

```json
{
  "decisionId": "string",
  "documentId": "string",
  "cnpj": "string",
  "operacao": "string",
  "signatariosSolicitados": ["string"],
  "status": "APROVADO | REPROVADO | MANUAL",
  "motivos": ["string"],
  "evidencias": [
    {
      "type": "documento | fonte_oficial",
      "trace": { "page": 0, "offsetStart": 0, "offsetEnd": 0, "snippet": "string" },
      "fonte": "string",
      "detalhe": "string"
    }
  ],
  "versions": {
    "rules": "string",
    "canonical": "string",
    "aiPrompt": "string",
    "aiModel": "string"
  },
  "evaluatedAt": "ISO-8601",
  "latencyMs": 0
}
```

| Status | Quando |
|---|---|
| 200 | decisão avaliada |
| 400 | query inválida (CNPJ, operation, signers) |
| 401 | token OAuth2 inválido/ausente |
| 403 | escopo insuficiente (`api:decision:read`) |
| 429 | rate limit do consumer (`rateLimitPerMin` em `apiConsumers`) |
| 503 | fonte oficial indisponível — **pode** devolver decisão `MANUAL` como fallback (descrição do mock) em vez de 503 puro |

**RBAC:** `consumer`, `analista`.  
**Regras:** RN01 (sócio ATIVO), RN02 (modo assinatura), RN03 (limite), RN04 (procuração vigente), TH01 (threshold → MANUAL).

---

### 4.2 POST `/v1/documents` — **202, não 200 síncrono**

Upload societário. Inicia pipeline **assíncrono**.

**In**

| Nome | Em | Tipo | Req | Descrição |
|---|---|---|---|---|
| `file` | body (multipart) | file | sim | PDF ou imagem; até 50 MB |
| `X-Correlation-Id` | header | string | não | correlação do ciclo |

MIME aceitos (UI `envio/page.tsx`, replicar no contrato público):

- `application/pdf`
- `image/png`, `image/jpeg`, `image/webp`, `image/tiff`
- fallback por extensão: `.pdf`, `.png`, `.jpg`, `.jpeg`, `.webp`, `.tif`, `.tiff`

Teto: `MAX_UPLOAD_BYTES = 50 * 1024 * 1024`.

**Out 202 Accepted** (exemplo do mock)

```json
{
  "documentId": "doc_xxx",
  "status": "pendente",
  "uploadedAt": "2026-04-30T14:22:00Z"
}
```

Header sugerido: `Location: /v1/documents/{documentId}/status`.

**Proibido neste contrato:**

- HTTP **200** com documento já processado (OCR/NER/canônico/decisão).
- `mode: "sync"` como contrato público (isso é só o BFF KAAS, §5).
- Devolver `DecisionRecord` neste POST.

Cliente **poll** `GET /v1/documents/{documentId}/status` até `canonico_pronto` | `decidido` | `revisao_humana` | `falha`.

| Status | Quando |
|---|---|
| **202** | aceito e enfileirado (`status: pendente`) |
| 401 | sem token |
| 403 | sem `documents:upload` / `api:documents:upload` |
| 413 | > 50 MB (UI já bloqueia; API deve espelhar) |
| 415 | tipo não suportado |
| 422 | antivírus, páginas, arquivo corrompido |

**RBAC:** `analista`; consumer `onboarding-pj` (`api:documents:upload`).  
**Regra:** persistir raw + hash + `correlationId`; não esperar KAAS/OCR para responder.

---

### 4.3 GET `/v1/documents/{documentId}/status`

**In:** path `documentId` (string).  
**Out 200** derivado de `Document` + `DocStatus`:

```json
{
  "documentId": "doc_001",
  "fileName": "contrato-social-acme.pdf",
  "status": "decidido",
  "uploadedAt": "2026-04-29T14:22:00Z",
  "paginas": 12,
  "hash": "a3f2c4...e9d1",
  "confianca": { "ocr": 0.97, "iagen": 0.92, "ner": 0.94 }
}
```

`DocStatus` (fonte `mocks.ts`):

`pendente` → `processando_ocr` → `processando_iagen` → `processando_ner` → `canonico_pronto` → `validacao_oficial` → `decidido` | `revisao_humana` | `falha`

| Status | Quando |
|---|---|
| 200 | encontrado |
| 404 | `documentId` inexistente |
| 401 / 403 | auth/RBAC |

**RBAC:** `documents:read` / `api:documents:read`.

---

### 4.4 POST `/v1/decision/evaluate`

Uso **interno** da UI. Mock nomeia o body `EvaluationRequest` sem interface TS. Contrato derivado de `DecisionRecord` + contexto de operação:

**In**

```json
{
  "documentId": "doc_001",
  "cnpj": "12.345.678/0001-90",
  "operacao": "movimentacao_financeira",
  "valorOperacao": 500000,
  "currency": "BRL",
  "signatariosSolicitados": ["João da Silva (Diretor)", "Maria Souza (Procuradora)"],
  "asOf": "2026-04-29T14:25:00Z"
}
```

**Out 200:** `DecisionRecord` completo (ver schema em 4.1, todos os campos).  
**RBAC:** `analista` (`decision:read`). Não expor a consumers externos (eles usam 4.1).  
**Regra:** persistir snapshot `versions` para replay.

---

### 4.5 POST `/v1/decision/{decisionId}/replay`

**In:** path `decisionId`. Body vazio.  
**Out 200:** mesmo `DecisionRecord` da avaliação original (mesmos `motivos`, `evidencias`, `versions`, `status`).  
**404** se decisão não existe.  
**RBAC:** `auditor` (`decision:replay`). Consumer `auditoria-corp` (`api:decision:replay`).  
**Regra:** usar snapshot; **não** reconsultar Junta; resposta byte-a-byte equivalente ao original (determinismo).

---

### 4.6 GET `/v1/audit/trail`

**In**

| Nome | Em | Tipo | Req |
|---|---|---|---|
| `documentId` | query | string | não |
| `correlationId` | query | string | não |
| `from` | query | ISO-8601 | não |
| `to` | query | ISO-8601 | não |

**Out 200:** array de `AuditEvent`:

```json
{
  "eventId": "ev_0001",
  "correlationId": "corr_a1b2c3",
  "documentId": "doc_001",
  "decisionId": "dec_001",
  "type": "document.uploaded",
  "actor": "ana.silva@bbf.com.br",
  "timestamp": "2026-04-29T14:22:00Z",
  "details": "Upload de contrato-social-acme.pdf (12 páginas, 2.3MB)"
}
```

Tipos mockados: `document.uploaded`, `ocr.completed`, `iagen.completed`, `canonical.ready`, `official.queried`, `decision.evaluated`, `audit.persisted`.

| Status | Quando |
|---|---|
| 200 | trilha |
| 403 | sem perfil auditor (texto explícito do mock) |
| 401 | sem token |

**RBAC:** mock diz **auditor**. Catálogo `roles` também dá `audit:read` a `dpo`. Paridade: implementar **auditor** como gate da rota pública; DPO usa console LGPD, não esta API, até decisão explícita.

---

### 4.7 GET `/v1/verification/health`

**Out 200:** `SourceHealth[]` (`sourceHealth` em `mocks.ts`):

```json
{
  "sourceId": "junta-sp",
  "nome": "Junta Comercial SP",
  "status": "operacional",
  "uptime24h": 0.998,
  "latenciaP95Ms": 1240,
  "errorRate": 0.002,
  "cacheHitRate": 0.42,
  "ultimaConsulta": "2026-04-29T14:24:55Z",
  "circuitBreaker": "fechado",
  "observacao": null
}
```

`status`: `operacional` | `degradado` | `indisponivel`.  
`circuitBreaker`: `fechado` | `meio-aberto` | `aberto`.  
**RBAC:** `admin` / `analista` (não está no mock; não é rota de consumer).

---

## 5. POST interno KAAS — ingest / result

Não são `/v1`. Proxy Next.js → jornada `testes-firmas-e-poderes`.

URL default (`kas-ids.ts`):

```text
https://kaas-core-dev.up.railway.app/kas/triggers/journeys/testes-firmas-e-poderes/run
```

Override: `KAS_RUN_URL`. Auth: `X-Flow-Api-Key: {KAS_API_KEY}`.

### 5.1 POST `/api/kas/run` — `action: ingest`

**In (browser → BFF):** `multipart/form-data`

| Campo | Req | Notas |
|---|---|---|
| `file` | sim | `File` size > 0 senão 400 `"Nenhum arquivo enviado."` |
| `correlationId` | não | se vazio, BFF chama `newCorrelationId()` |

**In (BFF → KAAS):**

```json
{
  "document_url": "data:application/pdf;base64,...",
  "mode": "sync",
  "payload": {
    "action": "ingest",
    "correlationId": "corr_<uuid>"
  }
}
```

**Out BFF (200 se `kas.ok`):**

```json
{
  "ok": true,
  "kasStatus": 200,
  "kasStatusText": "OK",
  "durationMs": 0,
  "fileName": "contrato.pdf",
  "fileSize": 0,
  "fileType": "application/pdf",
  "journey": "testes-firmas-e-poderes",
  "mode": "sync",
  "action": "ingest",
  "correlationId": "corr_<uuid>",
  "executionId": "<string ou null>",
  "body": {}
}
```

| Status BFF | Quando |
|---|---|
| 200 | KAAS ok |
| 400 | sem arquivo |
| 500 | `KAS_API_KEY` ausente |
| 502 | rede/KAAS não-ok |

`maxDuration = 300` s. Runtime `nodejs`.

### 5.2 POST `/api/kas/return` — `action: result`

**In (browser → BFF):**

```json
{
  "correlationId": "corr_<uuid>",
  "executionId": "<opcional>",
  "fileName": "contrato.pdf",
  "result": { }
}
```

Validações: JSON inválido → 400; `correlationId` vazio → 400 `"correlationId obrigatório."`; `result` undefined → 400 `"result obrigatório."`.

**In (BFF → KAAS):**

```json
{
  "mode": "sync",
  "payload": {
    "action": "result",
    "correlationId": "corr_<uuid>",
    "fileName": "contrato.pdf",
    "result": {},
    "executionId": "<omitir se null>"
  }
}
```

**Regra de jornada (UI `envio/page.tsx`):** ramificar em `payload.action === "result"`. Sem isso, o segundo `/run` tenta processar arquivo de novo.

Mesmos 200 / 500 / 502 do ingest.

### 5.3 Por que 202 no `/v1/documents` e 200 no KAAS

| | Público `POST /v1/documents` | BFF `POST /api/kas/run` |
|---|---|---|
| HTTP sucesso | **202** | **200** |
| Processamento | assíncrono (fila) | `mode: "sync"` espera JSON KAAS |
| Cliente | poll `/status` | recebe `body` na mesma request |
| Papel | contrato de produto | laboratório / ingestão KAAS |

Implementar `/v1/documents` como 200 síncrono **quebra** o mock OpenAPI e o pipeline (`DocStatus`). O 200 do BFF **não** é precedente do contrato público.

---

## 6. Tipos de domínio (payload canônico)

Fonte: `src/lib/mocks.ts`.

**Document** — recurso após o pipeline (não é o body do 202):

- ids: `documentId`, `fileName`, `cnpj`, `razaoSocial`
- `tipoSocietario`: `LTDA` | `S.A.` | `EIRELI`
- `uploadedAt`, `uploadedBy`, `hash`, `paginas`
- `status`: `DocStatus`
- `confianca`: `{ ocr, iagen, ner }` (0–1)
- `socios`: `Person[]` — `personId`, `nome`, `cpf`, `qualificacao`, `cargo`, `status: ativo|inativo`
- `poderes`: `Power[]` — `powerId`, `pessoa`, `operacao`, `limite { currency:"BRL", value, expression }`, `modoAssinatura { tipo: isolada|conjunta, n?, m?, qualificacoes? }`, `vigencia { validFrom, validTo? }`, `sourceTrace { page, offsetStart, offsetEnd, snippet }`

**DecisionRecord** — ver §4.1.  
**AuditEvent** — ver §4.6.

---

## 7. RBAC (catálogo `roles`)

| `roleId` | Permissões relevantes às rotas `/v1` |
|---|---|
| `analista` | `documents:read`, `documents:upload`, `decision:read` |
| `auditor` | `audit:read`, `decision:replay`, `documents:read` |
| `dpo` | `audit:read` (não usar em `/v1/audit/trail` até resolver conflito) |
| `consumer` | `api:decision:read` |
| `admin` | `*:read` + escrita de config |

Scopes de consumers (`apiConsumers`): `api:decision:read`, `api:documents:upload`, `api:documents:read`, `api:audit:read`, `api:decision:replay`.

401 = sem token. 403 = token sem papel/escopo da linha da matriz.

---

## 8. Regras de negócio ligadas à API

| ID | Aplica em | HTTP se violar | Fonte |
|---|---|---|---|
| RN01 | decisão | 200 `REPROVADO` (não 4xx) | `dmnRules` |
| RN02 | decisão | 200 `REPROVADO` | `dmnRules` |
| RN03 | decisão | 200 `REPROVADO` | `dmnRules` |
| RN04 | decisão | 200 `REPROVADO` | `dmnRules` |
| TH01 | decisão | 200 `MANUAL` | `dmnRules` |
| Upload MIME | `POST /v1/documents` | 415 | `envio/page.tsx` |
| Upload 50 MB | `POST /v1/documents` | 413 | `envio/page.tsx` |
| Arquivo inválido | `POST /v1/documents` | 422 | `openApiEndpoints` |
| Fonte oficial down | `GET /v1/authority/decision` | 503 **ou** 200 `MANUAL` | mock description |
| Rate limit | decisão / upload | 429 | `openApiEndpoints` + `apiConsumers` |
| Replay snapshot | `POST .../replay` | 200 idêntico / 404 | mock |
| Mesmo `correlationId` ingest→result | KAAS interno | 400 se ausente no result | `kas/return/route.ts` |

Decisão de negócio (`APROVADO`/`REPROVADO`/`MANUAL`) **não** é 4xx. 4xx = contrato/auth/validação de input.

---

## 9. Lacunas de paridade (contrato, não código)

1. `.NET` não tem as 7 rotas `/v1/*`. Só health.
2. `POST /v1/documents` no mock = **202**. BFF KAAS = **200 sync**. Não copiar o 200 para o público.
3. `Idempotency-Key` só no GET de decisão. Falta no POST de documentos (risco de duplicar ingest).
4. Header público `X-Correlation-Id` vs body/form `correlationId` no BFF — mapear 1:1 na implementação `/v1`.
5. `EvaluationRequest` não existe como tipo TS — schema do §4.4 é derivação.
6. `GET /v1/audit/trail` “só auditor” vs `dpo.audit:read`.
7. Envelope de erro único: mock não define; usar §1.
8. 401/403 omitidos em várias linhas do mock (status, evaluate, replay, health) — incluir no contrato alvo.
9. KAAS `mode: "sync"` é detalhe do laboratório; pipeline de produto é assíncrono (`DocStatus`).

---

## 10. Fora de escopo (OS)

- Implementação C# / controllers / EF
- Alteração React / páginas
- Docker / compose / Postgres

Próximo workflow (fora desta OS): OpenAPI gerado a partir desta matriz + controllers `/v1`.

---

**Documento elaborado com agente Escritor Back (BMAD UpStream) — WF-03/22.**
