using Microsoft.EntityFrameworkCore;
using estacionamientos.Models;

namespace estacionamientos.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PlayaEstacionamiento> Playas => Set<PlayaEstacionamiento>();
    public DbSet<PlazaEstacionamiento> Plazas => Set<PlazaEstacionamiento>();
    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
    public DbSet<Conductor> Conductores => Set<Conductor>();
    public DbSet<Ocupacion> Ocupaciones => Set<Ocupacion>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Tarifario> Tarifarios => Set<Tarifario>();
    public DbSet<Servicio> Servicios => Set<Servicio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Patente única
        modelBuilder.Entity<Vehiculo>()
            .HasIndex(v => v.Patente)
            .IsUnique();

        // Plaza: código único por playa
        modelBuilder.Entity<PlazaEstacionamiento>()
            .HasIndex(p => new { p.PlayaEstacionamientoId, p.Codigo })
            .IsUnique();

        // Ocupación: relaciones + delete restrict para no borrar históricos
        modelBuilder.Entity<Ocupacion>()
            .HasOne(o => o.Plaza)
            .WithMany(p => p.Ocupaciones)
            .HasForeignKey(o => o.PlazaEstacionamientoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ocupacion>()
            .HasOne(o => o.Vehiculo)
            .WithMany(v => v.Ocupaciones)
            .HasForeignKey(o => o.VehiculoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ocupacion>()
            .HasOne(o => o.Conductor)
            .WithMany(c => c.Ocupaciones)
            .HasForeignKey(o => o.ConductorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Pago 1-1 con Ocupacion
        modelBuilder.Entity<Pago>()
            .HasOne(p => p.Ocupacion)
            .WithOne(o => o.Pago)
            .HasForeignKey<Pago>(p => p.OcupacionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tarifario y Servicio pertenecen a una Playa
        modelBuilder.Entity<Tarifario>()
            .HasOne(t => t.Playa)
            .WithMany(p => p.Tarifas)
            .HasForeignKey(t => t.PlayaEstacionamientoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Servicio>()
            .HasOne(s => s.Playa)
            .WithMany(p => p.Servicios)
            .HasForeignKey(s => s.PlayaEstacionamientoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Precisión de decimales
        modelBuilder.Entity<PlayaEstacionamiento>()
            .Property(p => p.ValoracionPromedio)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Ocupacion>()
            .Property(o => o.ImporteCalculado)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Pago>()
            .Property(p => p.Monto)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Tarifario>()
            .Property(t => t.MontoHora)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Tarifario>()
            .Property(t => t.MontoFraccion)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Servicio>()
            .Property(s => s.Precio)
            .HasPrecision(10, 2);
    }
}
