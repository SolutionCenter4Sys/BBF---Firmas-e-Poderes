# REVIEW WF-18 — Security Guardian (OWASP)

**Reviewer:** Security Guardian (Cyber Security Specialist)
**Data:** 27/08/2026
**Projeto:** BBF Firmas e Poderes
**Persona:** `Agents Habilitadores/the-security-guardian.md`
**Config BMAD:** `_bmad/bmm/config.yaml` (user: KáritaMaia, PT-BR). `_bmad/cis/config.yaml` **ausente** neste repo — review segue BMM + OS do plano `docs/plano-execucao-refatoracao.html` § WF-18.
**Branch alvo de correção:** `wf-18-security` (somente PRs de hardening). Este chat **não implementa** features e **não abre** PR.
**Dependências lidas:** WF-12 Pierre (`REVIEW_WF-12_backend.md`), WF-07 (KAAS no Worker), WF-13 (cliente HTTP + CORS + JWT em sessionStorage).
**Nível de segurança recomendado:** **3** (corporativo / PII / financeiro bancário BBF). Controles atuais ≈ **nível 2 incompleto**.

Gate Pierre: **C4 < 6 = bloqueio**. Este review só pontua C4 (OWASP). Demais categorias C1–C3 / C5–C10 ficam com Pierre (WF-12 e WF-21).

---

## SCORECARD C4 (entrada Pierre)

| # | Categoria | Score | Peso Pierre | Status | Findings neste review |
|---|-----------|-------|-------------|--------|------------------------|
| C4 | Segurança (OWASP) | **6.6 / 10** | 15% | Gate C4 ≥ 6 | 6 MAJOR + 5 MINOR |

### DECISÃO PARA PIERRE: **NÃO BLOQUEIA** (C4 ≥ 6.0)

- C4 = **6.6 ≥ 6.0** → exceção de segurança **não dispara**.
- Score **não** autoriza produção AWS. Autoriza seguir WF-19/20 com PRs de correção em `wf-18-security` **antes** do Go de WF-21.
- Este chat **não faz merge** e **não entrega código**.

### Por que ≥ 6 (não bloqueia)

Baseline de resource server JWT está no lugar: deny-by-default, RBAC por rota, HS256 com chave ≥ 32 bytes, ValidateIssuer/Audience/Lifetime, sem SQL concatenado, KAAS isolado no Worker, `/api/kas/*` = 410, `KAS_API_KEY` **ausente** do bundle Next, CORS com allowlist (não `*`), teto de upload 50 MB, sanitização de filename, PII mascarado no mapper/log/audit.

### Por que não sobe a 8+

CWE-22 no read do blob, upload MIME **ou** extensão (sem magic bytes), senha Postgres no `appsettings.json` base, Swagger anônimo em qualquer ambiente, ausência de headers de segurança, JWT da UI em `sessionStorage`.

---

## GATES DO OS WF-18

| Gate | Resultado | Evidência |
|------|-----------|-----------|
| Secrets (working tree) | **PASS** com ressalvas | `Jwt:SigningKey` vazio em produção (`appsettings.json`). `Kas:ApiKey` = `""` no Worker. Sem `KAS_API_KEY` em `src/`. `.env.example` e `.env.local.example` **não** carregam a chave. `.env.local` gitignorado **ainda contém** `KAS_API_KEY` + `KAS_RUN_URL` (legado WF-07) — ver MJ-SEC-01. Senha docker `Password=bbf` no JSON base (MJ-SEC-02). `git.exe` fora do PATH — histórico **não** varrido. |
| Path traversal do blob | **FAIL** (MAJOR, não bloqueia C4) | `FileDocumentBlobStore.ReadAllBytesAsync` aceita path absoluto do banco sem confinar a `StorageRoot`. Save usa `doc_{guid}` (seguro). Exploit exige tamper de `storage_path`. |
| JWT | **PASS** (resource server) | Issuer + audience + lifetime + signing key ≥ 32 bytes. FallbackPolicy autenticada. ClockSkew 1 min. UI guarda token em `sessionStorage` (`bbf.access_token`) — XSS = roubo de sessão (MN-SEC-01). Sem pin de `ValidAlgorithms`. |
| CORS | **PASS** para lab local | Policy `bbf-front`: `WithOrigins("http://localhost:3000")` + `AllowAnyHeader` + `AllowAnyMethod`. **Sem** `AllowCredentials` (adequado a Bearer). Origem de produção **não** é configurável — fail-closed fora de `:3000` (MN-SEC-02). |
| Upload content-type | **FAIL** (MAJOR, não bloqueia C4) | `UploadRules.IsAllowed` = MIME **ou** extensão. `application/octet-stream` + `foo.pdf` passa. Sem magic bytes. Front (`envio`/`page`) filtra UX; servidor é a fonte da verdade. Teste 415 cobre `text/plain` + `.txt`, **não** o bypass OR. |
| `KAS_API_KEY` ausente do bundle Next | **PASS** | `src/**` não referencia `process.env.KAS_API_KEY` nem `KAS_API_KEY`. `next.config.js` só lê `NEXT_PUBLIC_API_URL`. Rotas `/api/kas/run` e `/api/kas/return` devolvem 410 **sem** ler env. Scan de `.next/**` (94 ficheiros, incluindo `static/`): **0 hits** em `KAS_API_KEY` e no prefixo da chave local. Única `NEXT_PUBLIC_*` no client: `NEXT_PUBLIC_API_URL`. |

