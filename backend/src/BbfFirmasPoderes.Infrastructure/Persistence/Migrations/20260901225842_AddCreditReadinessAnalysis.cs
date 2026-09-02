using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BbfFirmasPoderes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditReadinessAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "analysis_json",
                table: "documents",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "credit_readiness_classification",
                table: "documents",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "credit_readiness_justification",
                table: "documents",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "credit_readiness_recommendation",
                table: "documents",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "credit_readiness_score",
                table: "documents",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "documents",
                keyColumn: "document_id",
                keyValue: "doc_001",
                columns: new[] { "analysis_json", "credit_readiness_classification", "credit_readiness_justification", "credit_readiness_recommendation", "credit_readiness_score" },
                values: new object[] { "{}", null, null, null, null });

            migrationBuilder.UpdateData(
                table: "documents",
                keyColumn: "document_id",
                keyValue: "doc_003",
                columns: new[] { "analysis_json", "credit_readiness_classification", "credit_readiness_justification", "credit_readiness_recommendation", "credit_readiness_score" },
                values: new object[] { "{}", null, null, null, null });

            migrationBuilder.UpdateData(
                table: "documents",
                keyColumn: "document_id",
                keyValue: "doc_004",
                columns: new[] { "analysis_json", "credit_readiness_classification", "credit_readiness_justification", "credit_readiness_recommendation", "credit_readiness_score" },
                values: new object[] { "{}", null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "analysis_json",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "credit_readiness_classification",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "credit_readiness_justification",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "credit_readiness_recommendation",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "credit_readiness_score",
                table: "documents");
        }
    }
}
