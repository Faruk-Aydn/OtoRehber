using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OtoRehber.Migrations
{
    /// <inheritdoc />
    public partial class AddProsConsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cons",
                table: "Cars");

            migrationBuilder.DropColumn(
                name: "Pros",
                table: "Cars");

            migrationBuilder.CreateTable(
                name: "ProsCons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CarId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProsCons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProsCons_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ProsCons",
                columns: new[] { "Id", "CarId", "Description", "Type" },
                values: new object[,]
                {
                    { 1, 1, "Kaliteli iç mekan", "Pro" },
                    { 2, 1, "Tok sürüþ hissi", "Pro" },
                    { 3, 1, "Ýyi 2. el deðeri", "Pro" },
                    { 4, 1, "DSG þanzýman riski", "Con" },
                    { 5, 1, "Dizel motor partikül filtresi", "Con" },
                    { 6, 1, "Yüksek servis maliyeti", "Con" },
                    { 7, 2, "Mükemmel sorunsuzluk", "Pro" },
                    { 8, 2, "Geniþ iç hacim", "Pro" },
                    { 9, 2, "Düþük iþletme maliyeti", "Pro" },
                    { 10, 2, "Zayýf yalýtým", "Con" },
                    { 11, 2, "Vasat performans", "Con" },
                    { 12, 2, "Demode iç tasarým", "Con" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProsCons_CarId",
                table: "ProsCons",
                column: "CarId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProsCons");

            migrationBuilder.AddColumn<string>(
                name: "Cons",
                table: "Cars",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Pros",
                table: "Cars",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Cons", "Pros" },
                values: new object[] { "[\"DSG \\u015Fanz\\u0131man riski\",\"Dizel motor partik\\u00FCl filtresi\",\"Y\\u00FCksek servis maliyeti\"]", "[\"Kaliteli i\\u00E7 mekan\",\"Tok s\\u00FCr\\u00FC\\u015F hissi\",\"\\u0130yi 2. el de\\u011Feri\"]" });

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Cons", "Pros" },
                values: new object[] { "[\"Zay\\u0131f yal\\u0131t\\u0131m\",\"Vasat performans\",\"Demode i\\u00E7 tasar\\u0131m\"]", "[\"M\\u00FCkemmel sorunsuzluk\",\"Geni\\u015F i\\u00E7 hacim\",\"D\\u00FC\\u015F\\u00FCk i\\u015Fletme maliyeti\"]" });
        }
    }
}
