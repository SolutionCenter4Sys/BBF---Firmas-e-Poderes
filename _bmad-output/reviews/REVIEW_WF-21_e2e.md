# PEER REVIEW REPORT — WF-21 Gate E2E (reabertura 27/08 tarde)

**Reviewer:** Pierre (Tech Lead — Peer Review Specialist)
**Data:** 27/08/2026 (reabertura tarde; ensaio manhã = PIVOT)
**Projeto:** BBF Firmas e Poderes
**Stack:** ASP.NET Core 8 + EF Core/Npgsql + PostgreSQL 16 + Worker KAAS + Next.js 14
**Escopo:** Gate ponta a ponta (upload → pipeline → decisão → audit) + scorecard HTML Go/Pivot/Kill
**Persona:** `Agents DownStream/tech-lead-peer-review.md` · comando **[PR] + roteiro**
**Config BMAD:** `_bmad/bmm/config.yaml` (user: KáritaMaia, PT-BR). `_bmad/cis/config.yaml` ausente neste repo.
**OS:** `docs/plano-execucao-refatoracao.html` § WF-21. **Não fazer terraform.** Sem Go = não abrir WF-22. **WF-22 não aberto neste chat.**
**Dependências:** WF-18 (`REVIEW_WF-18_security.md` + hardening no código), WF-19 (`compose.yaml`), WF-20 (seed + login README).

Rubrica Pierre: **C1–C10**. Decisão do **gate local** = tabela Go/Pivot/Kill do HTML (não o limiar de merge 8.0).

---

## SCORECARD HTML (gate local) — ensaio tarde

Fonte: `docs/plano-execucao-refatoracao.html` § “Scorecard de decisão (gate local)”.

| Critério | Go | Pivot | Kill | **Tarde 27/08** | Evidência |
|----------|----|-------|------|-----------------|-----------|
| Compose | 4 serviços healthy | db+api healthy; worker/web com workaround | Não sobe | **Go** | `docker compose ps`: `bbf-db`, `bbf-api`, `bbf-worker`, `bbf-web` **healthy**. `GET /health/ready` → **200** `Healthy`. `GET /health/live` → **200**. `HEAD /` UI `:3000` → **200**. |
| Upload | POST `/v1/documents` → 202 + linha no PG | Só Swagger | Ainda só sessionStorage | **Go** | `POST /v1/documents` (PDF mínimo, Bearer operador) → **202** `pendente`, `Location` + `documentId=doc_61e48d6b…`, `correlationId=corr_04d95ca0-…`. Linha no PG. UI `/` e `/envio` **200**. |
| KAAS | Ingest + JSON persistido + correlationId | JSON na API, tela mock | Chave no client | **Pivot** | Worker healthy. `/api/kas/run` e `/api/kas/return` → **410**. `src/` sem `KAS_API_KEY`. Seed ACME/Delta/Gama tem JSON+`corr_*` no PG. Upload live: 202 → worker → status **`falha`** (`Kas__ApiKey=` vazio no `.env.example`). **Não é Kill.** |
| Decisão | GET `/v1/authority/decision` com evidências | Seed estático no PG | Só mock na UI | **Go** | Live: `GET /v1/authority/decision?cnpj=12.345.678/0001-90&operation=movimentacao_financeira&signers=p1` → **200 APROVADO** (`doc_001`, `dec_419d834a…`). PG: `dec_001` APROVADO, `dec_002` REPROVADO, `dec_003` MANUAL. Replay `dec_001` auditor **200**. UI `/decision` e `/api-helper` **200**. |
| Audit | GET `/v1/audit/trail` por correlationId | Tabela existe, UI não lê | Sem trilha | **Go** | Live: `GET /v1/audit/trail?documentId=doc_001` auditor **200**; `?correlationId=corr_a1b2c3` **200** (`document.uploaded`). Operador no trail → **403**. Replay operador → **403**. UI `/audit` **200**. |
| AWS | Fora do gate. WF-22 documentado | — | Deploy sem compose verde | **Fora** | Este chat **não** abriu WF-22 e **não** fez terraform. |

### DECISÃO DO GATE: **PIVOT**

**Não é Go.** WF-22 **não abre**.

**Não é Kill de produto.** Compose verde, README 8/8 live, upload 202+PG, decisão/audit live, MJ-SEC-03/04 fechados no código, Swagger Production **401**.

Motivo que impede Go (basta um):

