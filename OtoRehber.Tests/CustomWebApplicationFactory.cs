using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OtoRehber.Infrastructure.Data;

namespace OtoRehber.Tests;

/// <summary>
/// Testler için uygulamayı, her fabrika örneğine özel geçici bir SQLite dosyasıyla
/// ayağa kaldırır. `Program.cs`'teki seeding bloğu `EnsureCreatedAsync` ile tüm
/// tabloları (DataProtectionKeys dahil) modelden oluşturur; HasData ile 2 araç gelir.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"otorehber-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Katalog seeder testlerde kapalı: HasData'nın 2 aracıyla hızlı ve deterministik.
        builder.UseSetting("Catalog:Sync", "false");

        // Minimal hosting'de ConfigureAppConfiguration Program.cs'in erken config
        // okumasını güvenilir şekilde geçersiz kılamıyor; DbContext'i doğrudan
        // test SQLite dosyasına yönlendiriyoruz.
        builder.ConfigureTestServices(services =>
        {
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<OtoRehberDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ServiceType == typeof(OtoRehberDbContext))
                .ToList();
            foreach (var d in descriptors)
                services.Remove(d);

            services.AddDbContext<OtoRehberDbContext>(o => o.UseSqlite($"Data Source={_dbPath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { /* yoksay */ }
    }
}
