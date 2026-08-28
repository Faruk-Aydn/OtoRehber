using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using OtoRehber.Infrastructure.Data;
using OtoRehber.Infrastructure.Services;
using OtoRehber.Domain.Entities;
using OtoRehber.Domain.Interfaces;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Serilog — yapılandırılmış console log (Railway/Render yakalar).
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .WriteTo.Console());

// Hosting platformu (Railway/Render vb.) PORT ortam değişkeni atar; ona bağlan.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Veritabanı Bağlantısı
// Sağlayıcı seçimi: Database:Provider = "Sqlite" (yerel geliştirme, varsayılan) | "Postgres" (production).
// Yerelde Sqlite şeması EnsureCreated ile, Postgres şeması Migration'lar ile oluşturulur.
var dbProvider = (builder.Configuration["Database:Provider"] ?? "Sqlite").Trim();
var isPostgres = dbProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase);

string connectionString;
if (isPostgres)
{
    // Öncelik: DATABASE_URL ortam değişkeni (Railway/Render Postgres eklentisi),
    // sonra ConnectionStrings:DefaultConnection. appsettings.json'daki Sqlite
    // varsayılanı ("Data Source=...") Postgres modunda geçersizdir.
    var cfg = builder.Configuration.GetConnectionString("DefaultConnection");
    connectionString =
        Environment.GetEnvironmentVariable("DATABASE_URL")
        ?? (string.IsNullOrWhiteSpace(cfg) || cfg.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) ? null : cfg)
        ?? throw new InvalidOperationException(
            "Database:Provider=Postgres ancak geçerli bir PostgreSQL bağlantısı yok. " +
            "DATABASE_URL veya ConnectionStrings__DefaultConnection ortam değişkenini ayarlayın " +
            "(örn. Railway'de ConnectionStrings__DefaultConnection = ${{Postgres.DATABASE_URL}}).");

    // "postgresql://user:pass@host:port/db" biçimini Npgsql anahtar-değer biçimine çevir.
    if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
    {
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);
        connectionString =
            $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};" +
            $"Database={Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'))};" +
            $"Username={Uri.UnescapeDataString(userInfo[0])};" +
            $"Password={Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? "")};" +
            // Railway iç ağında SSL yok, public proxy'de var → Prefer ikisini de kapsar.
            "SSL Mode=Prefer;Trust Server Certificate=true";
    }
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=OtoRehberDB.db";
}

builder.Services.AddDbContext<OtoRehberDbContext>(options =>
{
    if (isPostgres)
        options.UseNpgsql(connectionString, b => b.MigrationsAssembly("OtoRehber"));
    else
        options.UseSqlite(connectionString);
});

// Identity Kurulumu
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
    // Giriş için e-posta doğrulaması zorunlu.
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<OtoRehberDbContext>()
.AddDefaultTokenProviders();

// E-posta gönderimi (doğrulama / şifre sıfırlama). Resend API key yoksa no-op + log.
builder.Services.Configure<ResendEmailOptions>(builder.Configuration.GetSection("Resend"));
builder.Services.AddHttpClient<IAppEmailSender, ResendEmailSender>();

// Cookie (Giriş) Ayarları
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";

    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // GET dışındaki tüm istekler için otomatik antiforgery doğrulaması.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// AJAX istekleri antiforgery token'ı bu header ile gönderir.
builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

// AI servisi: IHttpClientFactory üzerinden, sınırlı timeout ile
builder.Services.AddHttpClient<IAiCarDataService, AiCarDataService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(120);
});

// YouTube AI import: request thread'ini bloke etmemek için arka plan kuyruğu.
builder.Services.AddSingleton<YoutubeImportQueue>();
builder.Services.AddSingleton<IYoutubeImportQueue>(sp => sp.GetRequiredService<YoutubeImportQueue>());
builder.Services.AddHostedService<YoutubeImportHostedService>();

