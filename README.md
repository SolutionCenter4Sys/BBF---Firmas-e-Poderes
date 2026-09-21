# BBF Firmas e Poderes

Orquestrador de firmas e poderes: upload de contrato social → pipeline KAAS → modelo canônico → decisão (APROVADO / REPROVADO / MANUAL) → trilha de auditoria WORM.

Stack local: **PostgreSQL 16 + API .NET 8 + Worker KAAS + Next.js 14**. Sem AWS neste recorte.

## Subir o ambiente

`compose.yaml` (WF-19) lê `.env.example` (senhas demo só ali — não estão no código).

```powershell
docker compose down -v
docker compose up -d --build
docker compose ps
```

Aceite: 4 serviços healthy. Conferir:

```powershell
curl -i http://localhost:8080/health/ready
curl -I http://localhost:3000
```

UI: http://localhost:3000 · API: http://localhost:8080 · Swagger (Development): http://localhost:8080/swagger

`Database__MigrateOnStartup=true` (já no `.env.example` e no serviço `api`) aplica migrations + seed no boot, inclusive após `down -v`.

Engine Docker Linux precisa estar verde (WSL 2). Sem engine, dá para desenvolver a API na máquina com `dotnet run --project backend/src/BbfFirmasPoderes.Api` (JWT de `appsettings.Development.json`; senhas demo ainda vêm do env).

## Login demo

Senhas **somente** em `.env.example` (`DemoUsers__OperadorPassword` / `DemoUsers__AuditorPassword`). Não estão em `appsettings*.json` nem no C#.

| Perfil | Email | Uso |
|--------|--------|-----|
| operador | `ana.silva@bbf.com.br` | upload, lista, evaluate, filas |
| auditor | `auditor.interno@bbf.com.br` | trilha `/audit`, replay |

```powershell
# PowerShell (aspas simples no body)
Invoke-RestMethod http://localhost:8080/v1/auth/login -Method Post -ContentType "application/json" -Body '{"email":"ana.silva@bbf.com.br","password":"OperadorDemo!2026"}'
```

```bash
curl -s http://localhost:8080/v1/auth/login -H "Content-Type: application/json" \
  -d '{"email":"ana.silva@bbf.com.br","password":"OperadorDemo!2026"}'
```

Resposta: `{ accessToken, tokenType, expiresIn, role, email }`. Cole o `accessToken` no campo **JWT operador** do dashboard (sessionStorage `bbf.access_token`). Auditor: mesma rota com o e-mail/senha de auditor, colar em `/audit`.

Sem token → **401**. Senha errada → **401**. Perfil sem permissão → **403**.

## Seed (após compose up)

Três documentos de `src/lib/mocks.ts`:

| ID | Empresa | Status doc | Decisão |
|----|---------|------------|---------|
| `doc_001` | ACME Indústrias LTDA | `decidido` | `dec_001` **APROVADO** |
| `doc_004` | Delta EIRELI | `decidido` | `dec_002` **REPROVADO** |
| `doc_003` | Gama Investimentos LTDA | `revisao_humana` | `dec_003` **MANUAL** |

```powershell
docker compose exec db psql -U bbf -d bbf_firmas -c "SELECT document_id, razao_social, status FROM documents ORDER BY document_id;"
```

## Roteiro da demo (8 passos)

1. **Stack** — `docker compose down -v && docker compose up -d --build`. Esperar healthy. `curl http://localhost:8080/health/ready` → 200.
2. **Login operador** — `POST /v1/auth/login` com `ana.silva@bbf.com.br`. Abrir http://localhost:3000 e colar o JWT.
3. **Dashboard** — lista com ACME, Delta e Gama (Gama em revisão humana).
4. **APROVADO** — abrir `doc_001` ACME: 3 sócios, 2 poderes, decisão `dec_001` (Diretor + Procurador, R$ 500.000).
5. **REPROVADO** — abrir `doc_004` Delta: Carlos Pereira **inativo**, `dec_002` (procuração revogada).
6. **MANUAL** — abrir `doc_003` Gama / fila `/manual-queue` ou `/review-queue`: NER 66% < 75%, cláusula ambígua.
7. **API consumidora** — `/api-helper` ou `GET /v1/authority/decision?cnpj=12.345.678/0001-90&operation=movimentacao_financeira&signers=p1` (Bearer operador ou consumer) → **APROVADO**.
8. **Auditoria** — login auditor, colar JWT em `/audit`, filtrar `documentId=doc_001` (ou `corr_a1b2c3`). Replay `POST /v1/decision/dec_001/replay` com Bearer auditor. Operador nesta rota → **403**.

## Fora deste recorte

AWS (ECS, RDS, Secrets Manager), Junta Comercial HTTP real, IdP corporativo. Detalhe do backend: `backend/README.md`. Contrato: `docs/arquitetura/WF-03_MAPA_PARIDADE_API.md`. ER: `docs/arquitetura/ER.md`.
