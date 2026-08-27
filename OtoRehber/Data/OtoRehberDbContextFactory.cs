using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OtoRehber.Infrastructure.Data;

namespace OtoRehber.Data
{
    /// <summary>
    /// Tasarım zamanı (dotnet ef migrations add / database update) context fabrikası.
    /// Migration'lar her zaman PostgreSQL'e göre üretilir; production veritabanı Postgres'tir.
    /// Yerel Sqlite geliştirmesi migration kullanmaz (Program.cs'te EnsureCreated).
    /// </summary>
    public class OtoRehberDbContextFactory : IDesignTimeDbContextFactory<OtoRehberDbContext>
    {
        public OtoRehberDbContext CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? "Host=localhost;Port=5432;Database=otorehber;Username=otorehber;Password=otorehber";

            var options = new DbContextOptionsBuilder<OtoRehberDbContext>()
                .UseNpgsql(connectionString, b => b.MigrationsAssembly("OtoRehber"))
                .Options;

            return new OtoRehberDbContext(options);
        }
    }
}
