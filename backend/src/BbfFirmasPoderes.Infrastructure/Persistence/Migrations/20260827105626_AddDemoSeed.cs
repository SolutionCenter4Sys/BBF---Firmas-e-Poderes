using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BbfFirmasPoderes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "documents",
                columns: new[] { "document_id", "cnpj", "confianca_iagen", "confianca_ner", "confianca_ocr", "content_type", "correlation_id", "file_hash", "file_name", "paginas", "razao_social", "status", "storage_path", "tipo_societario", "uploaded_at", "uploaded_by" },
                values: new object[,]
                {
                    { "doc_003", "55.666.777/0001-22", 0.71m, 0.66m, 0.78m, null, "corr_g7h8i9", "c1d2...8e9f", "procuracao-gama.pdf", 4, "Gama Investimentos LTDA", "revisao_humana", null, "LTDA", new DateTimeOffset(new DateTime(2026, 4, 29, 9, 48, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "ana.silva@bbf.com.br" },
                    { "doc_004", "11.222.333/0001-44", 0.93m, 0.95m, 0.96m, null, "corr_d4e5f6", "d4e5...0f1a", "contrato-delta.pdf", 6, "Delta EIRELI", "decidido", null, "EIRELI", new DateTimeOffset(new DateTime(2026, 4, 28, 17, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "joao.t@bbf.com.br" }
                });

            migrationBuilder.InsertData(
                table: "decisions",
                columns: new[] { "decision_id", "cnpj", "document_id", "evaluated_at", "evidencias", "latency_ms", "motivos", "operacao", "signatarios_solicitados", "status", "version_ai_model", "version_ai_prompt", "version_canonical", "version_rules" },
                values: new object[,]
                {
                    { "dec_002", "11.222.333/0001-44", "doc_004", new DateTimeOffset(new DateTime(2026, 4, 28, 17, 33, 14, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "[{\"type\":\"fonte_oficial\",\"fonte\":\"Junta Comercial SP\",\"detalhe\":\"Status ATIVO=false desde 2025-09-12\"},{\"type\":\"documento\",\"trace\":{\"page\":2,\"offsetStart\":320,\"offsetEnd\":480,\"snippet\":\"...procuração com validade até 30/08/2025...\"},\"detalhe\":\"Cláusula 3 da Procuração\"}]", 1542, new[] { "Sócio Carlos Pereira consta como INATIVO na Junta Comercial desde 2025-09-12 (Regra RN01 v1.2.0).", "Procuração apresentada está revogada (validTo 2025-08-30)." }, "Contratação de crédito — R$ 200.000", new[] { "Carlos Pereira (Procurador)" }, "REPROVADO", "gemini-1.5-pro", "leitura-contrato-social@2.1.0", "1.0.0", "1.2.0" },
                    { "dec_003", "55.666.777/0001-22", "doc_003", new DateTimeOffset(new DateTime(2026, 4, 29, 9, 51, 8, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "[{\"type\":\"documento\",\"trace\":{\"page\":3,\"offsetStart\":800,\"offsetEnd\":1100,\"snippet\":\"...os procuradores poderão, em conjunto ou isoladamente conforme deliberação...\"},\"detalhe\":\"Cláusula ambígua — modo de assinatura não determinístico\"}]", 1102, new[] { "Confiança da extração NER abaixo do threshold (66% < 75%) — Regra de Threshold v1.0.0.", "Cláusula de poderes ambígua na página 3 — recomenda revisão jurídica." }, "Abertura de conta", new[] { "Pedro Henrique (Procurador)" }, "MANUAL", "gemini-1.5-pro", "leitura-contrato-social@2.1.0", "1.0.0", "1.2.0" }
                });

            migrationBuilder.InsertData(
                table: "people",
                columns: new[] { "person_id", "cargo", "cpf", "document_id", "nome", "qualificacao", "status" },
                values: new object[,]
                {
                    { "p4", "Procurador", "333.444.***-66", "doc_004", "Carlos Pereira", "Titular", "inativo" },
                    { "p5", "Procurador", "444.555.***-77", "doc_003", "Pedro Henrique", "Procurador", "ativo" }
                });

            migrationBuilder.InsertData(
                table: "powers",
                columns: new[] { "power_id", "document_id", "limite_currency", "limite_expression", "limite_value", "modo_assinatura_m", "modo_assinatura_n", "modo_assinatura_qualificacoes", "modo_assinatura_tipo", "operacao", "pessoa", "source_offset_end", "source_offset_start", "source_page", "source_snippet", "vigencia_from", "vigencia_to" },
                values: new object[] { "pw_delta_1", "doc_004", "BRL", "até R$ 200.000,00", 200000m, null, null, null, "isolada", "Contratação de crédito", "Titular", 480, 320, 2, "...procuração com validade até 30/08/2025...", new DateOnly(2024, 1, 1), new DateOnly(2025, 8, 30) });

            migrationBuilder.InsertData(
                table: "audit_events",
                columns: new[] { "event_id", "actor", "correlation_id", "decision_id", "details", "document_id", "occurred_at", "type" },
                values: new object[,]
                {
                    { "ev_0008", "system", "corr_d4e5f6", "dec_002", "Decisão REPROVADO (sócio inativo)", "doc_004", new DateTimeOffset(new DateTime(2026, 4, 28, 17, 33, 14, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "decision.evaluated" },
                    { "ev_0009", "system", "corr_g7h8i9", "dec_003", "Decisão MANUAL (baixa confiança)", "doc_003", new DateTimeOffset(new DateTime(2026, 4, 29, 9, 51, 8, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "decision.evaluated" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "audit_events",
                keyColumn: "event_id",
                keyValue: "ev_0008");

            migrationBuilder.DeleteData(
                table: "audit_events",
                keyColumn: "event_id",
                keyValue: "ev_0009");

            migrationBuilder.DeleteData(
                table: "people",
                keyColumn: "person_id",
                keyValue: "p4");

            migrationBuilder.DeleteData(
                table: "people",
                keyColumn: "person_id",
                keyValue: "p5");

            migrationBuilder.DeleteData(
                table: "powers",
                keyColumn: "power_id",
                keyValue: "pw_delta_1");

            migrationBuilder.DeleteData(
                table: "decisions",
                keyColumn: "decision_id",
                keyValue: "dec_002");

            migrationBuilder.DeleteData(
                table: "decisions",
                keyColumn: "decision_id",
                keyValue: "dec_003");

            migrationBuilder.DeleteData(
                table: "documents",
                keyColumn: "document_id",
                keyValue: "doc_003");

            migrationBuilder.DeleteData(
                table: "documents",
                keyColumn: "document_id",
                keyValue: "doc_004");
        }
    }
}
