using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtoRehber.Migrations
{
    /// <inheritdoc />
    public partial class AddCarSpecs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BodyType",
                table: "Cars",
                type: "character varying(24)",
                maxLength: 24,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Condition",
                table: "Cars",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Drivetrain",
                table: "Cars",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EngineDisplacementCc",
                table: "Cars",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FastChargeMinutes",
                table: "Cars",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuelType",
                table: "Cars",
                type: "character varying(24)",
                maxLength: 24,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PowerHp",
                table: "Cars",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RangeKm",
                table: "Cars",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Transmission",
                table: "Cars",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearEnd",
                table: "Cars",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearStart",
                table: "Cars",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BodyType", "Condition", "Drivetrain", "EngineDisplacementCc", "FastChargeMinutes", "FuelType", "PowerHp", "RangeKm", "Transmission", "YearEnd", "YearStart" },
                values: new object[] { null, null, null, null, null, null, null, null, null, null, null });

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "BodyType", "Condition", "Drivetrain", "EngineDisplacementCc", "FastChargeMinutes", "FuelType", "PowerHp", "RangeKm", "Transmission", "YearEnd", "YearStart" },
                values: new object[] { null, null, null, null, null, null, null, null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BodyType",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "Condition",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "Drivetrain",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "EngineDisplacementCc",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "FastChargeMinutes",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "FuelType",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "PowerHp",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "RangeKm",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "Transmission",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "YearEnd",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "YearStart",
                table: "Cars");
        }
    }
}
