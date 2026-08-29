using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OtoRehber.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeCarSegment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mevcut Segment değerlerini sabit listeye (CarSegments.All) hizala.
            // Sadece PostgreSQL'de çalışır (yerelde Sqlite EnsureCreated + seed zaten "C").
            if (migrationBuilder.ActiveProvider?.Contains("Npgsql") != true) return;

            migrationBuilder.Sql(@"
                UPDATE ""Cars"" SET ""Segment"" = TRIM(""Segment"") WHERE ""Segment"" IS NOT NULL;

                -- ""A Segmenti"", ""C segment"", ""d sinifi"" gibi ifadeleri tek harfe indir
                UPDATE ""Cars"" SET ""Segment"" = UPPER(LEFT(""Segment"", 1))
                WHERE ""Segment"" ~* '^[A-E]([[:space:]]|-)*(segment|segmenti|sinifi|sınıfı|class)?$';

                UPDATE ""Cars"" SET ""Segment"" = 'SUV'        WHERE LOWER(""Segment"") IN ('suv', 'crossover', 'suv/crossover');
                UPDATE ""Cars"" SET ""Segment"" = 'MPV'        WHERE LOWER(""Segment"") IN ('mpv', 'minivan', 'van (mpv)');
                UPDATE ""Cars"" SET ""Segment"" = 'Ticari'     WHERE LOWER(""Segment"") IN ('ticari', 'van', 'panelvan', 'hafif ticari');
                UPDATE ""Cars"" SET ""Segment"" = 'Spor'       WHERE LOWER(""Segment"") IN ('spor', 'sport', 'coupe', 'coupé');
                UPDATE ""Cars"" SET ""Segment"" = 'Elektrikli' WHERE LOWER(""Segment"") IN ('elektrikli', 'elektrik', 'ev', 'bev');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Veri normalizasyonu geri alınamaz.
        }
    }
}
