using Bogus;
using estacionamientos.Models;
using Microsoft.EntityFrameworkCore;

namespace estacionamientos.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        // ===== Dueños Fake =====
        if (!db.Duenios.Any())
        {
            var fakerDuenio = new Faker<Duenio>("es")
                .RuleFor(d => d.UsuNyA, f => f.Name.FullName())
                .RuleFor(d => d.UsuEmail, f => f.Internet.Email())
                .RuleFor(d => d.UsuPswd, _ => "12345678")
                .RuleFor(d => d.UsuNumTel, f => f.Random.ReplaceNumbers("##########"))
                .RuleFor(d => d.DueCuit, f => f.Random.ReplaceNumbers("###########"));

            var duenios = fakerDuenio.Generate(10);
            db.Duenios.AddRange(duenios);
            await db.SaveChangesAsync();
        }

        // Tomamos un dueño cualquiera (el primero)
        var duenio = await db.Duenios.AsNoTracking().FirstAsync();

        // ===== 2) Playa base (si no existe ninguna) =====
        PlayaEstacionamiento playa;
        if (!await db.Playas.AnyAsync())
        {
            playa = new PlayaEstacionamiento
            {
                PlyNom = "Playa Centro",
                PlyProv = "Chaco",
                PlyCiu = "Resistencia",
                PlyDir = "Av. 25 de Mayo 100",
                PlyTipoPiso = "Hormigón",
                PlyValProm = 0m,
                PlyLlavReq = false,
                PlyLat = -27.451m,
                PlyLon = -58.986m
            };
            db.Playas.Add(playa);
            await db.SaveChangesAsync();
        }
        else
        {
            playa = await db.Playas.FirstAsync();
        }

        // ===== 3) Vincular Dueño ↔ Playa en AdministraPlaya (si falta) =====
        var existeAdmin = await db.AdministraPlayas
            .AnyAsync(a => a.DueNU == duenio.UsuNU && a.PlyID == playa.PlyID);

        if (!existeAdmin)
        {
            db.AdministraPlayas.Add(new AdministraPlaya
            {
                DueNU = duenio.UsuNU,
                PlyID = playa.PlyID
            });
            await db.SaveChangesAsync();
        }

        // ===== 4) Asignar Servicios a la Playa (75% de probabilidad c/u) =====
        // Los servicios "genéricos" ya están seed-eados en OnModelCreating (Servicio.HasData)
        var servicios = await db.Servicios.AsNoTracking().ToListAsync();
        var rng = new Random();

        foreach (var s in servicios)
        {
            var yaTiene = await db.ServiciosProveidos
                .AnyAsync(sp => sp.PlyID == playa.PlyID && sp.SerID == s.SerID);
            if (yaTiene) continue;

            // 75% de probabilidad
            if (rng.NextDouble() < 0.75)
            {
                db.ServiciosProveidos.Add(new ServicioProveido
                {
                    PlyID = playa.PlyID,
                    SerID = s.SerID,
                    SerProvHab = true
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
