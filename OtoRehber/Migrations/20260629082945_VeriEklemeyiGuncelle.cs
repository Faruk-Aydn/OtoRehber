using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtoRehber.Migrations
{
    /// <inheritdoc />
    public partial class VeriEklemeyiGuncelle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Cars",
                columns: new[] { "Id", "Brand", "Cons", "Engine", "EstimatedMaintenanceCostEUR", "ExpertSummary", "ModelName", "PriceRange", "Pros", "ReliabilityScore", "Segment" },
                values: new object[] { 1, "Volkswagen", "[]", "1.6 TDI", 0, "C segmentinin referans modeli.", "Golf", "800k - 1.2M TL", "[]", 8.0, "C" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
