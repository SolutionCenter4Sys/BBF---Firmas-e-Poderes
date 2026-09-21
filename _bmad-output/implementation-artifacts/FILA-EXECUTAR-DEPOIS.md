# Fila — executar depois

Atualizado 27/08/2026 (Go KAAS). Compose 4 healthy. Ingest+result live **200**.

## Fechado neste host

- WF-21 KAAS: `.env` gitignorado + envelope `payload.document_url` + mapper sem arrays.
- Ensaio `doc_9fad45b12f8c4f74ac39b9e3edbbc483` → `revisao_humana`. Addendum `REVIEW_WF-21_e2e_go.md`.
- WF-22 docs: `docs/arquitetura/WF-22_AWS_PREP.md` + `compose.prod.yaml`. **Sem apply.**

## Git (ainda pendente nesta máquina)

`git.exe` continua fora do PATH. Commit só quando Git estiver visível **e** o usuário pedir. Não commitar `.env`.

## Não fazer sozinho

- `terraform apply` / `aws ecs create-service` / push registry.
- Rotacionar chave KAAS no portal (recomendado: circulou em `.env.local` legado).