---

## OWASP Top 10 (2021) — recorte API + Next + Worker

| Item | Status | Nota |
|------|--------|------|
| A01 Broken Access Control | Parcial | FallbackPolicy = autenticado. Policies por rota. Audit só auditor/admin. `documents:read` inclui **consumer** (corpus single-tenant). `ScopeClaims.CanReadAuthority`: `role=consumer` **basta**, sem exigir `api:decision:read` (MN-SEC-03). Upload com `.DisableAntiforgery()` — aceitável porque o token vai no header `Authorization`, não em cookie. |
| A02 Cryptographic Failures | Parcial | HS256 + key length check no boot. Lab key só `appsettings.Development.json`. JWT de produção vazio **quebra o boot**. Blobs em filesystem, sem cifrar at rest. CPF canônico mascarado **na escrita** (`CanonicalMapper` → `PiiMask.Cpf`); API não re-mascara na borda (defesa em profundidade). |
| A03 Injection | OK SQL / parcial upload | EF LINQ; zero `FromSql`/`ExecuteSql`. Filename via `Path.GetFileName`. Upload aceita polyglot se extensão **ou** MIME na whitelist (MJ-SEC-04). |
| A04 Insecure Design | Parcial | Outbox assíncrono (OCR fora do request). Rate limit **só** `/v1/authority/decision`. Upload e evaluate sem 429. Idempotência in-memory (WF-12 MJ-07). |
| A05 Security Misconfiguration | Parcial | ProblemDetails genérico (não vaza stack). Swagger UI + JSON anônimos em **qualquer** ambiente (MJ-SEC-05). Sem HSTS/CSP/`X-Frame-Options`/`X-Content-Type-Options`. Sem `UseHttpsRedirection`. `AllowedHosts: *`. |
| A06 Vulnerable Components | Não re-scan | WF-12: Api/Infra/Worker limpos no `dotnet list package --vulnerable`; testes com Scriban transitivo. Este review **não** reexecutou SCA. |
| A07 Identification and Authentication Failures | OK p/ resource server | ValidateLifetime. Sem IdP/login/lockout/MFA neste recorte (IdP fora de escopo). Token da UI em sessionStorage. |
| A08 Software and Data Integrity | Parcial | Sem SRI (SPA). CI backend inexistente. Magic bytes ausentes no upload. |
| A09 Logging and Monitoring | OK p/ Nível 2- | Serilog + `PiiMaskingTextFormatter`. `X-Correlation-Id`. Audit tipado + interceptor append-only. Sem SIEM. |
| A10 SSRF | OK | URL KAAS só de config (`Kas:RunUrl`), não de input do cliente. Rewrite Next `/health/:path*` aponta para `NEXT_PUBLIC_API_URL` fixo no build — path não escolhe host. |

---

## 1. Secrets

### O que está correto

- Worker: `Kas:ApiKey` vazio no JSON; boot recusa chave vazia (`OutboxWorker`, `HttpKasClient`). `UserSecretsId` = `bbf-firmas-poderes-worker`.
- API **não** registra `IKasClient` (teste `ApiHost_DoesNotRegisterKasClient`).
- Next: `.env.example` documenta que `KAS_API_KEY` **não** vive no front. `.env.local.example` só `NEXT_PUBLIC_API_URL`.
- `.gitignore` inclui `.env.local` e variantes.
- JWT produção = `""` (fail-secure no boot). Fixture HS256 só em Development: `dev-only-change-me-32-chars-min!!`.
- Testes: `test-kaas-key-not-real`, `test-only-signing-key-32-bytes!!`.

### Riscos

