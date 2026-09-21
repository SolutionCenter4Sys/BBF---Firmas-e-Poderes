# PEER REVIEW REPORT — WF-12 Gate backend

**Reviewer:** Pierre (Tech Lead — Peer Review Specialist)
**Data:** 27/08/2026
**Projeto:** BBF Firmas e Poderes
**Stack:** .NET 8 / C# 12 + ASP.NET Core Minimal APIs + EF Core 8 + Npgsql + PostgreSQL 16 + xUnit
**Escopo:** `backend/` (Api, Worker, Domain, Infrastructure, Tests) — recorte WF-06 a WF-11
**Branch:** somente leitura. Sem merge neste chat.
**Dependências:** WF-06 Documents, WF-07 Worker KAAS, WF-08 Canonical, WF-09 Decision, WF-10 Audit, WF-11 Authority + Verification (código presente; artefato `_bmad-output/implementation-artifacts/WF-11-*.md` ausente)
**Persona:** `Agents DownStream/tech-lead-peer-review.md`
**Config BMAD:** `_bmad/bmm/config.yaml` (user: KáritaMaia). `_bmad/cis/config.yaml` não existe neste repo — review segue BMM + plano `docs/plano-execucao-refatoracao.html`. Specs/`_exports/security`/Sonar do projeto: ausentes.

Rubrica Pierre: **C1–C10** (pesos somam 100%). O menu chama “12 categorias”; o scorecard oficial tem dez. Gates extras do OS WF-12 (secrets / testes / Swagger) estão na seção própria, fora da média ponderada.

---

## SCORECARD

| # | Categoria | Score | Peso | Status | Findings |
|---|-----------|-------|------|--------|----------|
| C1 | Arquitetura & Estrutura | 7.5/10 | 15% | OK com ressalvas | 2 |
| C2 | S.O.L.I.D / Patterns | 7.0/10 | 12% | OK com ressalvas | 2 |
| C3 | Clean Code | 7.5/10 | 10% | OK com ressalvas | 2 |
| C4 | Segurança (OWASP) | **6.8/10** | 15% | Gate C4 ≥ 6 | 7 |
| C5 | Testes & Cobertura | 7.8/10 | 12% | OK | 2 |
| C6 | Error Handling | 7.5/10 | 8% | OK com ressalvas | 2 |
| C7 | Performance | 6.5/10 | 8% | OK com ressalvas | 3 |
| C8 | Documentação | 8.0/10 | 5% | OK | 2 |
| C9 | SonarQube Compliance | 6.5/10 | 10% | Sem QG no CI | 3 |
| C10 | Stack Best Practices | 8.0/10 | 5% | OK | 1 |

### SCORE FINAL: **7.2 / 10**

Cálculo: `7.5×0.15 + 7.0×0.12 + 7.5×0.10 + 6.8×0.15 + 7.8×0.12 + 7.5×0.08 + 6.5×0.08 + 8.0×0.05 + 6.5×0.10 + 8.0×0.05 = 7.24` → **7.2**

### DECISÃO: APROVADO COM RESSALVAS

- Score final 6.0–7.9 → merge **não bloqueado** por Pierre, com issues menores para WF-18 / hardening.
- **C4 = 6.8 ≥ 6.0** → exceção de segurança **não dispara**. Merge **não** é bloqueado por C4.
- Este chat **não faz merge** (OS: branch só leitura).

### Gates extras do OS WF-12

| Gate | Resultado | Evidência |
|------|-----------|-----------|
| Zero secret de produção no git | **PASS** (working tree) | `Kas:ApiKey` vazio no Worker; JWT de produção vazio em `appsettings.json`; chave KAAS só via env/`UserSecretsId`. Fixture de laboratório commitada (ver MJ-01). `git.exe` ausente no PATH — histórico Git **não** varrido (igual WF-09). |
| Testes verdes | **PASS** | `dotnet test backend/BbfFirmasPoderes.sln` → **77 aprovados, 0 falha, 0 ignorado** (duração ~7 s). |
| Swagger cobre `/v1` implementado | **PASS** | Todas as rotas `MapGet`/`MapPost` `/v1/*` têm `.WithTags` + `.WithSummary`. `GET /swagger/index.html` anônimo 200 (`FoundationAuthTests`). Matriz abaixo. |

