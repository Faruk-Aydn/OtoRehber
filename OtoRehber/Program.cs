using System.Security.Cryptography;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OtoRehber.Infrastructure.Data;
using OtoRehber.Infrastructure.Services;
using OtoRehber.Domain.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Veritabanı Bağlantısı
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=OtoRehberDB.db";
builder.Services.AddDbContext<OtoRehberDbContext>(options =>
    options.UseSqlite(connectionString));

// Identity Kurulumu
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<OtoRehberDbContext>()
.AddDefaultTokenProviders();

// Cookie (Giriş) Ayarları
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// AutoMapper'ı ekle
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Gemini AI servisi: IHttpClientFactory üzerinden, sınırlı timeout ile
builder.Services.AddHttpClient<IAiCarDataService, AiCarDataService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(120);
});

// AI endpoint'leri için hız sınırlama (rate limiting)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("ai", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Ağır sorgu/AI çağrısı içeren sayfalar için response caching (Compare/Result vb.)
builder.Services.AddResponseCaching();

var app = builder.Build();

// Veritabanını oluştur ve Admin kullanıcısını ekle (Seeding)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<OtoRehberDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    // Veritabanını (Identity tablolarıyla birlikte) oluştur
    context.Database.EnsureCreated();

    try
    {
        context.Database.ExecuteSqlRaw("ALTER TABLE Cars ADD COLUMN ImageUrl TEXT;");
    }
    catch { /* Column might already exist */ }

    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    // Create Admin Role if it doesn't exist
    if (!roleManager.RoleExistsAsync("Admin").Result)
    {
        roleManager.CreateAsync(new IdentityRole("Admin")).Wait();
    }

    var config = services.GetRequiredService<IConfiguration>();
    string adminEmail = config["AdminSeed:Email"] ?? "admin@otorehber.com";
    string? adminPassword = config["AdminSeed:Password"];

    var adminUser = userManager.FindByEmailAsync(adminEmail).Result;
    if (adminUser == null)
    {
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            // Yapılandırmada şifre verilmemişse rastgele güvenli bir şifre üret ve logla.
            adminPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
            logger.LogWarning(
                "AdminSeed:Password yapılandırılmamış. Otomatik oluşturulan admin şifresi (sadece bu ilk çalıştırmada gösterilir): {AdminPassword}",
                adminPassword);
        }

        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail
        };

        var result = userManager.CreateAsync(adminUser, adminPassword).Result;
        if (!result.Succeeded)
        {
            logger.LogError("Admin kullanıcı oluşturulamadı: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            adminUser = null;
        }
    }

    // Assign Admin Role
    if (adminUser != null && !userManager.IsInRoleAsync(adminUser, "Admin").Result)
    {
        userManager.AddToRoleAsync(adminUser, "Admin").Wait();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Güvenlik header'ları
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.tailwindcss.com https://unpkg.com https://cdn.jsdelivr.net https://code.jquery.com https://cdn.datatables.net; " +
        "style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com https://unpkg.com https://cdn.datatables.net; " +
        "font-src 'self' https://cdnjs.cloudflare.com data:; " +
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