1. **KAAS live** não cumpre a coluna Go (ingest + JSON persistido a partir do upload). `Kas__ApiKey` vazio → documento novo vai a `falha`. Demo **seed** não depende disto; o critério HTML KAAS sim.

Bloqueadores da **manhã** (Docker/WSL 500 + MJ-SEC-03/04 abertos + Swagger anónimo em Production) **estão fechados** neste ensaio.

---

## Roteiro README (8 passos) — execução tarde (live)

Fonte: `README.md` § “Roteiro da demo”. **Não** se fez `compose down -v` neste chat (stack já up; OS = `docker compose ps` + ready 200).

| # | Passo | Live (compose/API) | Resultado |
|---|--------|--------------------|-----------|
| 1 | Stack healthy; `/health/ready` 200 | **PASS** | 4 healthy; ready **200** `Healthy` |
| 2 | `POST /v1/auth/login` operador; JWT | **PASS** | **200** `role=operador` `ana.silva@bbf.com.br` (tokenLen=377). Auditor **200** `role=auditor` (tokenLen=392) |
| 3 | Dashboard ACME + Delta + Gama | **PASS** | `GET /v1/documents` **200**. PG: `doc_001` ACME `decidido`, `doc_004` Delta `decidido`, `doc_003` Gama `revisao_humana`. `GET ?status=revisao_humana` = Gama |
| 4 | APROVADO `doc_001` | **PASS** | status **200** `decidido` `corr_a1b2c3`. Canonical **200**: João da Silva + Maria Souza (CPF mascarado). Replay `dec_001` **200 APROVADO** |
| 5 | REPROVADO `doc_004` Delta | **PASS** | status **200** `decidido`. Canonical: Carlos Pereira **`inativo`**. PG `dec_002` **REPROVADO** |
| 6 | MANUAL `doc_003` Gama / filas | **PASS** | status **200** `revisao_humana`, NER **0.66**. UI `/manual-queue` e `/review-queue` **200**. PG `dec_003` **MANUAL** |
| 7 | `GET /v1/authority/decision?...signers=p1` → APROVADO | **PASS** | **200** `status=APROVADO` `documentId=doc_001` |
| 8 | Audit auditor + replay; operador 403 | **PASS** | trail **200**; replay auditor **200**; operador trail **403**; operador replay **403** |

**Upload (OS passo 1, extra ao README 8):** PDF `%PDF-1.4` → **202** `pendente` → PG `doc_61e48d6b…` → status final **`falha`** (sem chave KAAS). Igual `doc_b1fc6405…` de ensaio anterior.

**Swagger Production:** `/swagger`, `/swagger/index.html`, `/swagger/v1/swagger.json` → **401**.

**Surrogate testes:** `dotnet` **fora do PATH** neste ensaio tarde (não reexecutei a suite). Inventário `[Fact]`: **95** métodos em `backend/tests/BbfFirmasPoderes.Tests`. Manhã: 87 verdes. Delta esperado = jail blob (2) + magic MIME (5) + ~1. Plano HTML cita 95 pós-hardening.

---

## SCORECARD PIERRE (C1–C10)

Pesos oficiais. Recorte = prontidão E2E + hardening WF-18 visível no código.

| # | Categoria | Score manhã | **Score tarde** | Peso | Status | Findings (tarde) |
|---|-----------|-------------|-----------------|------|--------|------------------|
| C1 | Arquitetura & Estrutura | 7.6 | **7.8/10** | 15% | OK | Compose 4 healthy verificado. Application layer ainda ausente |
| C2 | S.O.L.I.D / Patterns | 7.0 | **7.0/10** | 12% | OK com ressalvas | Sem mudança de recorte; outbox ainda orquestra ingest+result |
| C3 | Clean Code | 7.5 | **7.5/10** | 10% | OK com ressalvas | Helper `Problem` ainda copiado |
| C4 | Segurança (OWASP) | 6.6 | **7.4/10** | 15% | Gate C4 ≥ 6; **não autoriza Go** | MJ-SEC-03/04/05 (Swagger) **fechados**. Restam senha no JSON base + headers HSTS/CSP |
| C5 | Testes & Cobertura | 7.4 | **7.6/10** | 12% | OK; E2E live seed verde | 95 `[Fact]`; live 8/8; KAAS live falha; `dotnet` PATH ausente tarde |
| C6 | Error Handling | 7.5 | **7.5/10** | 8% | OK com ressalvas | Worker sobe sem chave; ingest live → `falha` |
| C7 | Performance | 6.5 | **6.5/10** | 8% | OK com ressalvas | Lista sem paginação; blob 50 MB |
| C8 | Documentação | 8.4 | **8.6/10** | 5% | OK | README 8 passos **reproduzidos live** |
| C9 | SonarQube Compliance | 6.5 | **6.5/10** | 10% | Sem QG no CI | Único workflow = Azure Static Web Apps |
| C10 | Stack Best Practices | 8.0 | **8.0/10** | 5% | OK | EF+PG; Next `standalone`; Swagger só Development |