---

## Swagger × `/v1` implementado

Contrato WF-03 (`docs/arquitetura/WF-03_MAPA_PARIDADE_API.md`) lista **7** rotas públicas. Backend implementa as 7 **e** duas extras (lista + canônico).

| Método | Path | Origem | Swagger (summary/tags) |
|--------|------|--------|------------------------|
| GET | `/v1/authority/decision` | WF-03 #1 / WF-11 | Sim |
| POST | `/v1/documents` | WF-03 #2 / WF-06 | Sim |
| GET | `/v1/documents/{documentId}/status` | WF-03 #3 / WF-06 | Sim |
| POST | `/v1/decision/evaluate` | WF-03 #4 / WF-09 | Sim |
| POST | `/v1/decision/{decisionId}/replay` | WF-03 #5 / WF-09 | Sim |
| GET | `/v1/audit/trail` | WF-03 #6 / WF-10 | Sim |
| GET | `/v1/verification/health` | WF-03 #7 / WF-11 | Sim |
| GET | `/v1/documents` | extra WF-06 | Sim |
| GET | `/v1/documents/{documentId}/canonical` | extra WF-08 | Sim |

Health (`/health`, `/health/live`, `/health/ready`) fora de `/v1`, documentados no README. Probe live/ready anônimos (WF-02/05).

---

## FINDINGS DETALHADOS

### CRITICAL (Bloqueadores)

Nenhum. Sem SQL concatenado, sem `Kas__ApiKey` real no código, sem JWT de produção commitado, sem `.Result`/`.Wait`.

### MAJOR (Devem ser corrigidos — não bloqueiam este gate; alvo WF-18)

| ID | Categoria | Arquivo:Linha | Descrição | Fix sugerido |
|----|-----------|---------------|-----------|-------------|
| MJ-01 | C4 | `backend/src/BbfFirmasPoderes.Api/appsettings.json:18-19` e `appsettings.Development.json:11-14` | Connection string com `Password=bbf` no JSON base (não só Development). Signing key HS256 de laboratório (`dev-only-change-me-32-chars-min!!`) no Development. Fora de Development o JWT vazio **quebra o boot** — bom. Senha docker no arquivo base pode ir para imagem de “prod” se ninguém sobrescrever `ConnectionStrings__Postgres`. | Tirar senha do `appsettings.json` (só env). Manter JWT lab **somente** em Development. No Worker, `Kas__ApiKey` só User Secrets / env (já é o desenho). |
| MJ-02 | C4 A01/A02 | `DocumentsEndpoints.cs:176-184` (`GetCanonical`) | `GET /v1/documents/{id}/canonical` devolve `Cpf` cru. Trilha de audit mascara (`PiiMask.InText`). Seed ACME já vem mascarado; payload KAAS real pode gravar CPF completo. Policy `documents:read` inclui **consumer**. | Aplicar `PiiMask.Cpf` na borda da API (ou campo só para operador/auditor). Consumer não precisa do CPF canônico. |
| MJ-03 | C4 A05 | `SwaggerExtensions.cs:48-61`, `Program.cs:108` | Swagger UI + JSON anônimos em **qualquer** ambiente. Superfície da API pública sem auth. | `UseBbfSwagger()` só se `IsDevelopment()` (ou flag). Produção: desligar ou proteger. |
| MJ-04 | C4 CWE-22 | `FileDocumentBlobStore.cs:40-52` | `ReadAllBytesAsync` aceita path absoluto do banco **sem** verificar que está sob `StorageRoot`. Save usa `doc_{guid}` (seguro). Exploit exige tamper de `storage_path`. | `Path.GetFullPath` + `StartsWith(root)`. Recusar path fora do volume. |
| MJ-05 | C4 A03/upload | `UploadRules.cs:23-30` | MIME **ou** extensão. `application/octet-stream` + `foo.pdf` passa. Sem magic bytes (`%PDF`, JPEG SOI). | Exigir MIME **e** extensão na whitelist **e** assinar o arquivo (primeiros bytes). |
| MJ-06 | C4 A05 | `Program.cs` (ausência) | Sem HSTS, CSP, `X-Frame-Options`, `X-Content-Type-Options`. Sem `UseHttpsRedirection`. `AllowedHosts: *`. Aceitável no compose HTTP local; insuficiente para AWS. | Middleware de headers + HTTPS no host (WF-18 / WF-19). |
| MJ-07 | C7 / C6 | `DocumentIngestService.cs:120-124`; `InMemoryIdempotencyStore.cs`; `OutboxKaasProcessor.cs:23-26` | Lista de documentos sem paginação. Idempotência 24h só in-memory (some no restart / não replica). Outbox `FirstOrDefault` sem `FOR UPDATE SKIP LOCKED` — dois workers podem pegar a mesma linha. | Paginar GET lista. Persistência da idempotência (tabela). Lock pessimista / `SKIP LOCKED` no outbox. |

