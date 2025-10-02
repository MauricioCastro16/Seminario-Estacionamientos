using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using DotNetEnv;
using System.IO;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Razor;
using estacionamientos.Seed;
// using QuestPDF.Infrastructure;  


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
builder.Services.Configure<RazorViewEngineOptions>(o =>
{
    // {1} = nombre del Controller sin "Controller" (p. ej. PlazaEstacionamiento)
    // {0} = nombre de la View/Action (p. ej. ConfigurarPlazas)
    o.ViewLocationFormats.Add("/Views/PlayaEstacionamiento/{1}/{0}.cshtml");
});

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(cs));

// ★ Autenticación por cookies
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Denied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        // options.Cookie.Name = "Estacionamientos.Auth"; // opcional
    });

// ★ Autorización (políticas/roles si luego las usás)
builder.Services.AddAuthorization();

var app = builder.Build();

// Configuración de base de datos según el entorno
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        if (app.Environment.IsProduction())
        {
            // En producción (Render): Reset completo de base de datos
            Console.WriteLine("🌍 Entorno de producción detectado - Reseteando base de datos...");
            Console.WriteLine("🗑️  Eliminando base de datos existente...");
            db.Database.EnsureDeleted();
            Console.WriteLine("✅ Base de datos eliminada");
            
            Console.WriteLine("🔧 Creando base de datos nueva...");
            db.Database.EnsureCreated();
            Console.WriteLine("✅ Base de datos creada");
            
            Console.WriteLine("📊 Aplicando migraciones...");
            db.Database.Migrate();
            Console.WriteLine("✅ Migraciones aplicadas exitosamente");
        }
        else
        {
            // En desarrollo (localhost): Solo aplicar migraciones si es necesario
            Console.WriteLine("🏠 Entorno de desarrollo detectado - Aplicando migraciones si es necesario...");
            Console.WriteLine("📊 Verificando y aplicando migraciones...");
            db.Database.Migrate();
            Console.WriteLine("✅ Base de datos actualizada");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Error con base de datos: " + ex.Message);
        // Continuar sin migraciones si fallan
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ★ MUY IMPORTANTE: primero autenticación, luego autorización
app.UseAuthentication(); // ★
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//Populado de la base de datos con datos de prueba
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    if (app.Environment.IsProduction())
    {
        // En producción: Siempre poblar con datos iniciales
        Console.WriteLine("🌱 Poblando base de datos con datos iniciales...");
        DbInitializer.Initialize(ctx);
        Console.WriteLine("✅ Base de datos poblada exitosamente");
    }
    else
    {
        // En desarrollo: Solo poblar si la base de datos está vacía
        if (!ctx.Usuarios.Any())
        {
            Console.WriteLine("🌱 Base de datos vacía - Poblando con datos iniciales...");
            DbInitializer.Initialize(ctx);
            Console.WriteLine("✅ Base de datos poblada exitosamente");
        }
        else
        {
            Console.WriteLine("📋 Base de datos ya contiene datos - Omitiendo poblado inicial");
        }
    }
}
// QuestPDF deshabilitado temporalmente para deploy
// QuestPDF.Settings.License = LicenseType.Community;
// QuestPDF.Settings.EnableDebugging = true;
app.Run();