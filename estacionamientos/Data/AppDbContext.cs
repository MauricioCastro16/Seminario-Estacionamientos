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
    public DbSet<ClasificacionDias> ClasificacionesDias { get; set; } = default!;
    public DbSet<Horario> Horarios { get; set; } = default!;
    public DbSet<MetodoPago> MetodosPago { get; set; } = default!;
    public DbSet<AceptaMetodoPago> AceptaMetodosPago { get; set; } = default!;
    public DbSet<Pago> Pagos { get; set; } = default!;
    public DbSet<PlazaEstacionamiento> Plazas { get; set; } = default!;
    public DbSet<Ocupacion> Ocupaciones { get; set; } = default!;
    public DbSet<Administrador> Administradores { get; set; } = default!;
    public DbSet<Servicio> Servicios { get; set; } = default!;
    public DbSet<ServicioProveido> ServiciosProveidos { get; set; } = default!;
    public DbSet<TarifaServicio> TarifasServicio { get; set; } = default!;
    public DbSet<ServicioExtraRealizado> ServiciosExtrasRealizados { get; set; } = default!;
    public DbSet<Abonado> Abonados { get; set; } = default!;
    public DbSet<Abono> Abonos { get; set; } = default!;
    public DbSet<VehiculoAbonado> VehiculosAbonados { get; set; } = default!;
    public DbSet<PeriodoAbono> PeriodosAbono { get; set; } = default!;
    public DbSet<PlazaClasificacion> PlazasClasificaciones { get; set; } = default!;
    public DbSet<MovimientoPlayero> MovimientosPlayeros { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //Tablas genéricas con data seeding
        modelBuilder.Entity<ClasificacionVehiculo>(entity =>
            {
                entity.ToTable("ClasificacionVehiculo");
                entity.HasKey(c => c.ClasVehID);
                entity.Property(c => c.ClasVehTipo).HasMaxLength(40).IsRequired();
                entity.Property(c => c.ClasVehDesc).HasMaxLength(200);

                entity.HasIndex(c => c.ClasVehTipo).IsUnique();
            });

        modelBuilder.Entity<ClasificacionDias>(e =>
            {
                e.ToTable("ClasificacionDias");
                e.HasKey(c => c.ClaDiasID);
                e.Property(c => c.ClaDiasTipo).HasMaxLength(40).IsRequired();
                e.Property(c => c.ClaDiasDesc).HasMaxLength(200);

                // (Opcional) único por nombre/tipo
                e.HasIndex(c => c.ClaDiasTipo).IsUnique();
            });

        modelBuilder.Entity<MetodoPago>(e =>
            {
                e.ToTable("MetodoPago");
                e.HasKey(m => m.MepID);
                e.Property(m => m.MepNom).HasMaxLength(40).IsRequired();
                e.Property(m => m.MepDesc).HasMaxLength(200);
                e.HasIndex(m => m.MepNom).IsUnique();
            });

        modelBuilder.Entity<Servicio>(e =>
            {
                e.ToTable("Servicio");
                e.HasKey(s => s.SerID);
                e.Property(s => s.SerNom).HasMaxLength(80).IsRequired();
                e.Property(s => s.SerTipo).HasMaxLength(40);
                e.Property(s => s.SerDesc).HasMaxLength(200);
                e.Property(s => s.SerDuracionMinutos); // para facilitar calculo de tarifas
                e.HasIndex(s => s.SerNom).IsUnique(); // opcional
            });

        modelBuilder.Entity<Servicio>().HasData(
        new Servicio
        {
            SerID = 1,
            SerNom = "Lavado de vehículo",
            SerTipo = "ServicioExtra",
            SerDesc = "Lavado exterior e interior del vehículo",
            SerDuracionMinutos = null
        },
        new Servicio
        {
            SerID = 2,
            SerNom = "Mantenimiento de vehículo",
            SerTipo = "ServicioExtra",
            SerDesc = "Revisión y mantenimiento mecánico del vehículo",
            SerDuracionMinutos = null
        },
        new Servicio
        {
            SerID = 3,
            SerNom = "Carga de combustible",
            SerTipo = "ServicioExtra",
            SerDesc = "Carga de combustible en el vehículo",
            SerDuracionMinutos = null
        },
        new Servicio
        {
            SerID = 4,
            SerNom = "Revisión técnica",
            SerTipo = "ServicioExtra",
            SerDesc = "Revisión técnica del vehículo para verificar su estado",
            SerDuracionMinutos = null
        },
        new Servicio
        {
            SerID = 5,
            SerNom = "Estacionamiento por 1 Hora",
            SerTipo = "Estacionamiento",
            SerDesc = "Servicio de estacionamiento por 1 hora en playa",
            SerDuracionMinutos = 60
        },
        new Servicio
        {
            SerID = 6,
            SerNom = "Estacionamiento por 6 Horas",
            SerTipo = "Estacionamiento",
            SerDesc = "Servicio de estacionamiento por 6 horas en playa",
            SerDuracionMinutos = 360
        },
        new Servicio
        {
            SerID = 7,
            SerNom = "Abono por 1 día",
            SerTipo = "Abono",
            SerDesc = "Abono de estacionamiento por 1 día en playa",
            SerDuracionMinutos = 1440
        },
        new Servicio
        {
            SerID = 8,
            SerNom = "Abono por 1 semana",
            SerTipo = "Abono",
            SerDesc = "Abono de estacionamiento por 1 semana en playa",
            SerDuracionMinutos = 10080
        },
        new Servicio
        {
            SerID = 9,
            SerNom = "Abono por 1 mes",
            SerTipo = "Abono",
            SerDesc = "Abono de estacionamiento por 1 mes en playa",
            SerDuracionMinutos = 43200
        }
    );

        modelBuilder.Entity<Administrador>(e =>
        {
            e.ToTable("Administrador");
            e.HasBaseType<Usuario>();
        });

        //Tablas dinámicas

        modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuario");
                entity.HasKey(e => e.UsuNU);

                entity.Property(e => e.UsuNU).UseIdentityByDefaultColumn();

                entity.Property(e => e.UsuNyA).HasMaxLength(120).IsRequired();
                entity.Property(e => e.UsuEmail).HasMaxLength(254).IsRequired();
                entity.Property(e => e.UsuPswd).HasMaxLength(200).IsRequired();
                entity.Property(e => e.UsuNumTel).HasMaxLength(30);
                entity.HasIndex(e => e.UsuEmail).IsUnique();
                entity.HasIndex(e => e.UsuNomUsu).IsUnique();
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
        // TrabajaEn (opción A)
        modelBuilder.Entity<TrabajaEn>(e =>
            {
                e.ToTable("TrabajaEn");
                e.HasKey(x => new { x.PlyID, x.PlaNU, x.FechaInicio }); // <-- PK incluye el inicio del período

                e.Property(x => x.FechaInicio).HasColumnType("timestamptz").IsRequired();
                e.Property(x => x.FechaFin).HasColumnType("timestamptz");
                e.Property(x => x.TrabEnActual).HasDefaultValue(true);

                e.HasOne(x => x.Playa).WithMany().HasForeignKey(x => x.PlyID).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.Playero).WithMany().HasForeignKey(x => x.PlaNU).OnDelete(DeleteBehavior.Cascade);

                // (Opcional) solo 1 período abierto por par
                e.HasIndex(x => new { x.PlyID, x.PlaNU }).IsUnique().HasFilter("\"FechaFin\" IS NULL");
            });


        // Turno
        modelBuilder.Entity<Turno>(e =>
            {
                e.ToTable("Turno");

                // Podés conservar esta PK (PlyID, PlaNU, TurFyhIni)
                e.HasKey(t => new { t.PlyID, t.PlaNU, t.TurFyhIni });

                e.Property(t => t.TurFyhIni).HasColumnType("timestamptz").IsRequired();
                e.Property(t => t.TurFyhFin).HasColumnType("timestamptz");
                e.Property(t => t.TrabFyhIni).HasColumnType("timestamptz").IsRequired(); // FK al período

                // FK AL PERÍODO exacto de TrabajaEn (Opción A)
                e.HasOne(t => t.TrabajaEn)
                .WithMany()
                .HasForeignKey(t => new { t.PlyID, t.PlaNU, t.TrabFyhIni })
                .OnDelete(DeleteBehavior.Restrict);

                // Navegaciones directas (comodidad)
                e.HasOne(t => t.Playa).WithMany().HasForeignKey(t => t.PlyID).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(t => t.Playero).WithMany().HasForeignKey(p => p.PlaNU).OnDelete(DeleteBehavior.Restrict);

                // Índices útiles
                e.HasIndex(t => new { t.PlyID, t.TurFyhIni });
            });


        modelBuilder.Entity<Horario>(e =>
            {
                e.ToTable("Horario");

                // PK compuesta
                e.HasKey(h => new { h.PlyID, h.ClaDiasID, h.HorFyhIni });

                // FK a Playa
                e.HasOne(h => h.Playa)
                .WithMany(p => p.Horarios)     // si querés, agregá esta colección en PlayaEstacionamiento
                .HasForeignKey(h => h.PlyID)
                .OnDelete(DeleteBehavior.Cascade);

                // FK a ClasificacionDias
                e.HasOne(h => h.ClasificacionDias)
                .WithMany(c => c.Horarios)
                .HasForeignKey(h => h.ClaDiasID)
                .OnDelete(DeleteBehavior.Restrict);

                // Índice útil para consultas por Playa/Clasificación
                e.HasIndex(h => new { h.PlyID, h.ClaDiasID, h.HorFyhIni });
            });
        modelBuilder.Entity<AceptaMetodoPago>(e =>
            {
                e.ToTable("AceptaMetodoPago");
                e.HasKey(a => new { a.PlyID, a.MepID });

                e.Property(a => a.AmpHab).HasDefaultValue(true);

                e.HasOne(a => a.Playa)
                .WithMany(p => p.Aceptaciones)
                .HasForeignKey(a => a.PlyID)
                .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(a => a.MetodoPago)
                .WithMany(m => m.Aceptaciones)
                .HasForeignKey(a => a.MepID)
                .OnDelete(DeleteBehavior.Cascade);
            });
        modelBuilder.Entity<Pago>(e =>
            {

                e.ToTable("Pago");

                // PK compuesta (PlyID, PagNum)
                e.HasKey(p => new { p.PlyID, p.PagNum });

                e.Property(p => p.PagMonto)
                    .HasColumnType("decimal(12,2)")
                    .IsRequired();

                e.Property(p => p.PagFyh).IsRequired();

                // FK directa a Playa (comodidad)
                e.HasOne(p => p.Playa)
                .WithMany()                     // o .WithMany(pl => pl.Pagos) si agregás colección
                .HasForeignKey(p => p.PlyID)
                .OnDelete(DeleteBehavior.Cascade);

                // FK directa a MetodoPago (comodidad)
                e.HasOne(p => p.MetodoPago)
                .WithMany()                     // o .WithMany(m => m.Pagos) si querés colección
                .HasForeignKey(p => p.MepID)
                .OnDelete(DeleteBehavior.Restrict);

                // FK compuesta a AceptaMetodoPago => garantiza que (PlyID, MepID) está aceptado por esa Playa
                e.HasOne(p => p.AceptaMetodoPago)
                .WithMany(a => a.Pagos)
                .HasForeignKey(p => new { p.PlyID, p.MepID })
                .OnDelete(DeleteBehavior.Restrict);

                // (Opcional) índice para consultas por fecha
                e.HasIndex(p => new { p.PlyID, p.PagFyh });


                // AceptaMetodoPago: (PlyID, MepID) -> uno (aceptación) a muchos (pagos)
                e.HasOne(p => p.AceptaMetodoPago)
                .WithMany()    // si tenés colección, ponela
                .HasForeignKey(p => new { p.PlyID, p.MepID })
                .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(p => p.Playa)
                .WithMany(pl => pl.Pagos)
                .HasForeignKey(p => p.PlyID)
                .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(p => p.MetodoPago)
                .WithMany() // si tenés colección en MetodoPago, apuntala acá
                .HasForeignKey(p => p.MepID)
                .OnDelete(DeleteBehavior.Restrict);
                
            });
        modelBuilder.Entity<PlazaEstacionamiento>(e =>
        {
            e.ToTable("PlazaEstacionamiento");
            e.HasKey(p => new { p.PlyID, p.PlzNum });

            e.Property(p => p.PlzAlt).HasPrecision(5, 2); // por ej. 3.50 m

            e.Property(p => p.PlzNombre).HasMaxLength(80);

            e.HasOne(p => p.Playa)
                .WithMany(pl => pl.Plazas)
                .HasForeignKey(p => p.PlyID)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔹 Relación con ClasificacionVehiculo
            //e.HasOne(p => p.Clasificacion)
                //.WithMany()
                //.HasForeignKey(p => p.ClasVehID)
                //.OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlazaClasificacion>(e =>
        {
            e.ToTable("PlazaClasificacion");
            e.HasKey(pc => new { pc.PlyID, pc.PlzNum, pc.ClasVehID });

            e.HasOne(pc => pc.Plaza)
                .WithMany(p => p.Clasificaciones)
                .HasForeignKey(pc => new { pc.PlyID, pc.PlzNum })
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(pc => pc.Clasificacion)
                .WithMany()
                .HasForeignKey(pc => pc.ClasVehID)
                .OnDelete(DeleteBehavior.Restrict);
        });


        modelBuilder.Entity<Ocupacion>(e =>
            {

                // FK (opcional) hacia Pago: (PlyID, PagNum)
                e.HasOne(o => o.Pago)
                .WithMany(p => p.Ocupaciones)
                .HasForeignKey(o => new { o.PlyID, o.PagNum })
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull); // si se borra el pago, deja PagNum en null



                e.ToTable("Ocupacion");
                e.HasKey(o => new { o.PlyID, o.PlzNum, o.VehPtnt, o.OcufFyhIni });

                // FK compuesta a Plaza (PlyID, PlzNum)
                e.HasOne(o => o.Plaza)
                .WithMany(p => p.Ocupaciones)
                .HasForeignKey(o => new { o.PlyID, o.PlzNum })
                .OnDelete(DeleteBehavior.Restrict);

                // FK a Vehiculo (VehPtnt)
                e.HasOne(o => o.Vehiculo)
                .WithMany(v => v.Ocupaciones)
                .HasForeignKey(o => o.VehPtnt)
                .OnDelete(DeleteBehavior.Restrict);

                // FK opcional a Pago: (PlyID, PagNum)
                e.HasOne(o => o.Pago)
                .WithMany() // el Pago no necesita colección ahora
                .HasForeignKey(o => new { o.PlyID, o.PagNum })  // PagNum puede ser null
                .OnDelete(DeleteBehavior.SetNull); // si se borra el pago, la ocupación queda sin pago

                // índices útiles
                e.HasIndex(o => new { o.PlyID, o.PlzNum, o.OcufFyhIni });
                e.HasIndex(o => new { o.VehPtnt, o.OcufFyhIni });
            });
        modelBuilder.Entity<ServicioProveido>(e =>
            {
                e.ToTable("ServicioProveido");
                e.HasKey(sp => new { sp.PlyID, sp.SerID });

                e.Property(sp => sp.SerProvHab).HasDefaultValue(true);

                e.HasOne(sp => sp.Playa)
                .WithMany(p => p.ServiciosProveidos /* o crea p.ServiciosProveidos si preferís */)
                .HasForeignKey(sp => sp.PlyID)
                .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(sp => sp.Servicio)
                .WithMany(s => s.Proveidos)
                .HasForeignKey(sp => sp.SerID)
                .OnDelete(DeleteBehavior.Cascade);
            });
        modelBuilder.Entity<TarifaServicio>(e =>
            {
                e.ToTable("TarifaServicio");
                e.HasKey(t => new { t.PlyID, t.SerID, t.ClasVehID, t.TasFecIni });

                e.Property(t => t.TasMonto).HasPrecision(12, 2).IsRequired();

                // FK a ServicioProveido (PlyID, SerID)
                e.HasOne(t => t.ServicioProveido)
                .WithMany(sp => sp.Tarifas)
                .HasForeignKey(t => new { t.PlyID, t.SerID })
                .OnDelete(DeleteBehavior.Cascade);

                // FK a ClasificacionVehiculo
                e.HasOne(t => t.ClasificacionVehiculo)
                .WithMany()
                .HasForeignKey(t => t.ClasVehID)
                .OnDelete(DeleteBehavior.Restrict);

                // Índice auxiliar para buscar la tarifa vigente
                e.HasIndex(t => new { t.PlyID, t.SerID, t.ClasVehID, t.TasFecIni });
            });
        modelBuilder.Entity<ServicioExtraRealizado>(e =>
            {

        

                // FK (opcional) hacia Pago: (PlyID, PagNum)
                e.HasOne(s => s.Pago)
                .WithMany(p => p.ServiciosExtras)
                .HasForeignKey(s => new { s.PlyID, s.PagNum })
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);


                e.ToTable("ServicioExtraRealizado");
                e.HasKey(se => new { se.PlyID, se.SerID, se.VehPtnt, se.ServExFyHIni });

                // Debe existir el servicio proveído en esa playa
                e.HasOne(se => se.ServicioProveido)
                .WithMany(sp => sp.ServiciosExtras)
                .HasForeignKey(se => new { se.PlyID, se.SerID })
                .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(se => se.Vehiculo)
                .WithMany()
                .HasForeignKey(se => se.VehPtnt)
                .OnDelete(DeleteBehavior.Restrict);

                // Pago opcional (PlyID, PagNum)
                e.HasOne(se => se.Pago)
                .WithMany()
                .HasForeignKey(se => new { se.PlyID, se.PagNum })
                .OnDelete(DeleteBehavior.SetNull);

                e.HasIndex(se => new { se.PlyID, se.SerID, se.ServExFyHIni });
            });
        modelBuilder.Entity<Abonado>(e =>
        {
            e.ToTable("Abonado");
            e.HasKey(a => a.AboDNI);
            e.Property(a => a.AboDNI).HasMaxLength(15);
            e.Property(a => a.AboNom).HasMaxLength(120).IsRequired();

            e.HasOne(a => a.Conductor)               // 0..1
             .WithMany()                             // si querés inversa: Conductor.Abonados
             .HasForeignKey(a => a.ConNU)
             .OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<Abono>(e =>
        {
            e.ToTable("Abono");
            e.HasKey(a => new { a.PlyID, a.PlzNum, a.AboFyhIni });

            // Plaza (PlyID, PlzNum)
            e.HasOne(a => a.Plaza)
             .WithMany(p => p.Abonos /* o crea p.Abonos si querés */)
             .HasForeignKey(a => new { a.PlyID, a.PlzNum })
             .OnDelete(DeleteBehavior.Restrict);

            // Abonado
            e.HasOne(a => a.Abonado)
             .WithMany(ab => ab.Abonos)
             .HasForeignKey(a => a.AboDNI)
             .OnDelete(DeleteBehavior.Restrict);

            // Pago requerido (PlyID, PagNum)
            e.HasOne(a => a.Pago)
             .WithMany() // el Pago puede o no tener abono; no forzamos inversa
             .HasForeignKey(a => new { a.PlyID, a.PagNum })
             .OnDelete(DeleteBehavior.Restrict);     // no permitir borrar pago usado por abono
        });
        modelBuilder.Entity<VehiculoAbonado>(e =>
        {
            e.ToTable("VehiculoAbonado");
            e.HasKey(v => new { v.PlyID, v.PlzNum, v.AboFyhIni, v.VehPtnt });

            // FK al Abono
            e.HasOne(v => v.Abono)
             .WithMany(a => a.Vehiculos)
             .HasForeignKey(v => new { v.PlyID, v.PlzNum, v.AboFyhIni })
             .OnDelete(DeleteBehavior.Cascade);

            // FK al Vehiculo
            e.HasOne(v => v.Vehiculo)
             .WithMany() // si querés inversa: Vehiculo.Abonos
             .HasForeignKey(v => v.VehPtnt)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MovimientoPlayero>(e => 
        {
            e.HasKey(m => new { m.MovNum, m.PlyID, m.PlaNU });

            // Autoincrementar MovNum
            e.Property(m => m.MovNum)
            .UseIdentityByDefaultColumn();
            
            // Relación con Playa
            e.HasOne(m => m.Playa)
            .WithMany(p => p.Movimientos)
            .HasForeignKey(m => m.PlyID)
            .OnDelete(DeleteBehavior.Restrict);

            // Relación con Playero
            e.HasOne(m => m.Playero)
            .WithMany(p => p.Movimientos)
            .HasForeignKey(m => m.PlaNU)
            .OnDelete(DeleteBehavior.Restrict);

            // Guardar enum TipoMov como string
            e.Property(m => m.TipoMov)
            .HasConversion<string>();
        });

        modelBuilder.Entity<PeriodoAbono>(e =>
        {
            e.ToTable("PeriodoAbono");
            e.HasKey(p => new { p.PlyID, p.PlzNum, p.AboFyhIni, p.PeriodoNumero });

            e.Property(p => p.PeriodoMonto).HasPrecision(12, 2).IsRequired();
            e.Property(p => p.PeriodoFechaInicio).IsRequired();
            e.Property(p => p.PeriodoFechaFin).IsRequired();
            e.Property(p => p.PeriodoPagado).HasDefaultValue(false);
            e.Property(p => p.PeriodoFechaPago);

            // FK al Abono
            e.HasOne(p => p.Abono)
             .WithMany(a => a.Periodos)
             .HasForeignKey(p => new { p.PlyID, p.PlzNum, p.AboFyhIni })
             .OnDelete(DeleteBehavior.Cascade);

             // 🔹 Relación opcional con Pago
            e.HasOne(p => p.Pago)
            .WithMany() // si querés, más adelante podés poner .WithMany(p => p.Periodos)
            .HasForeignKey(p => new { p.PlyID, p.PagNum })
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

            // Índice para consultas por fecha
            e.HasIndex(p => new { p.PlyID, p.PlzNum, p.AboFyhIni, p.PeriodoNumero });
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
