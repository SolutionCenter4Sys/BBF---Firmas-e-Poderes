# WF-20 — Seed de demonstração + README operação

**Workflow:** WF-20/22  
**Agente:** Ricardo  
**Branch:** `wf-20-seed`  
**Data:** 2026-08-27  
**Depende:** WF-04 (ACME), WF-19 (compose 4 containers)

## Entrega

- Seed espelha `src/lib/mocks.ts`: **ACME** `doc_001` APROVADO (já WF-04) + **Delta** `doc_004` / `dec_002` REPROVADO + **Gama** `doc_003` / `dec_003` MANUAL (`revisao_humana`).
- Migration `AddDemoSeed`.
- `POST /v1/auth/login` anônimo. Usuários demo operador/auditor. Senhas **somente** em `.env.example` (`DemoUsers__*Password`).
- `Database__MigrateOnStartup=true` → API aplica migrations no boot (volume Postgres novo).
- README raiz: compose up, login, roteiro 8 passos.

## Aceite

| Check | Resultado |
|---|---|
| `dotnet test backend/` | 87 testes (7 novos demo/login) |
| Seed `doc_001` / `doc_004` / `doc_003` | APROVADO / REPROVADO / MANUAL |
| Login operador + auditor | JWT; auditor lê `/v1/audit/trail`; operador → 403 |
| Senhas no C# / `appsettings*.json` | **ausentes** |
| AWS | **não** |

## Fora de escopo (OS)

AWS. `compose.yaml` é WF-19 (já no repo). Aceite `down -v && up` exige Docker engine Linux (WSL 2). Sem engine: testes in-memory + `dotnet run` para login.
