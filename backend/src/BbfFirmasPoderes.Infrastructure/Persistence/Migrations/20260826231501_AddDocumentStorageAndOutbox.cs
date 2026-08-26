using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BbfFirmasPoderes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentStorageAndOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "content_type",
                table: "documents",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "storage_path",
                table: "documents",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    outbox_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    document_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.outbox_id);
                    table.ForeignKey(
                        name: "FK_outbox_messages_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "document_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "documents",
                keyColumn: "document_id",
                keyValue: "doc_001",
                columns: new[] { "content_type", "storage_path" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_document_id",
                table: "outbox_messages",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_processed_at",
                table: "outbox_messages",
                column: "processed_at",
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_type",
                table: "outbox_messages",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "content_type",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "storage_path",
                table: "documents");
        }
    }
}
