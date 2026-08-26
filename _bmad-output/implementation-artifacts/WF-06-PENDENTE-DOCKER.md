# WF-06 — pendente Docker (evidência PG)

Código e `dotnet test` (23/23) já fecham o recorte HTTP. Falta só evidência `curl` + linha `pendente` no Postgres — mesmo bloqueio WSL/engine dos WF-02/04.

Depois do engine verde:

```powershell
docker compose -f compose.dev.yaml up -d db

$env:Path = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:Path"
$env:ASPNETCORE_ENVIRONMENT = "Development"

cd backend
dotnet ef database update --project src/BbfFirmasPoderes.Infrastructure --startup-project src/BbfFirmasPoderes.Api
cd ..

dotnet run --project backend/src/BbfFirmasPoderes.Api
```

Em outro terminal, JWT operador (issuer/audience/key de Development) + upload:

```powershell
# gerar JWT com o mesmo algoritmo dos testes (HS256) e POST:
curl -i http://localhost:8080/v1/documents -H "Authorization: Bearer <jwt-operador>" -F "file=@contrato.pdf;type=application/pdf"
curl -i http://localhost:8080/v1/documents/<documentId>/status -H "Authorization: Bearer <jwt-operador>"

docker compose -f compose.dev.yaml exec db psql -U bbf -d bbf_firmas -c "SELECT document_id, status, correlation_id FROM documents WHERE status = 'pendente';"
docker compose -f compose.dev.yaml exec db psql -U bbf -d bbf_firmas -c "SELECT type, processed_at FROM outbox_messages;"
```

Aceite: GET status = `pendente`; PG `status = pendente`; outbox `processed_at` nulo.
