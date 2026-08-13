using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OtoRehber.Migrations
{
    /// <inheritdoc />
    public partial class AddChronicIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChronicIssues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CarId = table.Column<int>(type: "INTEGER", nullable: false),
                    IssueTitle = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: false),
                    EstimatedCostEUR = table.Column<int>(type: "INTEGER", nullable: false),
                    AffectedYears = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChronicIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChronicIssues_Cars_CarId",
                        column: x => x.CarId,
                        principalTable: "Cars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Cons", "EstimatedMaintenanceCostEUR", "ExpertSummary", "Pros" },
                values: new object[] { "[\"DSG \\u015Fanz\\u0131man riski\",\"Dizel motor partik\\u00FCl filtresi\",\"Y\\u00FCksek servis maliyeti\"]", 400, "C segmentinin referans modeli, kaliteli iç mekan ve tok sürüþ hissi sunar. Ancak DSG þanzýman ve dizel motor bakým maliyetlerine dikkat edilmelidir.", "[\"Kaliteli i\\u00E7 mekan\",\"Tok s\\u00FCr\\u00FC\\u015F hissi\",\"\\u0130yi 2. el de\\u011Feri\"]" });

            migrationBuilder.InsertData(
                table: "Cars",
                columns: new[] { "Id", "Brand", "Cons", "Engine", "EstimatedMaintenanceCostEUR", "ExpertSummary", "ModelName", "PriceRange", "Pros", "ReliabilityScore", "Segment" },
                values: new object[] { 2, "Toyota", "[\"Zay\\u0131f yal\\u0131t\\u0131m\",\"Vasat performans\",\"Demode i\\u00E7 tasar\\u0131m\"]", "1.6 Valvematic", 200, "Sorunsuzluk dendiðinde akla ilk gelen model. Konfor odaklý, aile kullanýmýna çok uygun fakat performans beklentisi olanlarý üzebilir.", "Corolla", "700k - 1.1M TL", "[\"M\\u00FCkemmel sorunsuzluk\",\"Geni\\u015F i\\u00E7 hacim\",\"D\\u00FC\\u015F\\u00FCk i\\u015Fletme maliyeti\"]", 9.5, "C" });

            migrationBuilder.InsertData(
                table: "ChronicIssues",
                columns: new[] { "Id", "AffectedYears", "CarId", "Description", "EstimatedCostEUR", "IssueTitle", "Severity" },
                values: new object[,]
                {
                    { 1, "2013-2018", 1, "Özellikle kuru kavramalý 7 ileri DSG þanzýmanlarda dur-kalk trafikte ýsýnma ve mekatronik arýzasý.", 1200, "DSG Þanzýman Mekatronik Arýzasý", "Kritik" },
                    { 2, "2013-2020", 1, "Düþük devirlerde þehir içi kullanýmda kurum baðlamasý ve týkanýklýk.", 500, "EGR ve Partikül Filtresi", "Orta" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChronicIssues_CarId",
                table: "ChronicIssues",
                column: "CarId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChronicIssues");

            migrationBuilder.DeleteData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.UpdateData(
                table: "Cars",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Cons", "EstimatedMaintenanceCostEUR", "ExpertSummary", "Pros" },
                values: new object[] { "[]", 0, "C segmentinin referans modeli.", "[]" });
        }
    }
}
