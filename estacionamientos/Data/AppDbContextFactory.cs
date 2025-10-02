using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.IO;

namespace estacionamientos.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // Intentar cargar .env en diseño
            var contentRoot = Directory.GetCurrentDirectory();
            var envPath = Path.Combine(contentRoot, ".env");
            if (File.Exists(envPath))
            {
                try { Env.Load(envPath); } catch { }
            }

            string? Get(string key) => Environment.GetEnvironmentVariable(key);

            // Fallbacks locales para entorno de desarrollo si no hay variables
            var host = Get("DB_HOST") ?? "localhost";
            var port = Get("DB_PORT") ?? "5432";
            var database = Get("DB_NAME") ?? "estacionamientos";
            var user = Get("DB_USER") ?? "postgres";
            var password = Get("DB_PASSWORD") ?? "postgres";
            var sslMode = Get("DB_SSLMODE") ?? "Disable";

            var cs = $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode={sslMode};Include Error Detail=true";

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(cs);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}