| ID | Sev | Onde | Risco | PR em `wf-18-security` |
|----|-----|------|-------|------------------------|
| MJ-SEC-01 | Major | `.env.local` (gitignored) | Ficheiro local ainda tem `KAS_API_KEY` e `KAS_RUN_URL` (legado do proxy Node). Next **não** injeta no client (sem `NEXT_PUBLIC_` e sem referência em `src/`), mas o processo Node de `next dev` **carrega** o valor em `process.env`. Qualquer import futuro de `process.env.KAS_API_KEY` no client vaza a chave. | Apagar `KAS_*` de `.env.local` do Next. Chave só em User Secrets / env do **Worker** (`Kas__ApiKey`). Rotacionar a chave no KAAS se este valor já circulou em máquina de desenvolvimento. **Não** commitar o ficheiro. |
| MJ-SEC-02 | Major | `Api/appsettings.json:18-19`, `Worker/appsettings.json:9-11` | `Username=bbf;Password=bbf` no JSON **base** (não só Development). Imagem “prod” herda senha docker se ninguém sobrescrever `ConnectionStrings__Postgres`. | Tirar password do JSON base; só env. Manter no `appsettings.Development.json` se o compose local exigir. |
| MN-SEC-04 | Minor | `src/lib/kas-ids.ts:1-2` | `DEFAULT_KAS_RUN_URL` (Railway dev) ainda no módulo client. Não é secret, mas revela jornada e host KAAS. | Remover do client ou restringir a código server-only morto; URL só no Worker. |

**Chave KAAS:** o valor concreto **não** é reproduzido neste relatório.

---

## 2. Path traversal do blob (CWE-22)

`FileDocumentBlobStore`:

- **Save:** `Path.Combine(ResolveRoot(), documentId)` com `documentId = doc_{guid}` — **seguro**.
- **Read:** se `storagePath` é absoluto, usa o valor **tal como está no banco**, sem `Path.GetFullPath` + prefixo de `StorageRoot`.

```40:52:backend/src/BbfFirmasPoderes.Infrastructure/Storage/FileDocumentBlobStore.cs
    public Task<byte[]> ReadAllBytesAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        // ...
        var path = Path.IsPathRooted(storagePath)
            ? storagePath
            : Path.Combine(ResolveRoot(), storagePath);
        // File.ReadAllBytesAsync(path) — sem StartsWith(root)
```

**Pré-condição do exploit:** alterar `documents.storage_path` (SQL direto, backup restaurado, bug futuro de write). Ingest atual grava o path devolvido pelo Save (dentro do root). Não é RCE anónimo; é leitura arbitrária **pós-compromisso do dado**.

**Fix (PR correção):**

```csharp
var full = Path.GetFullPath(path);
var root = Path.GetFullPath(ResolveRoot());
if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
    && !string.Equals(full, root, StringComparison.OrdinalIgnoreCase))
    throw new UnauthorizedAccessException("storage_path fora de StorageRoot.");
```

Teste: gravar `storage_path = C:\Windows\win.ini` (ou `/etc/passwd`) e esperar recusa — hoje leria se o ficheiro existir.

---

## 3. JWT

### Resource server (API) — adequado ao Nível 2

`AuthExtensions.AddBbfAuth`:

- `ValidateIssuer` / `ValidateAudience` / `ValidateLifetime` / `ValidateIssuerSigningKey` = true
- `ClockSkew` = 1 minuto
- Signing key UTF-8 ≥ 32 bytes (HS256) senão o host não sobe
- `FallbackPolicy` = autenticado
- Challenge/Forbidden → `application/problem+json` sem stack
- Roles: `operador` (alias `analista`), `auditor`, `admin`, `consumer`

Lacunas (não bloqueiam C4):

- Sem `ValidAlgorithms = { SecurityAlgorithms.HmacSha256 }` (alg confusion é improvável com só chave simétrica, mas o pin é barato).
- Sem `RequireHttpsMetadata` explícito — default ASP.NET (true fora de Development). Lab HTTP local depende do ambiente.
- Sem rotação de chave / `kid`.

### Cliente (Next)

- Token em `sessionStorage` chave `bbf.access_token` (`src/lib/auth.ts`).
- `apiFetch` manda `Authorization: Bearer`.
- 401 limpa token e dispara `bbf:unauthorized`.
- Operador cola JWT na UI (Dashboard, envio, filas). **Não** há cookie HttpOnly — CSRF baixo; XSS alto.

`sessionStorage` some ao fechar o tab (melhor que `localStorage`). Continua visível a qualquer script da origem.

---

## 4. CORS

```37:43:backend/src/BbfFirmasPoderes.Api/Program.cs
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("bbf-front", policy =>
            policy.WithOrigins("http://localhost:3000")
                .AllowAnyHeader()
                .AllowAnyMethod());
    });
```