### SCORE FINAL TARDE: **7.4 / 10**

Cálculo: `7.8×0.15 + 7.0×0.12 + 7.5×0.10 + 7.4×0.15 + 7.6×0.12 + 7.5×0.08 + 6.5×0.08 + 8.6×0.05 + 6.5×0.10 + 8.0×0.05 = 7.382` → **7.4**

Manhã: **7.2**. Subida = C4 (jail + magic + Swagger) + C1/C5/C8 com evidência live.

### DECISÃO PIERRE (merge de código): APROVADO COM RESSALVAS

Score 6.0–7.9. C4 **7.4 ≥ 6.0**. **Isto não é Go de demo.** Gate HTML = **PIVOT** (KAAS live).

---

## FINDINGS DETALHADOS

### CRITICAL (bloqueadores de C4 / secrets)

Nenhum. Sem SQL concatenado. Sem `KAS_API_KEY` no bundle Next. `Kas__ApiKey=` vazio no `.env.example` (correcto). JWT de produção vazio no `appsettings.json` (compose injecta lab key via env). `/api/kas/*` = **410**.

### MAJOR fechados neste ensaio (eram bloqueio de Go de manhã)

| ID | Estado | Evidência |
|----|--------|-----------|
| MJ-E2E-01 | **FECHADO** | 4 healthy; ready 200 |
| MJ-E2E-02 / MJ-SEC-03 | **FECHADO** | `FileDocumentBlobStore.ReadAllBytesAsync` + `IsInsideRoot`; teste `ReadAllBytes_AbsolutePathOutsideRoot_ThrowsUnauthorized` |
| MJ-E2E-03 / MJ-SEC-04 | **FECHADO** | `UploadRules.IsAllowed` = MIME **∧** extensão **∧** `MatchesMagic`; ingest lê 16 bytes de header; `UploadRulesTests` (OR `octet-stream` falha) |
| MJ-E2E-04 Swagger | **FECHADO** | `UseBbfSwagger()` retorna se `!IsDevelopment()`; compose Production → Swagger **401** |

### MAJOR ainda abertos (não são C4 &lt; 6; um deles impede Go HTML)

| ID | Categoria | Onde | Descrição | Fix sugerido |
|----|-----------|------|-----------|--------------|
| MJ-E2E-05 | C5 / KAAS | `.env.example` `Kas__ApiKey=` | Worker healthy. Upload **202** depois **`falha`**. Sem ingest live + JSON KAAS. | `.env` gitignorado com `Kas__ApiKey` de laboratório. Compose já lê env. Sem chave = demo **só seed**. |
| MJ-E2E-06 / MJ-SEC-02 | C4 A02 | `Api/appsettings.json` ConnectionStrings | `Password=bbf` no JSON base (lab). Compose também mete a senha no YAML. | Senha só env. JSON base sem password. |
| MJ-SEC-06 | C4 A05 | `Program.cs` (ausência) | Sem HSTS, CSP, `X-Frame-Options`, `X-Content-Type-Options`. Swagger já gated. | Headers + HTTPS no host. |

### MINOR

| ID | Categoria | Onde | Descrição | Fix sugerido |
|----|-----------|------|-----------|-------------|
| MN-E2E-01 | C8 / UI | `src/app/page.tsx` | Lista/upload na API; KPIs ainda podem vir de `mocks.ts`. | Derivar KPIs de `GET /v1/documents` ou esconder. |
| MN-E2E-02 | C4 A07 | `src/lib/auth.ts` | JWT em `sessionStorage`. XSS = sessão. Aceitável em lab. | Cookie httpOnly no BFF, ou IdP. |
| MN-E2E-03 | C5 | testes | Sem Testcontainers. Aceite PG agora é live neste host. | Um teste Npgsql opcional. |
| MN-E2E-04 | C9 | repo | Sem `dotnet test` no CI do backend. | Workflow GitHub `dotnet test`. |
| MN-E2E-05 | C7 | `DocumentIngestService` | GET lista sem paginação. | `limit`/`cursor`. |
| MN-E2E-06 | C4 | `git.exe` | Histórico Git **não** varrido (PATH). | Scan de secrets quando Git estiver no PATH. |
| MN-E2E-07 | C5 | host tarde | `dotnet` fora do PATH; suite não reexecutada. | Reabrir PATH / rerun 95 testes. |

