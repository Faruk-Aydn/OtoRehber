using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtoRehber.Migrations
{
    /// <inheritdoc />
    public partial class AddDataConfidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DataConfidence",
                table: "Cars",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataConfidence_ChronicIssue",
                table: "Cars",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataConfidence_Community",
                table: "Cars",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataConfidence_Maintenance",
                table: "Cars",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataConfidence_MarketData",
                table: "Cars",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataConfidence_TechnicalData",
                table: "Cars",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedUtc",
                table: "Cars",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "LastUpdatedUtc", "DataConfidence_ChronicIssue", "DataConfidence_Community", "DataConfidence_Maintenance", "DataConfidence_MarketData", "DataConfidence", "DataConfidence_TechnicalData" },
                values: new object[] { null, null, null, null, null, "Medium", null });

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "LastUpdatedUtc", "DataConfidence_ChronicIssue", "DataConfidence_Community", "DataConfidence_Maintenance", "DataConfidence_MarketData", "DataConfidence", "DataConfidence_TechnicalData" },
                values: new object[] { null, null, null, null, null, "Medium", null });

            // Mevcut katalog satırları küratörlüdür → Medium (PRD v5 §1.5). AI/otomatik satırlar
            // (Source IS NULL değilse ama 'catalog' de değilse) Unknown kalır — bilerek.
            migrationBuilder.Sql("UPDATE \"Cars\" SET \"DataConfidence\" = 'Medium' WHERE \"Source\" = 'catalog' AND \"DataConfidence\" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataConfidence",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "DataConfidence_ChronicIssue",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "DataConfidence_Community",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "DataConfidence_Maintenance",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "DataConfidence_MarketData",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "DataConfidence_TechnicalData",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "LastUpdatedUtc",
                table: "Cars");
        }
    }
}