- Allowlist explícita (não `*`) — **correto** para o lab WF-13.
- Sem `AllowCredentials` — coerente com Bearer no header.
- `UseCors` **antes** de Authentication — ordem ASP.NET correta.
- Produção (CloudFront / domínio BBF) vai falhar CORS até existir config `Cors:Origins`. Preferível fail-closed do que `*`.

PR: `Cors__Origins` (lista) + default `http://localhost:3000` só em Development.

---

## 5. Upload content-type

Servidor (`UploadRules.IsAllowed`):

```23:31:backend/src/BbfFirmasPoderes.Domain/Documents/UploadRules.cs
        if (!string.IsNullOrWhiteSpace(mime) && AllowedContentTypes.Contains(mime))
            return true;
        var ext = Path.GetExtension(fileName ?? string.Empty);
        return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
```

Bypass: MIME fora da lista **e** extensão `.pdf` → aceito. Ou MIME `application/pdf` com payload não-PDF.

Front (`envio/page.tsx`, `page.tsx`): `accept` + `isAllowedFile` (MIME pdf/image **ou** extensão). `image/*` no client é mais largo que a whitelist do servidor (`svg` cai no client e o servidor rejeita se a extensão não estiver na lista). Contorno UX, não controlo.

Tamanho: 50 MB no domínio + Kestrel/FormOptions com teto + 1 MB; 413 remapado para 422. Teste `PostDocuments_Oversize_Returns422` existe.

**Fix:** exigir MIME **e** extensão na whitelist **e** assinar os primeiros bytes (`%PDF-`, JPEG SOI `FF D8`, PNG `\x89PNG`, WEBP `RIFF....WEBP`, TIFF `II*`/`MM*`). Rejeitar `application/octet-stream` mesmo com `.pdf`.

---

## 6. `KAS_API_KEY` × bundle Next

Cadeia verificada:

| Camada | Resultado |
|--------|-----------|
| `src/**/*.ts(x)` | Zero `KAS_API_KEY` / `process.env.KAS*` |
| `next.config.js` | Só `NEXT_PUBLIC_API_URL` |
| `src/app/api/kas/run/route.ts` | POST → **410**, sem env |
| `src/app/api/kas/return/route.ts` | POST → **410**, sem env |
| `src/lib/kas-client.ts` | Stub 410; não lê chave |
| `.env.local.example` | Sem chave |
| `.env.example` | Comentário proibindo chave no Next |
| `.next/**` (artefacto local) | **0** ocorrências de `KAS_API_KEY` e do prefixo da chave local |
| Worker | Único consumidor (`X-Flow-Api-Key`) |

**Veredicto:** chave **não** entra no bundle cliente. Risco residual = `.env.local` legado no processo `next dev` (MJ-SEC-01), não o JS servido ao browser.

---

## FINDINGS

### CRITICAL (bloqueadores C4 / produção imediata)

Nenhum. Sem secret de produção no working tree versionável, sem SQLi, sem chave KAAS no bundle.

### MAJOR (PRs em `wf-18-security` — não bloqueiam este gate)

| ID | OWASP | Onde | Descrição | Fix |
|----|-------|------|-----------|-----|
| MJ-SEC-01 | A02 | `.env.local` (local, gitignored) | `KAS_API_KEY` + `KAS_RUN_URL` ainda no env do Next. | Remover do Next; User Secrets do Worker; rotacionar chave KAAS. |
| MJ-SEC-02 | A02 | `appsettings.json` Api + Worker | Password Postgres no JSON base. | Só env / Development. |
| MJ-SEC-03 | A05 / CWE-22 | `FileDocumentBlobStore.cs:40-52` | Read de path absoluto sem jail em `StorageRoot`. | `GetFullPath` + `StartsWith(root)`. |
| MJ-SEC-04 | A03 / A08 | `UploadRules.cs:23-30` | MIME **ou** extensão; sem magic bytes. | MIME **e** extensão **e** assinatura. |
| MJ-SEC-05 | A05 | `SwaggerExtensions.cs:48-61`, `Program.cs:115` | Swagger anónimo em todos os ambientes. | `UseBbfSwagger()` só `IsDevelopment()` (ou flag). |
| MJ-SEC-06 | A05 | `Program.cs` (ausência) | Sem HSTS, CSP, `X-Frame-Options`, `X-Content-Type-Options`, `UseHttpsRedirection`. | Middleware de headers + HTTPS no host (alinha WF-19). |

