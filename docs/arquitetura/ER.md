# Modelo relacional — BBF Firmas e Poderes

**Workflow:** WF-04/22  
**Banco:** PostgreSQL 16  
**ORM:** EF Core 8 + Npgsql  
**Fonte:** `src/lib/mocks.ts` (`Document`, `Person`, `Power`, `DecisionRecord`, `AuditEvent`, `KasRunResult`)

Fuso da persistência: **timestamptz UTC**. Conversão para `America/Sao_Paulo` fica na borda da API (WF-05). CNPJ e CPF são `varchar` (aceitam valor mascarado). Hash do arquivo em `documents.file_hash`. Status do pipeline = string idêntica a `DocStatus` do front.

## Tabelas e chaves

| Tabela | PK | FKs |
|---|---|---|
| `documents` | `document_id` | — |
| `people` | `person_id` | `document_id` → `documents.document_id` (ON DELETE CASCADE) |
| `powers` | `power_id` | `document_id` → `documents.document_id` (ON DELETE CASCADE) |
| `decisions` | `decision_id` | `document_id` → `documents.document_id` (ON DELETE RESTRICT) |
| `audit_events` | `event_id` | `document_id` → `documents.document_id` (ON DELETE SET NULL); `decision_id` → `decisions.decision_id` (ON DELETE SET NULL) |
| `kas_runs` | `kas_run_id` | `document_id` → `documents.document_id` (ON DELETE SET NULL) |
| `outbox_messages` | `outbox_id` | `document_id` → `documents.document_id` (ON DELETE SET NULL) |

`__EFMigrationsHistory` é tabela de controle do EF Core (não é domínio).

## Diagrama ER

```mermaid
erDiagram
    documents ||--o{ people : "document_id"
    documents ||--o{ powers : "document_id"
    documents ||--o{ decisions : "document_id"
    documents ||--o{ audit_events : "document_id"
    documents ||--o{ kas_runs : "document_id"
    documents ||--o{ outbox_messages : "document_id"
    decisions ||--o{ audit_events : "decision_id"

    documents {
        varchar document_id PK
        varchar file_name
        varchar cnpj "mascarável"
        varchar razao_social
        varchar tipo_societario "LTDA | S.A. | EIRELI"
        timestamptz uploaded_at "UTC"
        varchar uploaded_by
        varchar status "DocStatus string"
        varchar file_hash
        varchar content_type "nullable"
        varchar storage_path "volume data/docs"
        int paginas
        numeric confianca_ocr
        numeric confianca_iagen
        numeric confianca_ner
        varchar correlation_id
    }

    people {
        varchar person_id PK
        varchar document_id FK
        varchar nome
        varchar cpf "mascarável"
        varchar qualificacao
        varchar cargo
        varchar status "ativo | inativo"
    }

    powers {
        varchar power_id PK
        varchar document_id FK
        varchar pessoa
        varchar operacao
        varchar limite_currency
        numeric limite_value
        varchar limite_expression
        varchar modo_assinatura_tipo "isolada | conjunta"
        int modo_assinatura_n
        int modo_assinatura_m
        text_array modo_assinatura_qualificacoes
        date vigencia_from
        date vigencia_to
        int source_page
        int source_offset_start
        int source_offset_end
        varchar source_snippet
    }

    decisions {
        varchar decision_id PK
        varchar document_id FK
        varchar cnpj "mascarável"
        varchar operacao
        text_array signatarios_solicitados
        varchar status "APROVADO | REPROVADO | MANUAL"
        text_array motivos
        jsonb evidencias
        varchar version_rules
        varchar version_canonical
        varchar version_ai_prompt
        varchar version_ai_model
        timestamptz evaluated_at "UTC"
        int latency_ms
    }

    audit_events {
        varchar event_id PK
        varchar correlation_id
        varchar document_id FK "nullable"
        varchar decision_id FK "nullable"
        varchar type
        varchar actor
        timestamptz occurred_at "UTC (mocks.timestamp)"
        varchar details
    }

    kas_runs {
        uuid kas_run_id PK
        varchar correlation_id
        varchar document_id FK "nullable"
        varchar execution_id
        varchar action "ingest | result"
        varchar file_name
        int http_status
        bool ok
        timestamptz occurred_at "UTC"
        int duration_ms
        jsonb payload "retorno bruto KAAS"
    }

    outbox_messages {
        uuid outbox_id PK
        varchar type "document.uploaded"
        jsonb payload
        timestamptz created_at
        timestamptz processed_at "null = pendente worker"
        varchar document_id FK "nullable"
        varchar correlation_id
    }
```

## Enums string (check constraints)

| Coluna | Valores |
|---|---|
| `documents.status` | `pendente`, `processando_ocr`, `processando_iagen`, `processando_ner`, `canonico_pronto`, `validacao_oficial`, `decidido`, `revisao_humana`, `falha` |
| `people.status` | `ativo`, `inativo` |
| `powers.modo_assinatura_tipo` | `isolada`, `conjunta` |
| `decisions.status` | `APROVADO`, `REPROVADO`, `MANUAL` |
| `kas_runs.action` | `ingest`, `result` |

## Seed

Migration `AddDomainModel` insere o documento **ACME** (`doc_001` de `mocks.ts`): 3 sócios, 2 poderes, decisão `dec_001`, evento `ev_0001`, 1 `kas_runs` com payload jsonb de laboratório (sem chamada KAAS live).

Migration `AddDocumentStorageAndOutbox` (WF-06): colunas `documents.content_type` / `documents.storage_path` + tabela `outbox_messages`. Blob no filesystem (`data/docs`), não bytea.
