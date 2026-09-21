# ADDENDUM WF-21 — Go KAAS live (27/08 tarde, após PIVOT)

**Reviewer:** execução local pós-reabertura Pierre  
**Data:** 27/08/2026  
**Depende:** `REVIEW_WF-21_e2e.md` (PIVOT só por KAAS live)

## Decisão HTML

| Critério | Tarde (Pierre) | Este addendum |
|----------|----------------|---------------|
| Compose | Go | Go (inalterado) |
| Upload | Go | Go |
| **KAAS** | **Pivot** | **Go** |
| Decisão | Go | Go |
| Audit | Go | Go |
| AWS | Fora | Fora (WF-22 só documentação, sem apply) |

**Gate local: Go** no critério KAAS. WF-22 **documentação** pode abrir. **Nenhum** terraform/apply neste host.

## Evidência live

- Worker interpola `Kas__ApiKey` de `.env` gitignorado. `Kas__ApiKey=` continua vazio no `.env.example`.
- Envelope alinhado à jornada real: `document_url` **dentro** de `payload` (root extra → 400 `"property document_url should not exist"`).
- `KasStatusMapper` não desce em arrays (fontes KAAS com `status=failed` não marcam o documento).

Ensaio PDF mínimo (`%PDF-1.4`):

| Campo | Valor |
|-------|--------|
| Upload | **202** `pendente` |
| documentId | `doc_9fad45b12f8c4f74ac39b9e3edbbc483` |
| correlationId | `corr_e072d399-1641-41db-9385-04d0f8c643dc` |
| Pipeline | `pendente` → `processando_ocr` → `processando_iagen` → **`revisao_humana`** |
| `kas_runs` ingest | HTTP **200** ok |
| `kas_runs` result | HTTP **200** ok |
| Canónico estruturado | não (PDF de 1 página vazia). `revisao_humana` = esperado WF-08 |

`/api/kas/*` permanece **410**. Chave **não** no Next (`.env.local` só `NEXT_PUBLIC_API_URL`).

## Fora deste addendum

- Rotacionar a chave KAAS se circulou em `.env.local` legado (MJ-SEC-01).
- HSTS/CSP e senha fora de `appsettings.json` (MJ-SEC-02/06) — recomendado antes de host exposto, não bloqueiam o gate HTML.
- `git.exe` / `dotnet` PATH nesta máquina: variável.

## Próximo

OS **WF-22** — preparar AWS **sem deploy**.
