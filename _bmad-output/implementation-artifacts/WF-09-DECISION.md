# WF-09 — Decision evaluate + replay

**Workflow:** WF-09/22  
**Agente:** Ricardo  
**Branch:** `wf-09-decision`  
**Data:** 2026-08-27  
**Depende:** WF-08

## Entrega

- `POST /v1/decision/evaluate` (RBAC `decision:read` = operador/admin) recebe `EvaluationRequest` e devolve `DecisionRecord`: `APROVADO` | `REPROVADO` | `MANUAL` + motivos + evidências + snapshot `versions` (rules 1.2.0, canonical 1.0.0, aiPrompt, aiModel).
- `POST /v1/decision/{id}/replay` (RBAC `decision:replay` = auditor/consumer/admin) devolve o snapshot persistido. Sem reconsulta de fonte oficial. Resposta idêntica à avaliação original.
- Regras mínimas no Domain (`DecisionRuleCatalog` + `DecisionEngine`), alinhadas a `dmnRules` do mock:
  - **RN01** — signatário ATIVO no canônico (não Junta live).
  - **RN02** — modo isolada vs conjunta.
  - **RN03** — valor ≤ limite do poder que casou o modo (ACME: isolada R$ 500.000 / conjunta R$ 5.000.000).
  - **RN04** — vigência do poder em `asOf`.
  - **TH01** — min(OCR, IAGen, NER) &lt; 0,75 → MANUAL.
- Persistência em `decisions` + evento `decision.evaluated`. Documento → `decidido` (ou `revisao_humana` se MANUAL).

## Aceite

| Check | Resultado |
|---|---|
| `dotnet test backend/` | **57/57** (9 regras ACME + 7 API evaluate/replay) |
| Evaluate ACME `movimentacao_financeira` Diretor R$ 500.000 | `APROVADO`, evidência `pw1` (página 4, isolada) |
| Replay do `decisionId` | JSON idêntico ao evaluate |
| Replay `dec_001` (seed) | snapshot original, `latencyMs` 1287 |
| Junta Comercial | **não** chamada (WF-11) |

**Git:** nesta máquina `git.exe` não está no PATH (igual WF-03). Branch `wf-09-decision` fica para o próximo chat com Git no PATH. Commit só se o usuário pedir.

## Fora de escopo (OS)

Junta Comercial real, health de fontes, circuit breaker (WF-11). GET `/v1/authority/decision` (consumidor). RN05/RN06 (não ativas no mock).