### MINOR (Recomendações)

| ID | Categoria | Arquivo:Linha | Descrição | Fix sugerido |
|----|-----------|---------------|-----------|-------------|
| MN-01 | C1 | solução `BbfFirmasPoderes.sln` | Sem projeto Application. Orquestração (`DocumentIngestService`, `DecisionService`, `OutboxKaasProcessor`) vive em Infrastructure. Domain sem dependência externa (correto). Api referencia Infra direto. | Extrair Application quando o recorte crescer; não é blocker do walking skeleton. |
| MN-02 | C2 | `OutboxKaasProcessor.cs:35-155` | `ProcessMessageAsync` orquestra ingest + result + canônico + audit (~120 linhas). | Partir em `IKasIngestStep` / `ICanonicalApplyStep`. |
| MN-03 | C3 | `*Endpoints.cs` (Documents, Decision, Authority, Audit) | Helper `Problem(...)` copiado 4 vezes. | Extensão `Results.BbfProblem(...)` única. |
| MN-04 | C4 A04 | `RateLimitingExtensions.cs` + `AuthorityEndpoints.cs:16` | 429 só em `/v1/authority/decision`. Upload e evaluate sem rate limit. | Política por IP/sub em POST documents e evaluate (WF-18). |
| MN-05 | C4 A01 | `ScopeClaims.cs:19-26` | Qualquer `role=consumer` lê authority **sem** exigir scope `api:decision:read`. Scope é fallback. | `consumer` **e** scope, alinhado ao OS WF-11. |
| MN-06 | C5 / C9 | `BbfFirmasPoderes.Tests.csproj` | `dotnet list package --vulnerable`: **Scriban.Signed 5.5.0** (Critical/High, transitivo — típico WireMock) e **System.Linq.Dynamic.Core 1.3.12** (High). **Api, Infrastructure e Worker: zero pacote vulnerável.** | Atualizar WireMock.Net; CVE não entra no runtime da API. |
| MN-07 | C5 | testes HTTP | WebApplicationFactory + EF InMemory. Sem Testcontainers/Postgres. Aceite PG dos WF-02/04/06 ainda pendente Docker/WSL. | WF-19 compose + um teste de integração Npgsql. |
| MN-08 | C6 | `OutboxKaasProcessor.cs:62-119` | Um POST ingest + um POST result; HTTP timeout 300s; **sem retry**. Falha → `falha` + outbox processado (não reentrega). | Retry com backoff só em 5xx/timeout; 4xx permanece `falha`. |
| MN-09 | C7 | `KasDataUrl.cs:11` | Blob até 50 MB vira data URL Base64 na memória (~33% a mais) e vai no POST KAAS. | URL pré-assinada / stream quando KAAS aceitar; teto de bytes no worker. |
| MN-10 | C8 | `_bmad-output/implementation-artifacts/` | README cobre WF-05…09. Falta `WF-11-*.md` (código de Authority/Verification/RateLimit/Idempotency está no tree). | Artefato de aceite WF-11 no próximo chat com Git. |
| MN-11 | C8 | Swagger | Sem `ProducesProblem`/`ProducesResponseType` por status; DTOs sem XML comments. | Attributes ou `TypedResults` para 401/403/422 no OpenAPI. |
| MN-12 | C9 | repo | Sem `sonar-project.properties`, sem Quality Gate no CI. Único workflow GitHub: Azure Static Web Apps (front). | Pipeline `dotnet test` + Sonar no backend (DevOps Master / WF-19+). |
| MN-13 | C10 | vs agente Ricardo genérico | Spec genérica do Ricardo manda Dapper + SQL Server + `AppResponse<T>`. **Este** projeto (AR Pierre 26/08 + plano HTML) fixou EF Core + PostgreSQL + ProblemDetails RFC 7807. Aderência ao ADR do BBF, não ao template genérico. | Manter EF+PG. Não “corrigir” para Dapper. |
| MN-14 | C6 | `AuthorityEndpoints.cs:55-66` | Documento sem canônico / not found vira **400**, não 404. Contrato consumer (WF-03: 400 params). | Documentar no Swagger; não misturar com 404 interno do evaluate. |

