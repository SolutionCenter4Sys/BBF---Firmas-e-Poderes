using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BbfFirmasPoderes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    document_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    cnpj = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    razao_social = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    tipo_societario = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    uploaded_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    file_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    paginas = table.Column<int>(type: "integer", nullable: false),
                    confianca_ocr = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    confianca_iagen = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    confianca_ner = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.document_id);
                    table.CheckConstraint("ck_documents_status", "status IN ('pendente','processando_ocr','processando_iagen','processando_ner','canonico_pronto','validacao_oficial','decidido','revisao_humana','falha')");
                });

            migrationBuilder.CreateTable(
                name: "decisions",
                columns: table => new
                {
                    decision_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    document_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    cnpj = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    operacao = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    signatarios_solicitados = table.Column<string[]>(type: "text[]", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    motivos = table.Column<string[]>(type: "text[]", nullable: false),
                    evidencias = table.Column<string>(type: "jsonb", nullable: false),
                    version_rules = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    version_canonical = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    version_ai_prompt = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    version_ai_model = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    evaluated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    latency_ms = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_decisions", x => x.decision_id);
                    table.CheckConstraint("ck_decisions_status", "status IN ('APROVADO','REPROVADO','MANUAL')");
                    table.ForeignKey(
                        name: "FK_decisions_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "document_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "kas_runs",
                columns: table => new
                {
                    kas_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    document_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    execution_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    action = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    http_status = table.Column<int>(type: "integer", nullable: false),
                    ok = table.Column<bool>(type: "boolean", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kas_runs", x => x.kas_run_id);
                    table.CheckConstraint("ck_kas_runs_action", "action IN ('ingest','result')");
                    table.ForeignKey(
                        name: "FK_kas_runs_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "document_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "people",
                columns: table => new
                {
                    person_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    document_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    nome = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    cpf = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    qualificacao = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    cargo = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_people", x => x.person_id);
                    table.CheckConstraint("ck_people_status", "status IN ('ativo','inativo')");
                    table.ForeignKey(
                        name: "FK_people_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "document_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "powers",
                columns: table => new
                {
                    power_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    document_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    pessoa = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    operacao = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    limite_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    limite_value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    limite_expression = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    modo_assinatura_tipo = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    modo_assinatura_n = table.Column<int>(type: "integer", nullable: true),
                    modo_assinatura_m = table.Column<int>(type: "integer", nullable: true),
                    modo_assinatura_qualificacoes = table.Column<string[]>(type: "text[]", nullable: true),
                    vigencia_from = table.Column<DateOnly>(type: "date", nullable: false),
                    vigencia_to = table.Column<DateOnly>(type: "date", nullable: true),
                    source_page = table.Column<int>(type: "integer", nullable: false),
                    source_offset_start = table.Column<int>(type: "integer", nullable: false),
                    source_offset_end = table.Column<int>(type: "integer", nullable: false),
                    source_snippet = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_powers", x => x.power_id);
                    table.CheckConstraint("ck_powers_modo_assinatura", "modo_assinatura_tipo IN ('isolada','conjunta')");
                    table.ForeignKey(
                        name: "FK_powers_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "document_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    event_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    document_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    decision_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    actor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    details = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.event_id);
                    table.ForeignKey(
                        name: "FK_audit_events_decisions_decision_id",
                        column: x => x.decision_id,
                        principalTable: "decisions",
                        principalColumn: "decision_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_audit_events_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "document_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "documents",
                columns: new[] { "document_id", "cnpj", "confianca_iagen", "confianca_ner", "confianca_ocr", "correlation_id", "file_hash", "file_name", "paginas", "razao_social", "status", "tipo_societario", "uploaded_at", "uploaded_by" },
                values: new object[] { "doc_001", "12.345.678/0001-90", 0.92m, 0.94m, 0.97m, "corr_a1b2c3", "a3f2c4...e9d1", "contrato-social-acme.pdf", 12, "ACME Indústrias LTDA", "decidido", "LTDA", new DateTimeOffset(new DateTime(2026, 4, 29, 14, 22, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "ana.silva@bbf.com.br" });

            migrationBuilder.InsertData(
                table: "audit_events",
                columns: new[] { "event_id", "actor", "correlation_id", "decision_id", "details", "document_id", "occurred_at", "type" },
                values: new object[] { "ev_0001", "ana.silva@bbf.com.br", "corr_a1b2c3", null, "Upload de contrato-social-acme.pdf (12 páginas, 2.3MB)", "doc_001", new DateTimeOffset(new DateTime(2026, 4, 29, 14, 22, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "document.uploaded" });

            migrationBuilder.InsertData(
                table: "decisions",
                columns: new[] { "decision_id", "cnpj", "document_id", "evaluated_at", "evidencias", "latency_ms", "motivos", "operacao", "signatarios_solicitados", "status", "version_ai_model", "version_ai_prompt", "version_canonical", "version_rules" },
                values: new object[] { "dec_001", "12.345.678/0001-90", "doc_001", new DateTimeOffset(new DateTime(2026, 4, 29, 14, 25, 32, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "[{\"type\":\"documento\",\"trace\":{\"page\":4,\"offsetStart\":1500,\"offsetEnd\":1750,\"snippet\":\"...acima desse valor, exigir-se-á assinatura conjunta de Diretor e Procurador...\"},\"detalhe\":\"Cláusula 8 do Contrato Social\"},{\"type\":\"fonte_oficial\",\"fonte\":\"Junta Comercial SP\",\"detalhe\":\"Quadro societário consultado em 2026-04-29; ambos os sócios constam como ATIVOS.\"}]", 1287, new[] { "Operação dentro do limite de R$ 500.000 com modo conjunto Diretor + Procurador (Regra RN02 + RN03 v1.2.0).", "Sócios João da Silva e Maria Souza confirmados como ATIVOS na Junta Comercial em 2026-04-29 14:22 UTC.", "Procuração vigente (validFrom 2024-01-01, sem revogação)." }, "Movimentação financeira — R$ 500.000", new[] { "João da Silva (Diretor)", "Maria Souza (Procuradora)" }, "APROVADO", "gemini-1.5-pro", "leitura-contrato-social@2.1.0", "1.0.0", "1.2.0" });

            migrationBuilder.InsertData(
                table: "kas_runs",
                columns: new[] { "kas_run_id", "action", "correlation_id", "document_id", "duration_ms", "execution_id", "file_name", "http_status", "occurred_at", "ok", "payload" },
                values: new object[] { new Guid("aaaaaaaa-bbbb-cccc-dddd-000000000001"), "ingest", "corr_a1b2c3", "doc_001", 14200, "exec_acme_001", "contrato-social-acme.pdf", 200, new DateTimeOffset(new DateTime(2026, 4, 29, 14, 22, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "{\"ok\":true,\"kasStatus\":200,\"action\":\"ingest\",\"correlationId\":\"corr_a1b2c3\",\"executionId\":\"exec_acme_001\",\"fileName\":\"contrato-social-acme.pdf\",\"journey\":\"testes-firmas-e-poderes\",\"mode\":\"sync\",\"body\":{\"status\":\"completed\",\"message\":\"seed ACME doc_001\"}}" });

            migrationBuilder.InsertData(
                table: "people",
                columns: new[] { "person_id", "cargo", "cpf", "document_id", "nome", "qualificacao", "status" },
                values: new object[,]
                {
                    { "p1", "Diretor", "111.222.***-44", "doc_001", "João da Silva", "Sócio-administrador", "ativo" },
                    { "p2", "Procuradora", "222.333.***-55", "doc_001", "Maria Souza", "Sócia", "ativo" },
                    { "p3", "Conselheiro", "333.444.***-66", "doc_001", "Carlos Pereira", "Sócio", "inativo" }
                });

            migrationBuilder.InsertData(
                table: "powers",
                columns: new[] { "power_id", "document_id", "limite_currency", "limite_expression", "limite_value", "modo_assinatura_m", "modo_assinatura_n", "modo_assinatura_qualificacoes", "modo_assinatura_tipo", "operacao", "pessoa", "source_offset_end", "source_offset_start", "source_page", "source_snippet", "vigencia_from", "vigencia_to" },
                values: new object[,]
                {
                    { "pw1", "doc_001", "BRL", "até R$ 500.000,00", 500000m, null, null, null, "isolada", "Movimentação financeira", "Diretor", 1480, 1280, 4, "...o Diretor poderá assinar isoladamente movimentações até R$ 500.000,00...", new DateOnly(2024, 1, 1), null },
                    { "pw2", "doc_001", "BRL", "acima de R$ 500.000,00", 5000000m, 2, 2, new[] { "Diretor", "Procurador" }, "conjunta", "Movimentação financeira", "Diretor + Procurador", 1750, 1500, 4, "...acima desse valor, exigir-se-á assinatura conjunta de Diretor e Procurador...", new DateOnly(2024, 1, 1), null }
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_correlation_id",
                table: "audit_events",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_decision_id",
                table: "audit_events",
                column: "decision_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_document_id",
                table: "audit_events",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_occurred_at",
                table: "audit_events",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_decisions_cnpj",
                table: "decisions",
                column: "cnpj");

            migrationBuilder.CreateIndex(
                name: "ix_decisions_document_id",
                table: "decisions",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_documents_cnpj",
                table: "documents",
                column: "cnpj");

            migrationBuilder.CreateIndex(
                name: "ix_documents_correlation_id",
                table: "documents",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_documents_file_hash",
                table: "documents",
                column: "file_hash");

            migrationBuilder.CreateIndex(
                name: "ix_documents_status",
                table: "documents",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_kas_runs_correlation_id",
                table: "kas_runs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_kas_runs_document_id",
                table: "kas_runs",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_kas_runs_execution_id",
                table: "kas_runs",
                column: "execution_id");

            migrationBuilder.CreateIndex(
                name: "ix_people_document_id",
                table: "people",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_powers_document_id",
                table: "powers",
                column: "document_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_events");

            migrationBuilder.DropTable(
                name: "kas_runs");

            migrationBuilder.DropTable(
                name: "people");

            migrationBuilder.DropTable(
                name: "powers");

            migrationBuilder.DropTable(
                name: "decisions");

            migrationBuilder.DropTable(
                name: "documents");
        }
    }
}
