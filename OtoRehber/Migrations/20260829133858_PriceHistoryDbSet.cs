using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtoRehber.Migrations
{
    /// <inheritdoc />
    public partial class PriceHistoryDbSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarPriceHistory_Cars_CarId",
                table: "CarPriceHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CarPriceHistory",
                table: "CarPriceHistory");

            migrationBuilder.RenameTable(
                name: "CarPriceHistory",
                newName: "CarPriceHistories");

            migrationBuilder.RenameIndex(
                name: "IX_CarPriceHistory_CarId",
                table: "CarPriceHistories",
                newName: "IX_CarPriceHistories_CarId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CarPriceHistories",
                table: "CarPriceHistories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CarPriceHistories_Cars_CarId",
                table: "CarPriceHistories",
                column: "CarId",
                principalTable: "Cars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarPriceHistories_Cars_CarId",
                table: "CarPriceHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CarPriceHistories",
                table: "CarPriceHistories");

            migrationBuilder.RenameTable(
                name: "CarPriceHistories",
                newName: "CarPriceHistory");

            migrationBuilder.RenameIndex(
                name: "IX_CarPriceHistories_CarId",
                table: "CarPriceHistory",
                newName: "IX_CarPriceHistory_CarId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CarPriceHistory",
                table: "CarPriceHistory",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CarPriceHistory_Cars_CarId",
                table: "CarPriceHistory",
                column: "CarId",
                principalTable: "Cars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