// Hız sınırlama (rate limiting)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // AI endpoint'leri: giriş yapan kullanıcıya daha yüksek, anonime sıkı limit.
    options.AddPolicy("ai", httpContext =>
    {
        var isAuth = httpContext.User.Identity?.IsAuthenticated == true;
        var key = isAuth
            ? "u:" + httpContext.User.Identity!.Name
            : "ip:" + (httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous");
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = isAuth ? 15 : 4,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });

    // Kimlik doğrulama (login/register/şifre sıfırlama): IP bazlı brute-force koruması.
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            "ip:" + (httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous"),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            }));

    // Yorum ekleme: kullanıcı bazlı spam koruması.
    options.AddPolicy("review", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            "u:" + (httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous"),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            }));
});

// AutoMapper'ı ekle
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Ağır sorgu/AI çağrısı içeren sayfalar için response caching (Stats, Compare/Result)
builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();

// Yanıt sıkıştırma (Brotli + Gzip)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// Reverse proxy (Railway/Render) arkasında gerçek şema/IP bilgisi.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// DataProtection anahtarları (oturum/antiforgery şifreleme).
// Anahtarlar konteyner içinde (~/.aspnet/DataProtection-Keys) tutulur:
// bir deploy boyunca sabit, deploy değişince yenilenir (kullanıcı tekrar giriş yapar).
// TODO (Faz 2): çok kopyalı / deploy'lar arası kalıcılık için Redis veya sabit anahtar.
builder.Services.AddDataProtection().SetApplicationName("OtoRehber");

// HSTS (production)
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

// Health check — sadece "süreç ayakta mı" (liveness). DB kontrolü deploy'u
// gereksiz yere düşürmesin diye ayrı: /health/ready.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OtoRehberDbContext>(name: "db", tags: new[] { "ready" });

var app = builder.Build();

// Veritabanını oluştur ve Admin kullanıcısını ekle (Seeding)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<OtoRehberDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    // Postgres (production): Migration'ları uygula.
    // Sqlite (yerel geliştirme): şemayı modelden oluştur (migration gerektirmez).
    if (isPostgres)
        await context.Database.MigrateAsync();
    else
        await context.Database.EnsureCreatedAsync();

    var userManager = services.GetRequiredService<UserManager<AppUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    var config = services.GetRequiredService<IConfiguration>();
    string adminEmail = config["AdminSeed:Email"] ?? "admin@otorehber.com";
    string? adminPassword = config["AdminSeed:Password"];

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null && !string.IsNullOrWhiteSpace(adminPassword))
    {
        adminUser = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (!result.Succeeded)
        {
            logger.LogError("Admin kullanıcı oluşturulamadı: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            adminUser = null;
        }
    }
    else if (adminUser == null)
    {
        // Şifre yapılandırılmamışsa admin kullanıcı OLUŞTURULMAZ.
        // Prod'da: AdminSeed__Password ortam değişkeni ile verilmelidir.
        logger.LogWarning(
            "AdminSeed:Password yapılandırılmamış. Admin kullanıcı ({AdminEmail}) oluşturulmadı.",
            adminEmail);
    }

    if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

// Reverse proxy header'ları — pipeline'ın en başında.
app.UseForwardedHeaders();

// İstek başına tek satır özet log (yol, durum, süre).
app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Özel hata sayfaları (404 vb.)
app.UseStatusCodePagesWithReExecute("/Home/Error", "?code={0}");

app.UseResponseCompression();

app.UseHttpsRedirection();

// Güvenlik header'ları
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=(), payment=()";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "font-src 'self' data:; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "object-src 'none';";
    await next();
});

app.UseStaticFiles();

app.UseRouting();

app.UseResponseCaching();
app.UseRateLimiter();

// ÖNEMLİ: Kimlik doğrulama her zaman yetkilendirmeden önce gelmelidir.
app.UseAuthentication();
app.UseAuthorization();

// Liveness: süreç ayakta mı (hiç kontrol çalıştırmaz, hep 200).
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
// Readiness: DB dahil tüm kontroller.
app.MapHealthChecks("/health/ready");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