---

## C1 — Arquitetura & Estrutura — 7.5/10

Esperado (.NET Clean): Api → Application → Infrastructure → Domain, dependências para dentro.

Encontrado:

- Domain **sem** pacotes NuGet; entidades, `DecisionEngine`, `CanonicalMapper`, ports (`IKasClient`, `IDocumentBlobStore`, `IIdempotencyStore`, `IOfficialSourceHealthStore`).
- Infrastructure: EF configurations, Npgsql, blob filesystem, KAAS HTTP, outbox processor.
- Api: auth JWT, endpoints Minimal API, Swagger, rate limit, correlation, ProblemDetails.
- Worker: só `AddKaasPipeline` + hosted service. Teste `ApiHost_DoesNotRegisterKasClient` confirma a API **não** registra `IKasClient`.

Desvio: não há projeto Application; serviços de aplicação estão na Infra. Entidades anêmicas (setters públicos, sem invariantes). Aceitável no recorte WF-02…11.

---

## C2 — S.O.L.I.D / Patterns — 7.0/10

- SRP: controllers não têm regra de decisão; `DecisionEngine` é puro (sem I/O). Upload valida em `UploadRules` + `DocumentIngestService`.
- DIP: Domain define ports; Infra implementa. Api não chama KAAS.
- OCP: catálogo `DecisionRuleCatalog` + engine; stub de circuit breaker por fonte.
- Interceptor append-only = proteção WORM no SaveChanges.
- God-ish: `OutboxKaasProcessor.ProcessMessageAsync`.

---

## C3 — Clean Code — 7.5/10

Nullable + implicit usings. Nomes alinhados ao domínio (`corr_`, `doc_`, `dec_`). Sem TODO/FIXME. Sem `.Result`. Duplicação do helper ProblemDetails. `ProcessMessageAsync` acima do limiar de 60 linhas (Sonar).

---

## C4 — Segurança (OWASP) — 6.8/10  (foco do gate)

**Por que ≥ 6 (não bloqueia):** baseline de recurso JWT está no lugar. Não há injection SQL, não há chave KAAS no git, deny-by-default, RBAC por rota, PII em log/audit, upload com teto e MIME, rate limit no consumer, timeout KAAS, chave só no Worker.

Checklist OWASP Top 10 (2021) no recorte `backend/`:

| Item | Status | Nota |
|------|--------|------|
| A01 Broken Access Control | Parcial | FallbackPolicy = autenticado. Policies por rota. Audit só auditor/admin (`AuditTrailApiTests` 403 operador). Consumer em documents:read = corpus inteiro (single-tenant BBF; sem ABAC por CNPJ). |
| A02 Cryptographic Failures | Parcial | HS256 com key ≥ 32 bytes; ValidateIssuer/Audience/Lifetime. Lab key só Development. CPF canônico pode vazar (MJ-02). Blobs em filesystem local, não cifrados at rest (volume compose). |
| A03 Injection | OK | EF LINQ parametrizado; zero `FromSql`/`ExecuteSql`. Upload sanitiza `Path.GetFileName`. |
| A04 Insecure Design | Parcial | Outbox assíncrono (não processa OCR no request). Rate limit só authority. Idempotência in-memory. Circuit breaker stub (WF-11: sem HTTP Junta). |
| A05 Misconfiguration | Parcial | ProblemDetails genérico (OnChallenge/OnForbidden). Swagger aberto (MJ-03). Sem security headers (MJ-06). `AllowedHosts *`. |
| A06 Vulnerable Components | OK na API | `dotnet list package --vulnerable`: Api/Infra/Worker limpos. Testes: Scriban transitivo (MN-06). |
| A07 Auth Failures | OK p/ resource server | JWT com expiração (`ValidateLifetime`, ClockSkew 1 min). Sem senha local / lockout / MFA — IdP fora de escopo. |
| A08 Integrity | N/A parcial | Sem SRI (API). CI backend inexistente. |
| A09 Logging | OK | Serilog + `PiiMaskingTextFormatter`. `X-Correlation-Id`. Audit tipado + interceptor. |
| A10 SSRF | OK | URL KAAS só de config (`Kas:RunUrl`), não de input do cliente. |

