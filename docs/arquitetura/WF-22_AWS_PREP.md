# WF-22 — Preparar AWS (sem deploy)

**Workflow:** WF-22/22  
**Agente:** DevOps Master  
**Branch sugerida:** `wf-22-aws-prep`  
**Data:** 27/08/2026  
**Depende:** WF-21 Go KAAS (`REVIEW_WF-21_e2e_go.md`)

**Não fazer:** `aws ecs create-service`, `terraform apply`, push em `main`, registry push de imagens com secrets.

## Alvo recomendado (lab → conta BBF)

| Peça local (compose) | AWS |
|---|---|
| `bbf-db` postgres:16 | **RDS PostgreSQL 16** (privado, Multi-AZ depois do lab) |
| `bbf-api` :8080 | **ECS Fargate** (serviço `api`) **ou** App Runner |
| `bbf-worker` (sem porta host) | **ECS Fargate** serviço `worker` (mesmo task role; volume EFS ou S3 para blobs) |
| `bbf-web` Next standalone :3000 | **ECS Fargate** serviço `web` **ou** App Runner |
| `.env` / `Kas__ApiKey`, JWT | **Secrets Manager** (nunca imagem, nunca task def plaintext) |
| volume `bbf_docs` | **EFS** montado em api+worker **ou** S3 + adapter (hoje filesystem) |

Rede: API e worker só falam com RDS e KAAS (`Kas__RunUrl`) via NAT. Web público (ALB). Worker **sem** listener público.

## Secrets Manager (nomes lógicos)

| Secret | Env no container |
|---|---|
| `bbf/kas-api-key` | `Kas__ApiKey` **só no worker** |
| `bbf/jwt-signing-key` | `Jwt__SigningKey` (API) |
| `bbf/postgres` | `ConnectionStrings__Postgres` (api + worker) |
| demo users | `DemoUsers__*` só se o lab de login continuar |

Proibido: `NEXT_PUBLIC_` com chave KAAS. Front só `NEXT_PUBLIC_API_URL` (URL HTTPS da API).

## Checklist de env (produção)

- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Swagger **off** (já: `UseBbfSwagger` só Development)
- [ ] JWT issuer/audience reais; signing key ≥ 32 bytes
- [ ] CORS origens do hostname do `web`, não `*`
- [ ] `Documents__StorageRoot` no volume persistente
- [ ] `Database__MigrateOnStartup` — decidir: job one-shot vs boot
- [ ] Health: ALB `/health/ready` (API). Worker probe interno
- [ ] Logs: CloudWatch; máscara PII já no Serilog
- [ ] Sem `Password=` em `appsettings.json` da imagem

## Artefacto local

`compose.prod.yaml` — esqueleto **sem** secrets. Usar `env_file: .env.prod` (gitignorado) ou interpolação `${VAR}`. **Não** aplicar na AWS a partir deste ficheiro; serve de contrato de runtime.

## Passos posteriores (outro chat, ordem explícita)

1. Conta + VPC + RDS (DPO/segurança BBF).  
2. ECR push das imagens `bbf-firmas-api` / `worker` / `web`.  
3. Task defs + Secrets Manager.  
4. Só então `create-service` / terraform **com ordem do usuário**.