MJ-02 do WF-12 (CPF cru no canónico): **rebaixado**. `CanonicalMapper` persiste `PiiMask.Cpf`. Residual: API não re-aplica máscara (MN-SEC-05).

### MINOR

| ID | OWASP | Onde | Descrição | Fix |
|----|-------|------|-----------|-----|
| MN-SEC-01 | A07 | `src/lib/auth.ts` | JWT em `sessionStorage`. XSS = sessão. | Aceitável no lab. Produção: BFF + cookie HttpOnly SameSite=strict **ou** SPA + CSP estrito. |
| MN-SEC-02 | A05 | `Program.cs:40` | CORS origem hardcoded. | `Cors__Origins`. |
| MN-SEC-03 | A01 | `ScopeClaims.cs:19-26` | `consumer` lê authority sem scope. | `consumer` **e** `api:decision:read`. |
| MN-SEC-04 | A01 | `kas-ids.ts` | URL da jornada KAAS no client. | Tirar do bundle. |
| MN-SEC-05 | A02 | `DocumentsEndpoints.GetCanonical` | CPF da BD sem re-máscara na borda. | `PiiMask.Cpf` no DTO. |
| MN-SEC-06 | A04 | Rate limiter | 429 só em authority. | Política em POST documents e evaluate. |
| MN-SEC-07 | A07 | `AuthExtensions` | Sem `ValidAlgorithms` pinado em HS256. | Uma linha. |

Itens operacionais já no WF-12 (paginação, outbox `SKIP LOCKED`, idempotência persistida) **não** reabertos aqui — não são o recorte do OS.

---

## AÇÕES REQUERIDAS

### Neste chat (WF-18)

- [x] OWASP: secrets, path traversal, JWT, CORS, upload content-type
- [x] Confirmar `KAS_API_KEY` ausente do bundle Next
- [x] Relatório `_bmad-output/reviews/REVIEW_WF-18_security.md`
- [x] Sem features
- [x] C4 = 6.6 ≥ 6 → Pierre **não** bloqueia

### PRs de correção (`wf-18-security`) — antes de tratar como produção

1. Jail do blob (`StorageRoot`)
2. MIME ∧ extensão ∧ magic bytes
3. Password fora do `appsettings.json` base
4. Swagger só Development
5. Headers de segurança + HTTPS no host (pode ir com WF-19)
6. Limpar `KAS_*` do `.env.local` do Next + User Secrets no Worker + rotação da chave KAAS
7. (opcional no mesmo PR) CORS via env, pin HS256, scope do consumer, `PiiMask` na borda canónica

### Fora deste review

- Histórico Git de secrets — pendente (`git.exe` ausente no PATH, igual WF-12)
- SCA NuGet/npm — não reexecutado
- Compose 4 containers — WF-19
- Go/No-Go demo — WF-21 (Pierre)

---

## STRIDE (resumo — upload + KAAS + decisão)

| Ameaça | Superfície | Mitigação actual | Gap |
|--------|------------|------------------|-----|
| Spoofing | JWT HS256 | ValidateIssuer/Audience/Key | Token colado na UI; sem IdP |
| Tampering | Blob / canónico | Hash SHA-256 no save; audit WORM | Path de read não jaulado |
| Repudiation | Audit | Interceptor append-only + correlation | — |
| Info disclosure | Swagger, CPF, KAAS URL | PII no mapper/log | Swagger aberto; URL KAAS no client; `.env.local` legado |
| DoS | Upload 50 MB | Teto Kestrel + 422 | Sem rate limit no POST documents |
| Elevation | RBAC | Policies por rota | consumer sem scope obrigatório |

---

## VEREDITO

| Critério | Valor | Efeito |
|----------|-------|--------|
| C4 Segurança | **6.6 / 10** | **Pierre não bloqueia** (limiar 6.0) |
| `KAS_API_KEY` no bundle Next | Ausente | Pass |
| Secrets de produção no tree versionável | Não (password docker no JSON base = lab) | Pass com MJ-SEC-02 |
| Path traversal blob | Aberto (pré-condição: tamper DB) | PR correção |
| Upload content-type | Bypass OR | PR correção |
| JWT resource server | Adequado | Pass |
| CORS lab | Allowlist `:3000` | Pass |

**Security Guardian não autoriza Go de produção.** Autoriza continuar a esteira (WF-19 compose) **em paralelo** com PRs de correção em `wf-18-security`. WF-21 (Pierre E2E) deve recusar Go se MJ-SEC-03 e MJ-SEC-04 continuarem abertos no host de demo exposto.

Próximo: DevOps Master **WF-19** (compose) e/ou PR `wf-18-security` com os seis MAJOR.
