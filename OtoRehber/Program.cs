using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OtoRehber.Infrastructure.Data;
using OtoRehber.Infrastructure.Services;
using OtoRehber.Domain.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Veritabanı Bağlantısı
builder.Services.AddDbContext<OtoRehberDbContext>(options =>
    options.UseSqlite("Data Source=OtoRehberDB.db"));

// Identity Kurulumu
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
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

// Gemini AI servisi
builder.Services.AddScoped<IAiCarDataService, AiCarDataService>();

var app = builder.Build();

// Veritabanını oluştur ve Admin kullanıcısını ekle (Seeding)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<OtoRehberDbContext>();
    
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

    string adminEmail = "admin@otorehber.com";
    string adminPassword = "Admin123!"; // Basit geliştirme şifresi

    var adminUser = userManager.FindByEmailAsync(adminEmail).Result;
    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail
        };

        var result = userManager.CreateAsync(adminUser, adminPassword).Result;
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
app.UseStaticFiles();

app.UseRouting();

// ÖNEMLİ: Kimlik doğrulama her zaman yetkilendirmeden önce gelmelidir.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
