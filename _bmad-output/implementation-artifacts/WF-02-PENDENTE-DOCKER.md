# WF-02 — pendência Docker (executar depois)

**Branch:** `wf-02-skeleton`  
**Status:** esqueleto .NET pronto. Migration `InitialCreate` **criada**, **não aplicada**.  
**Motivo:** Docker Desktop sem engine Linux. WSL 2 não instalado nesta máquina (`wsl -l` → "não está instalado").

## O que já está verde (sem Docker)

- `dotnet build backend/BbfFirmasPoderes.sln`
- `dotnet test backend/` (fumaça `/health/live`)
- API em `http://localhost:8080`
- `GET /health/live` → 200 Healthy

## Pré-requisito (uma vez, Admin)

```powershell
wsl --install
```

Reiniciar o Windows. Abrir Docker Desktop e esperar o engine **verde**.

SDK .NET 8 (se o terminal novo não achar `dotnet`):

```powershell
$env:Path = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:Path"
```

## Comandos (raiz do repo)

```powershell
docker compose -f compose.dev.yaml up -d db
cd backend
dotnet tool restore
dotnet ef database update --project src/BbfFirmasPoderes.Infrastructure --startup-project src/BbfFirmasPoderes.Api
cd ..
dotnet run --project backend/src/BbfFirmasPoderes.Api
```

Aceite:

- `docker compose -f compose.dev.yaml ps` → `db` healthy
- `curl -i http://localhost:8080/health/ready` → **200 Healthy**

## Docker é obrigatório?

| Trabalho | Precisa Docker? |
|----------|-----------------|
| Compilar / teste `/health/live` | Não |
| Rodar API e `/health/live` | Não |
| Aplicar migration no Postgres | Sim (`compose.dev.yaml`) |
| `/health/ready` = 200 | Sim |
| Fechar WF-02 com evidência da OS | Sim (passo 1 da VERIFICAÇÃO) |
| WF-03 matriz de contratos | Não |
| WF-04+ modelo/seed e WF-19 compose 4 containers | Sim |

Chat novo: *"executa a pendência Docker do WF-02"* + este arquivo.