**Secrets (working tree):**

- `Kas:ApiKey` = `""` em `Worker/appsettings.json`.
- JWT produção = `""` (boot falha se env não vier).
- JWT Development = fixture documentada no README.
- Postgres `Username=bbf;Password=bbf` = compose local (MJ-01).
- Testes: `test-kaas-key-not-real`, `test-only-signing-key-32-bytes!!`.
- `.env.example` **não** contém `KAS_API_KEY`. `.env.local` está no `.gitignore`.
- Worker tem `UserSecretsId` (`bbf-firmas-poderes-worker`).

Histórico Git: `git.exe` não está no PATH nesta máquina. Gate “zero secret” vale para o working tree analisado, não para `git log -p`.

---

## C5 — Testes & Cobertura — 7.8/10

77 fatos em 14 classes. Recorte por WF:

- Health/JWT/Swagger: `FoundationAuthTests`, `HealthLiveSmokeTests`
- Documents 202/415/422/outbox: `DocumentsApiTests`
- KAAS ingest+result+WireMock: `KaasPipelineTests`
- Canônico: `CanonicalMapperTests`
- Decisão RN01–RN04+TH01 + API evaluate/replay: `DecisionEngineTests`, `DecisionApiTests`
- Audit + interceptor: `AuditTrailApiTests`, `AppendOnlyAuditInterceptorTests`
- Authority 200/400/401/403 + 429: `AuthorityApiTests`, `AuthorityRateLimitTests`
- Verification stub: `VerificationHealthApiTests`

Faltam: cobertura numérica (coverlet), Testcontainers, teste de path traversal, teste de magic bytes, inventário automatizado de paths no `swagger.json`. Coverage estimada (julgamento, sem instrumentação): **~65–75%** do código de aplicação — abaixo do 70% “oficial” Pierre para .NET se contar Infra/Worker sem asserts de integração PG.

---

## C6 — Error Handling & Resiliência — 7.5/10

`UseExceptionHandler` + `AddProblemDetails` com `correlationId`. JWT challenge/forbidden → problem+json sem stack. Upload 413 do Kestrel remapado para 422 (OS WF-06). Worker captura exceção do ciclo e segue. Pipeline KAAS: catch → `falha` + outbox marcado (não deixa mensagem zumbi). Circuit breaker **stub** (aberto → MANUAL, sem HTTP Junta — correto para WF-11). Sem retry HTTP.

---

## C7 — Performance — 6.5/10

Índices em documents (cnpj, status, correlation, hash), audit, outbox, decisions, kas_runs. Npgsql pooling default. Sem N+1 óbvio no canônico (`Include` socios/poderes). Lista GET sem Skip/Take. Payload KAAS data-URL em memória. Idempotência e health de fontes in-memory. Outbox sem skip-locked.

---

## C8 — Documentação — 8.0/10

`backend/README.md` atualizado até WF-09 (rotas, JWT, worker, aceite). Swagger com Bearer + descrições nas rotas. Mapa WF-03. ER em `docs/arquitetura/ER.md`. Falta artefato WF-11. OpenAPI não lista todos os status codes.

---

## C9 — SonarQube (simulado) — 6.5/10

Não há Sonar neste repo. Simulação:

- **Bugs:** 0 evidentes (NRE de stream tratado com dispose; blob missing → FileNotFound → falha). Reliability ~B por falta de QG.
- **Vulnerabilities (app):** 0 CWE-89/79/78. Hotspots: path (MJ-04), upload MIME (MJ-05), Swagger (MJ-03). Security ~B.
- **Smells:** helper Problem duplicado; `ProcessMessageAsync` longo; entidades anêmicas. Maintainability ~B.
- **Duplications:** helper Problem ×4; baixo % no restante.
- **Coverage:** não medida. Estimativa < 80% new-code gate.

