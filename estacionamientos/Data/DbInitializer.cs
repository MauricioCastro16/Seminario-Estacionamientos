using Bogus;
using estacionamientos.Data;
using estacionamientos.Models;
using Microsoft.EntityFrameworkCore;

namespace estacionamientos.Seed;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.EnsureCreated();

        // Idempotencia: si ya sembramos dueños, salgo
        if (context.Duenios.Any())
            return;

        var faker = new Faker("es");

        // =========================
        // 1) Dueños (5)
        // =========================
        int nextUsuNu = Math.Max(9, (context.Usuarios.Any() ? context.Usuarios.Max(u => u.UsuNU) + 1 : 9));
        var duenios = new List<Duenio>();
        for (int i = 0; i < 5; i++)
        {
            var correo = faker.Internet.Email();
            if (!correo.Contains("@")) correo += "@mail.com";

            duenios.Add(new Duenio
            {
                UsuNU = nextUsuNu++,
                UsuNyA = faker.Name.FullName(),
                UsuEmail = correo,
                UsuPswd = "12345678",
                UsuNumTel = faker.Phone.PhoneNumber("##########"),
                DueCuit = faker.Random.ReplaceNumbers("###########")
            });
        }
        context.Duenios.AddRange(duenios);
        context.SaveChanges();

        // =========================
        // 2) Playas (5 por dueño) + AdministraPlaya
        // =========================
        int nextPlyId = 1;
        var playas = new List<PlayaEstacionamiento>();
        var adminPlaya = new List<AdministraPlaya>();

        foreach (var dueno in duenios)
        {
            for (int j = 0; j < 5; j++)
            {
                var playa = new PlayaEstacionamiento
                {
                    PlyID = nextPlyId++,
                    PlyNom = $"Playa {j + 1} de {dueno.UsuNyA.Split(' ')[0]}",
                    PlyProv = faker.Address.State(),
                    PlyCiu = faker.Address.City(),
                    PlyDir = faker.Address.StreetAddress(),
                    PlyTipoPiso = faker.PickRandom("Hormigón", "Asfalto", "Tierra"),
                    PlyValProm = 0m,
                    PlyLlavReq = faker.Random.Bool(),
                    PlyLat = decimal.Parse(faker.Address.Latitude().ToString("F6")),
                    PlyLon = decimal.Parse(faker.Address.Longitude().ToString("F6"))
                };
                playas.Add(playa);

                adminPlaya.Add(new AdministraPlaya
                {
                    DueNU = dueno.UsuNU,
                    PlyID = playa.PlyID
                });
            }
        }
        context.Playas.AddRange(playas);
        context.AdministraPlayas.AddRange(adminPlaya);
        context.SaveChanges();

        var playasPorDueno = adminPlaya
            .GroupBy(ap => ap.DueNU)
            .ToDictionary(g => g.Key, g => g.Select(x => x.PlyID).ToList());

        // =========================
        // 3) Playeros (5 por dueño)
        // =========================
        var playeros = new List<Playero>();
        for (int i = 0; i < duenios.Count; i++)
        {
            for (int k = 0; k < 5; k++)
            {
                var correo = faker.Internet.Email();
                if (!correo.Contains("@")) correo += "@mail.com";

                playeros.Add(new Playero
                {
                    UsuNU = nextUsuNu++,
                    UsuNyA = faker.Name.FullName(),
                    UsuEmail = correo,
                    UsuPswd = "12345678",
                    UsuNumTel = faker.Phone.PhoneNumber("##########"),
                });
            }
        }
        context.Playeros.AddRange(playeros);
        context.SaveChanges();

        // =========================
        // 4) TrabajaEn: histórico + actual
        // =========================
        var trabajaEn = new List<TrabajaEn>();
        int idxPlayero = 0;

        foreach (var dueno in duenios)
        {
            var playerosDeEsteDueno = playeros.Skip(idxPlayero).Take(5).ToList();
            idxPlayero += 5;

            var plyIdsDeDueno = playasPorDueno[dueno.UsuNU];

            foreach (var pla in playerosDeEsteDueno)
            {
                var asignadas = faker.PickRandom(plyIdsDeDueno, faker.Random.Int(2, 3)).Distinct().ToList();

                foreach (var plyId in asignadas)
                {
                    var histIni = faker.Date.Past(1, DateTime.UtcNow.AddDays(-90)).ToUniversalTime();
                    var histFin = histIni.AddDays(faker.Random.Int(20, 50));

                    trabajaEn.Add(new TrabajaEn
                    {
                        PlyID = plyId,
                        PlaNU = pla.UsuNU,
                        TrabEnActual = false,
                        FechaInicio = histIni,
                        FechaFin = histFin
                    });

                    var actIni = faker.Date.Between(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-10)).ToUniversalTime();

                    trabajaEn.Add(new TrabajaEn
                    {
                        PlyID = plyId,
                        PlaNU = pla.UsuNU,
                        TrabEnActual = true,
                        FechaInicio = actIni,
                        FechaFin = null
                    });
                }
            }
        }
        context.Trabajos.AddRange(trabajaEn);
        context.SaveChanges();

        // =========================
        // 5) Turnos donde trabajan
        // =========================
        var turnos = new List<Turno>();
        var periodosPorPar = trabajaEn
            .GroupBy(t => (t.PlyID, t.PlaNU))
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.FechaInicio).ToList());

        foreach (var ((plyId, plaNu), periodos) in periodosPorPar)
        {
            foreach (var periodo in periodos)
            {
                if (periodo.FechaFin == null)
                {
                    int cantTurnos = faker.Random.Int(2, 4);
                    for (int n = 0; n < cantTurnos; n++)
                    {
                        var start = faker.Date.Between(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow).ToUniversalTime();
                        var fin = start.AddHours(faker.Random.Int(6, 9));
                        var apertura = faker.Random.Bool(0.7f) ? faker.Random.Decimal(1000, 10000) : (decimal?)null;
                        var cierre = apertura.HasValue ? apertura.Value + faker.Random.Decimal(-500, 1500) : (decimal?)null;

                        turnos.Add(new Turno
                        {
                            PlyID = plyId,
                            PlaNU = plaNu,
                            TurFyhIni = start,
                            TurFyhFin = fin,
                            TrabFyhIni = periodo.FechaInicio,
                            TurApertCaja = apertura,
                            TurCierrCaja = cierre
                        });
                    }
                }
                else
                {
                    int cantTurnos = faker.Random.Int(1, 2);
                    for (int n = 0; n < cantTurnos; n++)
                    {
                        var start = faker.Date.Between(periodo.FechaInicio, periodo.FechaFin.Value).ToUniversalTime();
                        var fin = start.AddHours(faker.Random.Int(5, 8));

                        turnos.Add(new Turno
                        {
                            PlyID = plyId,
                            PlaNU = plaNu,
                            TurFyhIni = start,
                            TurFyhFin = fin,
                            TrabFyhIni = periodo.FechaInicio,
                            TurApertCaja = null,
                            TurCierrCaja = null
                        });
                    }
                }
            }
        }
        turnos = turnos
            .GroupBy(t => new { t.PlyID, t.PlaNU, t.TurFyhIni })
            .Select(g => g.First())
            .ToList();

        context.Turnos.AddRange(turnos);
        context.SaveChanges();

        // =========================
        // 6) ServiciosProveidos por playa
        // =========================
        var servicios = context.Servicios.AsNoTracking().ToList(); // SerID, SerNom, SerTipo
        var serviciosEst = servicios.Where(s => (s.SerTipo ?? "").Equals("Estacionamiento", StringComparison.OrdinalIgnoreCase)).ToList();
        var serviciosExtra = servicios.Where(s => (s.SerTipo ?? "").Equals("ServicioExtra", StringComparison.OrdinalIgnoreCase)).ToList();

        var serviciosProveidos = new List<ServicioProveido>();

        foreach (var playa in playas)
        {
            foreach (var s in serviciosEst)
            {
                serviciosProveidos.Add(new ServicioProveido
                {
                    PlyID = playa.PlyID,
                    SerID = s.SerID,
                    SerProvHab = true
                });
            }

            var extrasPick = faker.PickRandom(serviciosExtra, faker.Random.Int(1, Math.Min(3, serviciosExtra.Count)))
                                  .Distinct()
                                  .ToList();

            foreach (var s in extrasPick)
            {
                serviciosProveidos.Add(new ServicioProveido
                {
                    PlyID = playa.PlyID,
                    SerID = s.SerID,
                    SerProvHab = faker.Random.Bool(0.9f)
                });
            }
        }

        serviciosProveidos = serviciosProveidos
            .GroupBy(sp => new { sp.PlyID, sp.SerID })
            .Select(g => g.First())
            .ToList();

        context.ServiciosProveidos.AddRange(serviciosProveidos);
        context.SaveChanges();

        // =========================
        // 7) Tarifas (históricas y actuales)
        // =========================
        var clasifIds = context.ClasificacionesVehiculo
                               .Where(c => new[] { 1, 2, 4 }.Contains(c.ClasVehID))
                               .Select(c => c.ClasVehID)
                               .ToList();

        var tarifas = new List<TarifaServicio>();

        foreach (var sp in serviciosProveidos)
        {
            var servicio = servicios.First(s => s.SerID == sp.SerID);
            bool esEst = (servicio.SerTipo ?? "").Equals("Estacionamiento", StringComparison.OrdinalIgnoreCase);

            foreach (var clasId in clasifIds)
            {
                var histIni = faker.Date.Between(DateTime.UtcNow.AddDays(-120), DateTime.UtcNow.AddDays(-60)).Date.ToUniversalTime();
                var histFin = histIni.AddDays(faker.Random.Int(20, 40));

                decimal baseMonto = esEst
                    ? faker.Random.Decimal(600, 3000)
                    : faker.Random.Decimal(1500, 10000);

                decimal factor = clasId switch
                {
                    2 => 1.10m, // Camioneta
                    4 => 0.85m, // Moto
                    _ => 1.00m  // Auto
                };

                var histMonto = Redondear(baseMonto * factor);

                tarifas.Add(new TarifaServicio
                {
                    PlyID = sp.PlyID,
                    SerID = sp.SerID,
                    ClasVehID = clasId,
                    TasFecIni = histIni,
                    TasFecFin = histFin,
                    TasMonto = histMonto
                });

                var actIni = faker.Date.Between(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-5)).Date.ToUniversalTime();
                var actMonto = Redondear(histMonto * (1m + faker.Random.Decimal(0.05m, 0.25m)));

                if (histFin >= actIni) histFin = actIni.AddDays(-1);
                var tHist = tarifas.Last();
                tHist.TasFecFin = histFin;

                tarifas.Add(new TarifaServicio
                {
                    PlyID = sp.PlyID,
                    SerID = sp.SerID,
                    ClasVehID = clasId,
                    TasFecIni = actIni,
                    TasFecFin = null,
                    TasMonto = actMonto
                });
            }
        }

        tarifas = tarifas
            .GroupBy(t => new { t.PlyID, t.SerID, t.ClasVehID, t.TasFecIni })
            .Select(g => g.First())
            .ToList();

        context.TarifasServicio.AddRange(tarifas);
        context.SaveChanges();

        // =========================
        // 8) AceptaMetodoPago por playa (NUEVO)
        //    - Siempre "Efectivo"
        //    - + 1..3 métodos adicionales aleatorios
        // =========================
        var metodos = context.MetodosPago.AsNoTracking().ToList(); // (seed en AppDbContext)
        var acepta = new List<AceptaMetodoPago>();

        foreach (var p in playas)
        {
            // Siempre Efectivo si existe, sino el primero
            var efectivo = metodos.FirstOrDefault(m => m.MepNom.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
                           ?? metodos.First();

            var restantes = metodos.Where(m => m.MepID != efectivo.MepID).ToList();
            var adicionales = faker.PickRandom(restantes, faker.Random.Int(1, Math.Min(3, restantes.Count)))
                                   .Distinct()
                                   .ToList();

            // Efectivo (habilitado)
            acepta.Add(new AceptaMetodoPago
            {
                PlyID = p.PlyID,
                MepID = efectivo.MepID,
                AmpHab = true
            });

            // Otros (mayoría habilitados)
            foreach (var m in adicionales)
            {
                acepta.Add(new AceptaMetodoPago
                {
                    PlyID = p.PlyID,
                    MepID = m.MepID,
                    AmpHab = faker.Random.Bool(0.85f)
                });
            }
        }

        // Evitar duplicados (PK compuesta)
        acepta = acepta
            .GroupBy(a => new { a.PlyID, a.MepID })
            .Select(g => g.First())
            .ToList();

        context.AceptaMetodosPago.AddRange(acepta);
        context.SaveChanges();

        // =========================
        // 9) Plazas por Playa (muchas)
        // =========================
        var clasifs = context.ClasificacionesVehiculo
                             .AsNoTracking()
                             .ToDictionary(c => c.ClasVehID, c => c.ClasVehTipo);

        // Si no hay las típicas, fallback a IDs existentes
        var preferidas = new[] { 1, 2, 4, 3 } // 1=Auto, 2=Camioneta, 4=Moto, 3=Camión (según tu seed)
                           .Where(id => clasifs.ContainsKey(id))
                           .ToArray();

        var plazas = new List<PlazaEstacionamiento>();
        var rnd = new Random();

        foreach (var p in playas)
        {
            // "Muchas": entre 40 y 120 por playa (ajustá a gusto)
            int cantidad = faker.Random.Int(40, 120);

            // Si querés pisos, repartimos en 1..3
            int pisos = faker.Random.Int(1, 3);
            int plzNum = 1;

            for (int i = 0; i < cantidad; i++)
            {
                // Distribución de clasificación (ajustable)
                // 60% Auto, 25% Camioneta, 10% Moto, 5% Camión
                int clasId = preferidas.Length switch
                {
                    >= 4 => faker.Random.WeightedRandom(
                                new[] { preferidas[0], preferidas[1], preferidas[2], preferidas[3] },
                                new[] { 60f, 25f, 10f, 5f }),
                    _ => preferidas[faker.Random.Int(0, preferidas.Length - 1)]
                };

                var piso = pisos == 1 ? 1 : faker.Random.Int(1, pisos);
                var nombre = $"P{piso}-{plzNum.ToString("D3")}";

                plazas.Add(new PlazaEstacionamiento
                {
                    PlyID = p.PlyID,
                    PlzNum = plzNum++,
                    PlzOcupada = false,
                    PlzTecho = faker.Random.Bool(0.55f),                 // ~55% techadas
                    PlzAlt = Math.Round(faker.Random.Decimal(1.80m, 3.30m), 2), // precisión 2 decimales
                    PlzHab = true,
                    PlzNombre = nombre,
                    Piso = piso,
                    ClasVehID = clasId
                });
            }
        }

        // Evitar duplicados (por si se ejecuta dos veces antes de guardar)
        plazas = plazas
            .GroupBy(x => new { x.PlyID, x.PlzNum })
            .Select(g => g.First())
            .ToList();

        context.Plazas.AddRange(plazas);
        context.SaveChanges();

        // =========================
        // 10) Conductores (10 en total)
        // =========================
        var conductores = new List<Conductor>();
        for (int i = 0; i < 10; i++) // Puedes ajustar la cantidad de conductores
        {
            var correo = faker.Internet.Email();
            if (!correo.Contains("@")) correo += "@mail.com";

            conductores.Add(new Conductor
            {
                UsuNU = nextUsuNu++, // ID incremental
                UsuNyA = faker.Name.FullName(),
                UsuEmail = correo,
                UsuPswd = "12345678",
                UsuNumTel = faker.Phone.PhoneNumber("##########"),
                // Las colecciones las dejamos vacías por ahora, pero puedes agregarlas si lo necesitas
                Conducciones = new List<Conduce>(),
                UbicacionesFavoritas = new List<UbicacionFavorita>(),
                Valoraciones = new List<Valoracion>()
            });
        }

        context.Conductores.AddRange(conductores);
        context.SaveChanges();

        
        // =========================
        // 11) Ubicaciones favoritas (2-4 por conductor) 
        // =========================
        var ubicacionesFavoritas = new List<UbicacionFavorita>();
        foreach (var conductor in conductores)
        {
            int cantidadUbicaciones = faker.Random.Int(2, 4); // 2 a 4 ubicaciones por conductor
            for (int j = 0; j < cantidadUbicaciones; j++)
            {
                var ubicacion = new UbicacionFavorita
                {
                    ConNU = conductor.UsuNU, // Asociamos al conductor
                    UbfApodo = faker.Commerce.ProductName(), // Nombre o apodo
                    UbfProv = faker.Address.State(), // Provincia
                    UbfCiu = faker.Address.City(), // Ciudad
                    UbfDir = faker.Address.StreetAddress(), // Dirección
                    UbfTipo = faker.Random.Bool() ? "Casa" : "Trabajo" // Tipo aleatorio
                };

                ubicacionesFavoritas.Add(ubicacion);
            }
        }

        context.UbicacionesFavoritas.AddRange(ubicacionesFavoritas);
        context.SaveChanges();


    }

    private static decimal Redondear(decimal monto)
        => Math.Round(monto, 2, MidpointRounding.AwayFromZero);
}
