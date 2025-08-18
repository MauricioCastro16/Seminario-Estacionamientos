using Bogus;
using estacionamientos.Models;

namespace estacionamientos.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        // ===== Conductores Fake =====
        if (!db.Conductores.Any())
        {
            var fakerConductor = new Faker<Conductor>("es")
                .RuleFor(c => c.UsuNyA, f => f.Name.FullName())
                .RuleFor(c => c.UsuEmail, f => f.Internet.Email())
                .RuleFor(c => c.UsuPswd, _ => "12345678") // ⚠️ En prod: hashear
                .RuleFor(c => c.UsuNumTel, f => f.Random.ReplaceNumbers("##########"));

            var conductores = fakerConductor.Generate(20);
            db.Conductores.AddRange(conductores);
        }

        // ===== Dueños Fake =====
        if (!db.Duenios.Any())
        {
            var fakerDuenio = new Faker<Duenio>("es")
                .RuleFor(d => d.UsuNyA, f => f.Name.FullName())
                .RuleFor(d => d.UsuEmail, f => f.Internet.Email())
                .RuleFor(d => d.UsuPswd, _ => "12345678")
                .RuleFor(d => d.UsuNumTel, f => f.Phone.PhoneNumber())
                .RuleFor(d => d.DueCuit, f => f.Random.ReplaceNumbers("###########"));

            var duenios = fakerDuenio.Generate(10);
            db.Duenios.AddRange(duenios);
        }

        await db.SaveChangesAsync();
    }
}
