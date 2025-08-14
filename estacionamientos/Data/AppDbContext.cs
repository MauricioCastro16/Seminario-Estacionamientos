using Microsoft.EntityFrameworkCore;
using estacionamientos.Models;

namespace estacionamientos.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios { get; set; } = default!;
    public DbSet<Duenio> Duenios { get; set; } = default!;
    public DbSet<Conductor> Conductores { get; set; } = default!;
    public DbSet<Playero> Playeros { get; set; } = default!;
    public DbSet<Vehiculo> Vehiculos { get; set; } = default!;
    public DbSet<Conduce> Conducciones { get; set; } = default!;
    public DbSet<Conduce> Conduces { get; set; } = default!;
    public DbSet<ClasificacionVehiculo> ClasificacionesVehiculo { get; set; } = default!;
    public DbSet<UbicacionFavorita> UbicacionesFavoritas { get; set; } = default!;
    public DbSet<PlayaEstacionamiento> Playas { get; set; } = default!;
    public DbSet<Valoracion> Valoraciones { get; set; } = default!;
    public DbSet<AdministraPlaya> AdministraPlayas { get; set; } = default!;
    public DbSet<TrabajaEn> Trabajos { get; set; } = default!;
    public DbSet<Turno> Turnos { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuario"); // nombre de la tabla

                entity.HasKey(e => e.UsuNU);

                entity.Property(e => e.UsuNU)
                      .HasColumnName("UsuNU")
#if NET8_0_OR_GREATER
                .UseIdentityByDefaultColumn(); // PostgreSQL identity
#endif

                entity.Property(e => e.UsuNyA)
                      .HasColumnName("UsuNyA")
                      .HasMaxLength(120)
                      .IsRequired();

                entity.Property(e => e.UsuEmail)
                      .HasColumnName("UsuEmail")
                      .HasMaxLength(254)
                      .IsRequired();

                entity.Property(e => e.UsuPswd)
                      .HasColumnName("UsuPswd")
                      .HasMaxLength(200)
                      .IsRequired();

                entity.Property(e => e.UsuNumTel)
                      .HasColumnName("UsuNumTel")
                      .HasMaxLength(30);

                // Ejemplo de índice único en email (útil normalmente)
                entity.HasIndex(e => e.UsuEmail).IsUnique();
            });
        modelBuilder.Entity<Duenio>(entity =>
            {
                entity.ToTable("Duenio");       // tabla hija
                entity.HasBaseType<Usuario>(); // establece herencia

                entity.Property(d => d.DueCuit)
                        .HasColumnName("DueCuit")
                        .HasMaxLength(11)
                        .IsRequired();

                // Si querés índice único por CUIT:
                entity.HasIndex(d => d.DueCuit).IsUnique();
            });
        modelBuilder.Entity<Conductor>(entity =>
            {
                entity.ToTable("Conductor");   // PK/FK a Usuario.UsuNU (automático por TPT)
                entity.HasBaseType<Usuario>();
            });
        modelBuilder.Entity<Playero>(entity =>
            {
                entity.ToTable("Playero");     // PK/FK a Usuario.UsuNU (automático por TPT)
                entity.HasBaseType<Usuario>();
            });
        modelBuilder.Entity<ClasificacionVehiculo>(entity =>
            {
                entity.ToTable("ClasificacionVehiculo");
                entity.HasKey(c => c.ClasVehID);
                entity.Property(c => c.ClasVehTipo).HasMaxLength(40).IsRequired();
                entity.Property(c => c.ClasVehDesc).HasMaxLength(200);

                entity.HasIndex(c => c.ClasVehTipo).IsUnique();
            });
        modelBuilder.Entity<Vehiculo>(entity =>
            {
                entity.ToTable("Vehiculo");
                entity.HasKey(v => v.VehPtnt);
                entity.Property(v => v.VehPtnt).HasMaxLength(10);
                entity.Property(v => v.VehMarc).HasMaxLength(80).IsRequired();

                entity.HasOne(v => v.Clasificacion)
                    .WithMany(c => c.Vehiculos)
                    .HasForeignKey(v => v.ClasVehID)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        modelBuilder.Entity<Conduce>(entity =>
        {
            entity.ToTable("Conduce");

            // PK compuesta
            entity.HasKey(c => new { c.ConNU, c.VehPtnt });

            // Relación obligatoria con Conductor (1..* Conduce)
            entity.HasOne(c => c.Conductor)
                  .WithMany(x => x.Conducciones) // o .WithMany(x => x.Conducciones) si agregaste la colección en Conductor
                  .HasForeignKey(c => c.ConNU)
                  .OnDelete(DeleteBehavior.Cascade); // borrar conductor borra sus conducciones

            // Relación obligatoria con Vehiculo (1..* Conduce desde Vehiculo, pero Vehiculo puede no tener Conduce)
            entity.HasOne(c => c.Vehiculo)
                  .WithMany(v => v.Conducciones)
                  .HasForeignKey(c => c.VehPtnt)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<UbicacionFavorita>(entity =>
            {
                entity.ToTable("UbicacionFavorita");

                // PK compuesta
                entity.HasKey(u => new { u.ConNU, u.UbfApodo });

                entity.Property(u => u.UbfApodo).HasMaxLength(50).IsRequired();
                entity.Property(u => u.UbfProv).HasMaxLength(50).IsRequired();
                entity.Property(u => u.UbfCiu).HasMaxLength(80).IsRequired();
                entity.Property(u => u.UbfDir).HasMaxLength(120).IsRequired();
                entity.Property(u => u.UbfTipo).HasMaxLength(30);

                // 1 Conductor -> N UbicacionesFavoritas (requerido)
                entity.HasOne(u => u.Conductor)
                        .WithMany(c => c.UbicacionesFavoritas)
                        .HasForeignKey(u => u.ConNU)
                        .OnDelete(DeleteBehavior.Cascade);
            });
        modelBuilder.Entity<PlayaEstacionamiento>(e =>
            {
                e.ToTable("PlayaEstacionamiento");
                e.HasKey(p => p.PlyID);
                e.Property(p => p.PlyProv).HasMaxLength(50).IsRequired();
                e.Property(p => p.PlyCiu).HasMaxLength(80).IsRequired();
                e.Property(p => p.PlyDir).HasMaxLength(120).IsRequired();
                e.Property(p => p.PlyTipoPiso).HasMaxLength(30);

                // decimal con precisión (0..9.99 por ej). Ajustá a tu gusto
                e.Property(p => p.PlyValProm).HasPrecision(4, 2).HasDefaultValue(0m);

                e.Property(p => p.PlyLlavReq);
            });
        modelBuilder.Entity<Valoracion>(e =>
            {
                e.ToTable("Valoracion");
                e.HasKey(v => new { v.PlyID, v.ConNU });

                e.Property(v => v.ValNumEst).IsRequired();
                e.Property(v => v.ValFav);

                e.HasOne(v => v.Playa)
                .WithMany(p => p.Valoraciones)
                .HasForeignKey(v => v.PlyID)
                .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(v => v.Conductor)
                .WithMany(c => c.Valoraciones)
                .HasForeignKey(v => v.ConNU)
                .OnDelete(DeleteBehavior.Cascade);
            });
        modelBuilder.Entity<AdministraPlaya>(e =>
            {
                e.ToTable("AdministraPlaya");
                e.HasKey(a => new { a.DueNU, a.PlyID });

                e.HasOne(a => a.Duenio)
                .WithMany(d => d.Administraciones /* si querés, agregá esta colección en Dueno */)
                .HasForeignKey(a => a.DueNU)
                .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(a => a.Playa)
                .WithMany(p => p.Administradores)
                .HasForeignKey(a => a.PlyID)
                .OnDelete(DeleteBehavior.Cascade);
            });
        modelBuilder.Entity<TrabajaEn>(e =>
           {
               e.ToTable("TrabajaEn");
               e.HasKey(x => new { x.PlyID, x.PlaNU });   // PK compuesta

               e.HasOne(x => x.Playa)
                .WithMany()                                // podés crear p.Trabajos si querés
                .HasForeignKey(x => x.PlyID)
                .OnDelete(DeleteBehavior.Cascade);

               e.HasOne(x => x.Playero)
                .WithMany()                                // podés crear pl.Trabajos si querés
                .HasForeignKey(x => x.PlaNU)
                .OnDelete(DeleteBehavior.Cascade);
           });
        modelBuilder.Entity<Turno>(e =>
            {
                e.ToTable("Turno");

                // PK compuesta: (PlyID, PlaNU, TurFyhIni)
                e.HasKey(t => new { t.PlyID, t.PlaNU, t.TurFyhIni });

                // FK compuesta a TrabajaEn para forzar que el playero-trabaja-en-la-playa
                e.HasOne(t => t.TrabajaEn)
                .WithMany()
                .HasForeignKey(t => new { t.PlyID, t.PlaNU })
                .OnDelete(DeleteBehavior.Restrict);

                // Navegaciones directas (comodidad para Include)
                e.HasOne(t => t.Playa)
                .WithMany()
                .HasForeignKey(t => t.PlyID)
                .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(t => t.Playero)
                .WithMany()
                .HasForeignKey(t => t.PlaNU)
                .OnDelete(DeleteBehavior.Restrict);

                // Índice útil para búsquedas por playa y fechas
                e.HasIndex(t => new { t.PlyID, t.TurFyhIni });
            });
    }

    // ---- Recalcular promedio de una/s playa/s cuando cambian valoraciones
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Tomamos los PlyID afectados por inserts/updates/deletes de Valoracion
        var affectedPlyIds = ChangeTracker.Entries<Valoracion>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => e.State == EntityState.Deleted ? e.OriginalValues.GetValue<int>(nameof(Valoracion.PlyID))
                                                        : e.CurrentValues.GetValue<int>(nameof(Valoracion.PlyID)))
            .Distinct()
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (affectedPlyIds.Count > 0)
        {
            foreach (var plyId in affectedPlyIds)
            {
                // Calculamos el promedio (0 si no hay valoraciones)
                var avg = await Valoraciones
                    .Where(v => v.PlyID == plyId)
                    .Select(v => (decimal?)v.ValNumEst)
                    .AverageAsync(cancellationToken) ?? 0m;

                // Redondeo a 2 decimales (ajustá si querés comportamiento distinto)
                avg = Math.Round(avg, 2, MidpointRounding.AwayFromZero);

                // Actualizamos el campo persistido
                await Playas.Where(p => p.PlyID == plyId)
                            .ExecuteUpdateAsync(s => s.SetProperty(p => p.PlyValProm, avg), cancellationToken);
            }
        }

        return result;
    }
}