---

## DESTAQUES POSITIVOS

- Roteiro README **8/8 live** neste host (manhã era 0/8).
- Compose **4 healthy**; ready **200**; UI rotas de pipeline **200**.
- MJ-SEC-03 jail (`IsInsideRoot`) + teste de path absoluto fora do root.
- MJ-SEC-04 MIME ∧ extensão ∧ magic bytes + testes de bypass OR.
- Swagger **só Development**; Production compose **401**.
- KAAS **fora** do Next: 410 live; Worker isolado; chave vazia no example.
- `correlationId` `corr_*` no upload, authority e audit.
- Seed demo: ACME APROVADO, Delta REPROVADO, Gama MANUAL — visível no PG e na API.
- Login demo + RBAC: operador **403** em audit e replay.
- Upload real **202** + linha Postgres (não sessionStorage).

---

## GATES DO OS WF-21

| Gate | Resultado |
|------|-----------|
| Executar roteiro README (upload, pipeline, decisão, audit) | **CUMPRIDO** — 8/8 live; upload 202; pipeline OCR live = `falha` (chave) |
| Scorecard HTML Go/Pivot/Kill | **PIVOT** (KAAS live) |
| Relatório `REVIEW_WF-21_e2e.md` | **Este ficheiro** (reabertura tarde) |
| Sem Go = não abrir WF-22 | **Cumprido** — WF-22 não aberto |
| Não fazer terraform | **Cumprido** |
| `docker compose ps` + ready 200 | **Cumprido** |

---

## AÇÕES PARA VIRAR GO

Só resta o critério HTML **KAAS**:

1. `.env` (gitignored) com `Kas__ApiKey` real de laboratório. Não commitar.
2. Reensaiar **um** upload PDF → poll até canónico/JSON persistido (não `falha`) + `correlationId`.
3. Addendum neste ficheiro ou `REVIEW_WF-21_e2e_go.md`. **Só então** copiar OS WF-22 (documentar AWS, **sem** apply) — noutro chat.

Headers HSTS/CSP e senha fora do `appsettings.json` base **não** são o bloqueio HTML actual; continuam recomendados antes de qualquer host exposto.

Não tratar seed-only como Go KAAS. Go KAAS = ingest live + JSON persistido.

---

## MÉTRICAS SONARQUBE SIMULADAS

- **Bugs:** 0 evidentes no recorte E2E (Reliability: B — sem QG)
- **Vulnerabilities runtime:** CWE-22 e upload OR **fechados**; restam senha lab no JSON + headers (Security: B+)
- **Coverage estimada:** 65–75% (sem coverlet neste ensaio)
- **Testes:** 95 `[Fact]` no source; suite não reexecutada tarde (`dotnet` PATH)

---

## VEREDITO

| Critério | Manhã | Tarde | Efeito |
|----------|-------|-------|--------|
| Scorecard HTML | PIVOT | **PIVOT** | **Não abrir WF-22** |
| Score Pierre | 7.2 | **7.4 / 10** | Código aprovado com ressalvas |
| C4 | 6.6 | **7.4** | Não bloqueia merge; não autoriza Go |
| MJ-SEC-03 / MJ-SEC-04 | Abertos | **Fechados no código** | Deixam de recusar Go |
| Compose / ready | Engine 500 | **4 healthy / 200** | Go compose |
| Roteiro live | 0/8 | **8/8** | Demo seed verde |
| Upload live | n/a | **202 → falha** (sem chave) | Upload Go; KAAS Pivot |
| Swagger Production | anónimo | **401** | MJ-E2E-04 Swagger fechado |
| Terraform / AWS | Não | **Não** | Conforme OS |

**Pierre não autoriza Go.** Autoriza a esteira local: preencher `Kas__ApiKey` fora do git e reensaiar um ingest. AWS (WF-22) espera o Go. **Este chat não copia o bloco WF-22.**

---

## Apêndice — ensaio manhã (histórico)

Manhã 27/08: PIVOT por Docker Desktop Linux engine 500 + WSL ausente + MJ-SEC-03/04 abertos no código daquele snapshot + Swagger anónimo em Production. Score 7.2. Surrogate `dotnet test` 87/87. Live 0/8. Esse diagnóstico **não** descreve o estado da tarde.
