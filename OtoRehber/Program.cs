using Microsoft.EntityFrameworkCore;
using OtoRehber.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<OtoRehberDbContext>(options =>
    options.UseSqlite("Data Source=OtoRehberDB.db"));

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHostedService<OtoRehber.Services.AiCarDataWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
