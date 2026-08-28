using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BbfFirmasPoderes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandPartnerDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "documento",
                table: "people",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "mandate_end",
                table: "people",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "mandate_start",
                table: "people",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "person_type",
                table: "people",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "quotas",
                table: "people",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rg",
                table: "people",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "people",
                keyColumn: "person_id",
                keyValue: "p1",
                columns: new[] { "documento", "mandate_end", "mandate_start", "person_type", "quotas", "rg" },
                values: new object[] { "", null, null, "", null, "" });

            migrationBuilder.UpdateData(
                table: "people",
                keyColumn: "person_id",
                keyValue: "p2",
                columns: new[] { "documento", "mandate_end", "mandate_start", "person_type", "quotas", "rg" },
                values: new object[] { "", null, null, "", null, "" });

            migrationBuilder.UpdateData(
                table: "people",
                keyColumn: "person_id",
                keyValue: "p3",
                columns: new[] { "documento", "mandate_end", "mandate_start", "person_type", "quotas", "rg" },
                values: new object[] { "", null, null, "", null, "" });

            migrationBuilder.UpdateData(
                table: "people",
                keyColumn: "person_id",
                keyValue: "p4",
                columns: new[] { "documento", "mandate_end", "mandate_start", "person_type", "quotas", "rg" },
                values: new object[] { "", null, null, "", null, "" });

            migrationBuilder.UpdateData(
                table: "people",
                keyColumn: "person_id",
                keyValue: "p5",
                columns: new[] { "documento", "mandate_end", "mandate_start", "person_type", "quotas", "rg" },
                values: new object[] { "", null, null, "", null, "" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "documento",
                table: "people");

            migrationBuilder.DropColumn(
                name: "mandate_end",
                table: "people");

            migrationBuilder.DropColumn(
                name: "mandate_start",
                table: "people");

            migrationBuilder.DropColumn(
                name: "person_type",
                table: "people");

            migrationBuilder.DropColumn(
                name: "quotas",
                table: "people");

            migrationBuilder.DropColumn(
                name: "rg",
                table: "people");
        }
    }
}
