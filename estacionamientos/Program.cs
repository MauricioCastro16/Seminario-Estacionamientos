using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using DotNetEnv;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Cargar el .env desde la carpeta del proyecto
Env.Load(Path.Combine(builder.Environment.ContentRootPath, ".env"));

string Required(string key) =>
    Environment.GetEnvironmentVariable(key)
    ?? throw new InvalidOperationException($"Falta la variable de entorno {key}.");

var cs =
    $"Host={Required("DB_HOST")};" +
    $"Port={Required("DB_PORT")};" +
    $"Database={Required("DB_NAME")};" +
    $"Username={Required("DB_USER")};" +
    $"Password={Required("DB_PASSWORD")};" +
    $"SSL Mode={(Environment.GetEnvironmentVariable("DB_SSLMODE") ?? "Disable")};" +
    $"Include Error Detail=true";

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(cs));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.OpenConnection();
        Console.WriteLine("✅ Conexión a PostgreSQL exitosa");
        db.Database.CloseConnection();
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Error conectando a PostgreSQL: " + ex.Message);
    }
}


// (opcional) aplicar migraciones al arrancar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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