---

## C10 — Stack Best Practices (.NET deste projeto) — 8.0/10

Primary constructors, `async` ponta a ponta, `JsonStringEnumConverter`, Minimal APIs, Options pattern, `IHttpClientFactory` no Worker, Nullable. ADR BBF: EF Core + PostgreSQL (não Dapper/SQL Server do template genérico Ricardo). Erros em ProblemDetails, não `AppResponse<T>` — adequado a API pública RFC 7807.

---

## DESTAQUES POSITIVOS

- Deny-by-default (`FallbackPolicy`) + RBAC por capability (`documents:upload`, `audit:read`, `api:decision:read`).
- Chave KAAS isolada no Worker; teste garante que a Api **não** registra `IKasClient`; Next `/api/kas/*` 410 (WF-07).
- Motor de decisão determinístico no Domain, replay pelo snapshot (sem reconsulta).
- Audit append-only no interceptor EF + máscara PII em log e na trilha.
- Upload 202 + outbox; request HTTP não chama OCR/KAAS.
- Correlation-Id gerado/ecoado; ProblemDetails com `correlationId`.
- 77 testes verdes incluindo 401/403/415/422/429 e pipeline WireMock.
- Pacotes da **API/Infra/Worker** sem advisory NuGet nas fontes atuais.

---

## AÇÕES REQUERIDAS

### Para este gate (WF-12)

Nenhuma correção de código neste chat (OS: só gate, sem features).

- [x] C4 ≥ 6
- [x] Testes verdes (77/77)
- [x] Swagger cobre `/v1` implementado
- [x] Sem secret de produção no working tree
- [ ] Histórico Git de secrets — **pendente** (`git.exe` fora do PATH)

### Antes de tratar como produção (WF-18 / WF-19)

- [ ] [MJ-01] Senha fora do `appsettings.json` base; JWT lab só Development
- [ ] [MJ-02] Máscara CPF no canônico (ou restringir consumer)
- [ ] [MJ-03] Swagger só em Development
- [ ] [MJ-04] Confinar blob ao `StorageRoot`
- [ ] [MJ-05] MIME + extensão + magic bytes
- [ ] [MJ-06] Headers de segurança + HTTPS no host
- [ ] [MJ-07] Paginação, idempotência persistida, lock de outbox
- [ ] [MN-06] Atualizar WireMock (CVE só em testes)
- [ ] Compose 4 containers + evidência PG (fila `WF-*-PENDENTE-DOCKER`)

### Recomendações (não bloqueantes)

- [ ] [MN-01][MN-02][MN-03] Application layer / split do processor / DRY Problem
- [ ] [MN-10] Artefato WF-11
- [ ] [MN-12] CI `dotnet test` + Sonar no backend

---

## MÉTRICAS SONARQUBE SIMULADAS

- **Bugs:** 0 evidentes (Reliability: B — sem QG)
- **Vulnerabilities (runtime API):** 0 conhecidas NuGet; hotspots de config/upload (Security: B)
- **Code Smells:** duplicação Problem + método longo no outbox (Maintainability: B)
- **Duplications:** baixa (< 5% estimado no app; helper Problem é o bloco óbvio)
- **Cognitive Complexity:** `DecisionEngine.Evaluate` e `ProcessMessageAsync` são os picos
- **Coverage estimada:** 65–75% (sem coverlet)
- **Testes:** 77 / 77 verdes (2026-08-27, SDK local)

---

## VEREDITO DE MERGE

| Critério | Valor | Efeito |
|----------|-------|--------|
| Score final | 7.2 | Aprovado com ressalvas |
| C4 Segurança | 6.8 | **Não bloqueia** |
| Secrets produção no tree | Não | Pass |
| Testes | 77 verdes | Pass |
| Swagger `/v1` | 7+2 rotas documentadas | Pass |

**Pierre não autoriza merge neste chat** (OS: branch só leitura). **Pierre não veta** um merge futuro da branch de backend por C4: o veto C4 < 6 **não se aplica**.

Próximo passo do plano: **WF-13** (Sofia, cliente HTTP no front) **ou** **WF-18** (Security Guardian) para fechar MJ-01…MJ-07 antes de AWS.
)
