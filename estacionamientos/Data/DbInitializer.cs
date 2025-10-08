using Bogus;
using estacionamientos.Data;
using estacionamientos.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace estacionamientos.Seed;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.EnsureCreated();

        // ⚠️ Si ya hay datos, los borro para resembrar
        if (context.ClasificacionesVehiculo.Any() || context.Servicios.Any())
        {
            context.ClasificacionesVehiculo.RemoveRange(context.ClasificacionesVehiculo);
            context.Servicios.RemoveRange(context.Servicios);
            context.SaveChanges();
        }


        var faker = new Faker("es");

        // =========================
        // 1) Datos básicos de inicialización
        // =========================
        
        // ClasificacionVehiculo
        var clasificacionesVehiculo = new List<ClasificacionVehiculo>
        {
            new ClasificacionVehiculo { ClasVehID = 1, ClasVehTipo = "Automóvil", ClasVehDesc = "Vehículo de pasajeros" },
            new ClasificacionVehiculo { ClasVehID = 2, ClasVehTipo = "Camioneta", ClasVehDesc = "Vehículo utilitario" },
            new ClasificacionVehiculo { ClasVehID = 3, ClasVehTipo = "Camión", ClasVehDesc = "Vehículo de carga" },
            new ClasificacionVehiculo { ClasVehID = 4, ClasVehTipo = "Motocicleta", ClasVehDesc = "Vehículo de dos ruedas" }
        };
        context.ClasificacionesVehiculo.AddRange(clasificacionesVehiculo);
        context.SaveChanges();

        // ClasificacionDias
        var clasificacionesDias = new List<ClasificacionDias>
        {
            new ClasificacionDias { ClaDiasID = 1, ClaDiasTipo = "Hábil", ClaDiasDesc = "Lunes a Viernes" },
            new ClasificacionDias { ClaDiasID = 2, ClaDiasTipo = "Fin de semana", ClaDiasDesc = "Sábado y Domingo" },
            new ClasificacionDias { ClaDiasID = 3, ClaDiasTipo = "Feriado", ClaDiasDesc = "Feriados no laborables" }
        };
        context.ClasificacionesDias.AddRange(clasificacionesDias);
        context.SaveChanges();

        // MetodoPago
        var metodosPago = new List<MetodoPago>
        {
            new MetodoPago { MepID = 1, MepNom = "Efectivo", MepDesc = "Pago en efectivo" },
            new MetodoPago { MepID = 2, MepNom = "Tarjeta de crédito", MepDesc = "Pago con tarjeta de crédito" },
            new MetodoPago { MepID = 3, MepNom = "Tarjeta de débito", MepDesc = "Pago con tarjeta de débito" },
            new MetodoPago { MepID = 4, MepNom = "Transferencia bancaria", MepDesc = "Pago mediante transferencia bancaria" }
        };
        context.MetodosPago.AddRange(metodosPago);
        context.SaveChanges();

        // Servicio
        var servicios = new List<Servicio>
        {
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
                SerNom = "Estacionamiento por hora",
                SerTipo = "Estacionamiento",
                SerDesc = "Servicio de estacionamiento por 1 hora en playa",
                SerDuracionMinutos = 60
            },

            new Servicio
            {
                SerID = 6,
                SerNom = "Estacionamiento por fraccion de hora",
                SerTipo = "Estacionamiento",
                SerDesc = "Servicio de estacionamiento por fraccion",
                SerDuracionMinutos = 30
            },

            new Servicio
            {
                SerID = 7,
                SerNom = "Abono por 1 día",
                SerTipo = "Abono",
                SerDesc = "Servicio de estacionamiento por 1 día en playa",
                SerDuracionMinutos = 1440
            },
            new Servicio
            {
                SerID = 8,
                SerNom = "Abono por 1 semana",
                SerTipo = "Abono",
                SerDesc = "Servicio de estacionamiento por 1 semana en playa",
                SerDuracionMinutos = 10080
            },
            new Servicio
            {
                SerID = 9,
                SerNom = "Abono por 1 mes",
                SerTipo = "Abono",
                SerDesc = "Servicio de estacionamiento por 1 mes en playa",
                SerDuracionMinutos = 43200
            }
        };
        context.Servicios.AddRange(servicios);
        context.SaveChanges();

        // Administrador
        var administradores = new List<Administrador>
        {
            new Administrador
            {
                UsuNU = 1,
                UsuNyA = "Mauricio Nicolás Castro",
                UsuEmail = "castromauricionicolas@hotmail.com",
                UsuPswd = BCrypt.Net.BCrypt.HashPassword("12345678"),
                UsuNumTel = "1234567890",
                UsuNomUsu = "MauriCastro"
            },
            new Administrador
            {
                UsuNU = 2,
                UsuNyA = "Yoel Brizuela Silvestri",
                UsuEmail = "brizuelajoelelian@gmail.com",
                UsuPswd = BCrypt.Net.BCrypt.HashPassword("12345678"),
                UsuNumTel = "0987654321",
                UsuNomUsu = "YoelBrizuela"
            },
            new Administrador
            {
                UsuNU = 3,
                UsuNyA = "Nadine Andrea Peralta Ruiz",
                UsuEmail = "nadineperaltaruiz@gmail.com",
                UsuPswd = BCrypt.Net.BCrypt.HashPassword("12345678"),
                UsuNumTel = "1122334455",
                UsuNomUsu = "NadinePeralta"
            },
            new Administrador
            {
                UsuNU = 4,
                UsuNyA = "Mateo Beneyto",
                UsuEmail = "mateobeneyto@gmail.com",
                UsuPswd = BCrypt.Net.BCrypt.HashPassword("12345678"),
                UsuNumTel = "5566778899",
                UsuNomUsu = "MateoBeneyto"
            },
            new Administrador
            {
                UsuNU = 5,
                UsuNyA = "Iván Josué Nikcevich",
                UsuEmail = "ivan.nikcevich@hotmail.com",
                UsuPswd = BCrypt.Net.BCrypt.HashPassword("12345678"),
                UsuNumTel = "2233445566",
                UsuNomUsu = "IvanNikcevich"
            },
            new Administrador
            {
                UsuNU = 6,
                UsuNyA = "Adriano Nikcevich",
                UsuEmail = "adri.nikce30@gmail.com",
                UsuPswd = BCrypt.Net.BCrypt.HashPassword("12345678"),
                UsuNumTel = "6677889900",
                UsuNomUsu = "AdrianoNikcevich"
            },
            new Administrador
            {
                UsuNU = 7,
                UsuNyA = "Solana Livio",
                UsuEmail = "solana.livio1976@gmail.com",
                UsuPswd = BCrypt.Net.BCrypt.HashPassword("12345678"),
                UsuNumTel = "3344556677",
                UsuNomUsu = "SolanaLivio"
            },
            new Administrador
            {
                UsuNU = 8,
                UsuNyA = "Elías Obregón",
                UsuEmail = "obregon.elias@gmail.com",
                UsuPswd = BCrypt.Net.BCrypt.HashPassword("12345678"),
                UsuNumTel = "7788990011",
                UsuNomUsu = "EliasObregon"
            }
        };
        context.Administradores.AddRange(administradores);
        context.SaveChanges();

        // =========================
        // 2) Dueños (5)
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
                UsuPswd = BCrypt.Net.BCrypt.HashPassword("12345678"),
                UsuNumTel = faker.Phone.PhoneNumber("##########"),
                UsuNomUsu = faker.Internet.UserName(), // Nombre de usuario
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
                    UsuPswd = BCrypt.Net.BCrypt.HashPassword("12345678"),
                    UsuNumTel = faker.Phone.PhoneNumber("##########"),
                    UsuNomUsu = faker.Internet.UserName(), // Nombre de usuario
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
                        var apertura = faker.Random.Decimal(0, 10000);
                        var cierre = apertura + faker.Random.Decimal(-500, 1500);

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
                        var apertura = faker.Random.Decimal(0, 10000);
                        var cierre = apertura + faker.Random.Decimal(-500, 1500);

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
        var serviciosDisponibles = context.Servicios.AsNoTracking().ToList(); // SerID, SerNom, SerTipo
        var serviciosEst = serviciosDisponibles.Where(s => (s.SerTipo ?? "").Equals("Estacionamiento", StringComparison.OrdinalIgnoreCase) || (s.SerTipo ?? "").Equals("Abono", StringComparison.OrdinalIgnoreCase)).ToList();
        var serviciosExtra = serviciosDisponibles.Where(s => (s.SerTipo ?? "").Equals("ServicioExtra", StringComparison.OrdinalIgnoreCase)).ToList();

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
            var servicio = serviciosDisponibles.First(s => s.SerID == sp.SerID);
            bool esEst = (servicio.SerTipo ?? "").Equals("Estacionamiento", StringComparison.OrdinalIgnoreCase) || (servicio.SerTipo ?? "").Equals("Abono", StringComparison.OrdinalIgnoreCase);

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

                // Crear plaza
                var plaza = new PlazaEstacionamiento
                {
                    PlyID = p.PlyID,
                    PlzNum = plzNum++,
                    PlzOcupada = false,
                    PlzTecho = faker.Random.Bool(0.55f),                 // ~55% techadas
                    PlzAlt = Math.Round(faker.Random.Decimal(1.80m, 3.30m), 2), // precisión 2 decimales
                    PlzHab = true,
                    PlzNombre = nombre,
                    Piso = piso
                };

                // 🔹 agregar clasificación en tabla intermedia
                plaza.Clasificaciones.Add(new PlazaClasificacion
                {
                    PlyID = plaza.PlyID,
                    PlzNum = plaza.PlzNum,
                    ClasVehID = clasId
                });

                plazas.Add(plaza);
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
            if (!correo.Contains('@')) correo += "@mail.com";

            conductores.Add(new Conductor
            {
                UsuNU = nextUsuNu++, // ID incremental
                UsuNyA = faker.Name.FullName(),
                UsuEmail = correo,
                UsuPswd = BCrypt.Net.BCrypt.HashPassword("12345678"),
                UsuNumTel = faker.Phone.PhoneNumber("##########"),
                UsuNomUsu = faker.Internet.UserName(), // Nombre de usuario
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

        // =========================
        // 12) Valoraciones (2-3 por conductor)
        // =========================
        var valoraciones = new List<Valoracion>();
        foreach (var conductor in conductores)
        {
            int cantidadValoraciones = faker.Random.Int(2, 3); // 2 a 3 valoraciones por conductor
            var playasValoradas = new HashSet<int>(); // Conjunto para asegurarnos que un conductor no valore más de una vez la misma playa

            for (int j = 0; j < cantidadValoraciones; j++)
            {
                // Seleccionamos una playa aleatoria que el conductor aún no haya valorado
                PlayaEstacionamiento playa;
                do
                {
                    playa = faker.PickRandom(context.Playas.ToList()); // Elegir una playa aleatoria
                }
                while (playasValoradas.Contains(playa.PlyID)); // Aseguramos que la playa no ha sido valorada aún por este conductor

                // Añadimos la playa a las playas valoradas para este conductor
                playasValoradas.Add(playa.PlyID);

                // Crear la valoracion
                valoraciones.Add(new Valoracion
                {
                    PlyID = playa.PlyID, // ID de la playa asociada
                    ConNU = conductor.UsuNU, // ID del conductor asociado
                    ValNumEst = faker.Random.Int(1, 5), // Estrellas entre 1 y 5
                    ValFav = faker.Random.Bool(), // Aleatorio si es favorito o no
                });
            }
        }

        context.Valoraciones.AddRange(valoraciones);
        context.SaveChanges();

        // =========================
        // 13) Abonados (20% conductores + 80% independientes)
        // =========================
        var abonados = new List<Abonado>();
        
        // Calcular cantidad total de abonados necesarios: 5 abonos por playa
        int totalPlayas = playas.Count;
        int totalAbonosNecesarios = totalPlayas * 5;
        
        // 20% serán conductores, 80% independientes
        int conductoresAbonadosCount = (int)(totalAbonosNecesarios * 0.2);
        int abonadosIndependientesCount = totalAbonosNecesarios - conductoresAbonadosCount;
        
        // Limitar la cantidad de conductores-abonados a los conductores disponibles
        int conductoresDisponibles = conductores.Count;
        int conductoresAbonadosReales = Math.Min(conductoresAbonadosCount, conductoresDisponibles);
        int abonadosIndependientesReales = totalAbonosNecesarios - conductoresAbonadosReales;
        
        // Abonados que también son conductores (hasta el 20% del total, pero no más que los disponibles)
        var conductoresSeleccionados = faker.PickRandom(conductores, conductoresAbonadosReales).ToList();
        
        foreach (var conductor in conductoresSeleccionados)
        {
            var abonado = new Abonado
            {
                AboDNI = faker.Random.ReplaceNumbers("########"), // DNI aleatorio
                AboNom = conductor.UsuNyA, // Nombre del conductor
                ConNU = conductor.UsuNU, // Conductor asociado
            };

            abonados.Add(abonado);
        }

        // Abonados independientes (el resto del total)
        for (int i = 0; i < abonadosIndependientesReales; i++)
        {
            var abonado = new Abonado
            {
                AboDNI = faker.Random.ReplaceNumbers("########"), // DNI aleatorio
                AboNom = faker.Name.FullName(), // Nombre aleatorio
                ConNU = null, // No es conductor del sistema
            };

            abonados.Add(abonado);
        }

        context.Abonados.AddRange(abonados);
        context.SaveChanges();

        // =========================
        // 14) Vehiculos
        // =========================
        var vehiculos = new List<Vehiculo>();
        foreach (var conductor in conductores)
        {
            int cantidadVehiculos = faker.Random.Int(1, 2); // Cada conductor puede tener 1 o 2 vehículos
            for (int j = 0; j < cantidadVehiculos; j++)
            {
                var vehiculo = new Vehiculo
                {
                    VehPtnt = faker.Vehicle.Vin().Substring(0, 10), // Generar un número de patente aleatorio, limitada a 10 caracteres
                    VehMarc = faker.Vehicle.Manufacturer(), // Generar marca del vehículo
                    ClasVehID = faker.PickRandom(context.ClasificacionesVehiculo.Select(c => c.ClasVehID).ToList()) // Asignar clasificación aleatoria
                };

                vehiculos.Add(vehiculo);
            }
        }

        context.Vehiculos.AddRange(vehiculos);
        context.SaveChanges();

        // =========================
        // 15) Conduce (Asociar vehículos con conductores)
        // =========================
        var conduceList = new List<Conduce>();
        foreach (var conductor in conductores)
        {
            // Seleccionamos aleatoriamente entre 1 y 2 vehículos para cada conductor
            var cantidadVehiculos = faker.Random.Int(1, 2); // Cada conductor puede tener entre 1 o 2 vehículos
            var vehiculosDisponibles = vehiculos.ToList(); // Lista de vehículos disponibles

            // Asociar vehículos al conductor
            var vehiculosAsignados = faker.PickRandom(vehiculosDisponibles, cantidadVehiculos).ToList();

            foreach (var vehiculo in vehiculosAsignados)
            {
                // Crear la relación en la tabla intermedia
                conduceList.Add(new Conduce
                {
                    ConNU = conductor.UsuNU, // ID del conductor
                    VehPtnt = vehiculo.VehPtnt, // Patente del vehículo
                    Conductor = conductor, // Relación de navegación con Conductor
                    Vehiculo = vehiculo // Relación de navegación con Vehículo
                });
            }
        }

        context.Conduces.AddRange(conduceList);
        context.SaveChanges();

        // =========================
        // 16) Pagos (5-10 por playa)
        // =========================
        var pagosList = new List<Pago>();
        foreach (var playa in playas)
        {
            // Obtener los métodos de pago disponibles para esta playa
            var metodosPagoDisponibles = context.AceptaMetodosPago
                .Where(ap => ap.PlyID == playa.PlyID)  // Métodos aceptados por la playa
                .ToList();

            // Seleccionar un número aleatorio de pagos entre 5 y 10 por playa
            int cantidadPagos = faker.Random.Int(5, 10);

            for (int i = 0; i < cantidadPagos; i++)
            {
                // Seleccionar un método de pago aleatorio de los aceptados por la playa
                var metodoPago = faker.PickRandom(metodosPagoDisponibles);

                // Crear un nuevo pago
                var pago = new Pago
                {
                    PlyID = playa.PlyID, // ID de la playa asociada
                    PagNum = i + 1, // Número de pago (podrías usar un contador si prefieres secuencias)
                    MepID = metodoPago.MepID, // ID del método de pago
                    PagMonto = faker.Random.Decimal(100, 5000), // Monto del pago (ajustable según necesidades)
                    PagFyh = faker.Date.Between(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow), // Fecha y hora de pago
                    Playa = playa, // Relación de navegación con Playa
                    MetodoPago = metodoPago.MetodoPago, // Relación de navegación con MetodoPago
                    AceptaMetodoPago = metodoPago // Relación con AceptaMetodoPago
                };

                pagosList.Add(pago);
            }
        }

        context.Pagos.AddRange(pagosList);
        context.SaveChanges();

        // =========================
        // 17) Ocupaciones (2-5 por playa)
        // =========================
        var ocupacionesList = new List<Ocupacion>();
        foreach (var playa in playas)
        {
            // Obtener las plazas disponibles para la playa (filtrar por plazas no ocupadas)
            var plazasDisponibles = context.Plazas
                .Where(p => p.PlyID == playa.PlyID && !p.PlzOcupada) // Solo las plazas no ocupadas
                .ToList();

            // Seleccionar un número aleatorio de ocupaciones entre 2 y 5 por playa
            int cantidadOcupaciones = faker.Random.Int(2, 5);

            for (int i = 0; i < cantidadOcupaciones; i++)
            {
                // Seleccionar una plaza aleatoria para la ocupación
                var plaza = faker.PickRandom(plazasDisponibles);

                // Buscar vehículos compatibles según las clasificaciones asociadas a la plaza
                var clasificaciones = context.PlazasClasificaciones
                    .Where(pc => pc.PlyID == plaza.PlyID && pc.PlzNum == plaza.PlzNum)
                    .Select(pc => pc.ClasVehID)
                    .ToList();

                var vehiculosDisponibles = context.Vehiculos
                    .Where(v => clasificaciones.Contains(v.ClasVehID))
                    .ToList();


                var vehiculo = faker.PickRandom(vehiculosDisponibles); // Elegir un vehículo aleatorio

                // Obtener un método de pago aleatorio (aceptado por la playa)
                var metodoPago = faker.PickRandom(context.AceptaMetodosPago
                    .Where(ap => ap.PlyID == playa.PlyID)
                    .ToList());

                // Verificar si la plaza ya tiene un pago registrado
                var pagoExistente = context.Pagos
                    .FirstOrDefault(p => p.PlyID == playa.PlyID && p.PagNum == i + 1); // Número de pago único por cada ocupación

                if (pagoExistente != null) // Si ya existe un pago
                {
                    // Si el pago ya existe, liberamos la plaza
                    plaza.PlzOcupada = false;
                }
                else
                {
                    // Si no hay pago, ocupamos la plaza
                    plaza.PlzOcupada = true;
                }

                // Crear la ocupación
                var ocupacion = new Ocupacion
                {
                    PlyID = playa.PlyID,
                    PlzNum = plaza.PlzNum,
                    VehPtnt = vehiculo.VehPtnt,
                    OcufFyhIni = faker.Date.Between(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow), // Fecha de ingreso aleatoria
                    OcufFyhFin = faker.Random.Bool() ? (DateTime?)faker.Date.Between(DateTime.UtcNow, DateTime.UtcNow.AddDays(7)) : null, // Fecha de egreso opcional
                    OcuLlavDej = faker.Random.Bool(), // Aleatorio si se dejaron llaves
                    PagNum = faker.Random.Int(1, 100), // Asignar un número de pago aleatorio
                    Plaza = plaza, // Relación con la plaza
                    Vehiculo = vehiculo, // Relación con el vehículo
                    Pago = pagoExistente ?? new Pago // Si hay pago, asignamos el pago, si no, lo dejamos vacío
                    {
                        PlyID = playa.PlyID,
                        PagNum = i + 1, // Número de pago único por cada ocupación
                        MepID = metodoPago.MepID, // Método de pago
                        PagMonto = faker.Random.Decimal(100, 5000), // Monto aleatorio
                        PagFyh = DateTime.Now // Fecha de pago actual
                    }
                };

                // Añadir la ocupación a la lista
                ocupacionesList.Add(ocupacion);
            }
        }

        context.Ocupaciones.AddRange(ocupacionesList);
        context.SaveChanges();


        // =========================
        // 18) Clasificación de días (Entre semana, Fin de semana, Festivos, etc.)
        // =========================
        var clasificacionesDiasList = new List<ClasificacionDias>();


        // Crear datos con Faker para agregar más diversidad y asegurarse de que no haya duplicados
        var tiposDias = new List<string>
        {
            "Lunes a Viernes (Laborables)",
            "Sábado y Domingo (Fin de semana)",
            "Festivos Nacionales",
            "Vacaciones de Invierno",
            "Vacaciones de Verano",
            "Días de descanso programado",
            "Jornadas especiales (eventos)"
        };

        var descripcionesDias = new List<string>
        {
            "De lunes a viernes, con horario laboral habitual, dedicado al trabajo o estudio.",
            "Sábado y domingo, días de descanso y actividades recreativas.",
            "Días festivos nacionales y locales, sin actividad laboral.",
            "Periodo de descanso durante el invierno, usualmente para desconectar del trabajo.",
            "Periodo de descanso durante el verano, ideal para vacaciones y actividades al aire libre.",
            "Días específicos programados para descanso o desconexión laboral, por ejemplo, días de puente.",
            "Jornadas especiales relacionadas a eventos importantes o celebraciones."
        };

        // Agregar las entradas a la lista con Faker, asegurando que cada "ClaDiasID" sea único
        for (int i = 0; i < tiposDias.Count; i++)
        {
            var clasificacionDia = new ClasificacionDias
            {
                // Dejar que el ClaDiasID sea autoincrementable si está configurado así
                ClaDiasTipo = tiposDias[i], // Tipo de día
                ClaDiasDesc = descripcionesDias[i] // Descripción
            };

            clasificacionesDiasList.Add(clasificacionDia);
        }

        // Eliminar los registros existentes antes de agregar los nuevos
        context.ClasificacionesDias.RemoveRange(context.ClasificacionesDias);
        context.SaveChanges();

        // Añadir los datos a la base de datos
        context.ClasificacionesDias.AddRange(clasificacionesDiasList);
        context.SaveChanges();


        // =========================
        // 19) Horarios de atención de una Playa en una Clasificación de días
        // =========================
        var horariosList = new List<Horario>();
        foreach (var playa in playas)
        {
            // Para cada playa, asignamos horarios a cada tipo de día (ClasificacionDias)
            foreach (var clasificacionDia in context.ClasificacionesDias.ToList())
            {
                // Generar entre 1 y 3 franjas horarias por clasificación de días
                int cantidadHorarios = faker.Random.Int(1, 3); // Entre 1 y 3 franjas horarias por día

                for (int i = 0; i < cantidadHorarios; i++)
                {
                    // Hora de inicio aleatoria entre 6:00 AM y 9:00 AM
                    var horaInicio = faker.Date.Between(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow).AddHours(faker.Random.Int(6, 9)).AddMinutes(faker.Random.Int(0, 59));

                    // Hora de fin aleatoria entre 3 y 5 horas después de la hora de inicio
                    var horaFin = horaInicio.AddHours(faker.Random.Int(3, 5));

                    // Crear el horario para la combinación de playa y clasificación de días
                    var horario = new Horario
                    {
                        PlyID = playa.PlyID, // ID de la playa
                        ClaDiasID = clasificacionDia.ClaDiasID, // ID de la clasificación de días
                        HorFyhIni = horaInicio, // Hora de inicio
                        HorFyhFin = horaFin // Hora de fin
                    };

                    horariosList.Add(horario);
                }
            }
        }

        context.Horarios.AddRange(horariosList);
        context.SaveChanges();

// =========================
// 20) Servicio Extra Realizado (Asociar el 30% de los pagos de cada playa)
// =========================
var servicioExtraList = new List<ServicioExtraRealizado>();
foreach (var playa in playas)
{
    // Obtener todos los pagos asociados a la playa
    var pagosDePlaya = context.Pagos
        .Where(p => p.PlyID == playa.PlyID)
        .ToList();

    // Determinar el 30% de los pagos para asociar con un servicio extra realizado
    int cantidadPagos = (int)(pagosDePlaya.Count * 0.30); // 30% de los pagos de la playa
    var pagosSeleccionados = pagosDePlaya.Take(cantidadPagos).ToList(); // Seleccionamos el 30%

    // Para cada pago seleccionado, generar un servicio extra realizado
    foreach (var pago in pagosSeleccionados)
    {
        // Obtener un vehículo aleatorio (asegurándonos de que tenga una patente)
        var vehiculo = faker.PickRandom(context.Vehiculos.Where(v => v.VehPtnt != null).ToList());

        // Seleccionar un servicio extra aleatorio para este pago (servicios disponibles en esta playa)
        var servicioExtra = faker.PickRandom(context.ServiciosProveidos
            .Where(sp => sp.PlyID == playa.PlyID) // Solo servicios de esta playa
            .ToList());

        // Crear un servicio extra realizado
        var servicioExtraRealizado = new ServicioExtraRealizado
        {
            PlyID = playa.PlyID, // Playa asociada
            SerID = servicioExtra.SerID, // Servicio extra asociado
            VehPtnt = vehiculo.VehPtnt, // Patente del vehículo
            ServExFyHIni = faker.Date.Between(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow), // Fecha de inicio aleatoria
            ServExFyHFin = faker.Random.Bool() ? (DateTime?)faker.Date.Between(DateTime.UtcNow, DateTime.UtcNow.AddDays(7)) : null, // Fecha de fin aleatoria
            ServExComp = faker.Random.Bool() ? faker.Lorem.Sentence() : null, // Comentario aleatorio
            PagNum = pago.PagNum, // Número de pago asociado
            ServicioProveido = servicioExtra, // Relación con el servicio extra
            Vehiculo = vehiculo, // Relación con el vehículo
            Pago = pago // Relación con el pago
        };

        // Añadir el servicio extra realizado a la lista
        servicioExtraList.Add(servicioExtraRealizado);
    }
}

// Guardar los servicios extra realizados en la base de datos
context.ServiciosExtrasRealizados.AddRange(servicioExtraList);
context.SaveChanges();

        // =========================
        // 21) Abonos y Períodos de Abono
        // =========================
        var abonosList = new List<Abono>();
        var periodosAbonoList = new List<PeriodoAbono>();
        var vehiculosAbonadosList = new List<VehiculoAbonado>();
        var pagosAbonosList = new List<Pago>();

        // Obtener servicios de tipo "Abono" disponibles
        var serviciosAbono = context.Servicios
            .Where(s => s.SerTipo == "Abono")
            .ToList();

        // Obtener plazas disponibles para abonos (no ocupadas)
        var plazasDisponiblesAbonos = context.Plazas
            .Where(p => !p.PlzOcupada)
            .ToList();

        // Obtener el siguiente número de pago disponible
        var siguientePagNum = context.Pagos.Any() ? context.Pagos.Max(p => p.PagNum) + 1 : 1;

        // Crear exactamente 5 abonos por playa
        int abonadoIndex = 0;
        foreach (var playa in playas)
        {
            // Crear exactamente 5 abonos para esta playa
            for (int i = 0; i < 5; i++)
            {
                // Seleccionar el siguiente abonado disponible
                var abonado = abonados[abonadoIndex % abonados.Count];
                abonadoIndex++;
                
                // Seleccionar una plaza aleatoria disponible de la playa actual
                var plazasDeEstaPlaya = plazasDisponiblesAbonos.Where(p => p.PlyID == playa.PlyID).ToList();
                var plaza = plazasDeEstaPlaya.Any() ? faker.PickRandom(plazasDeEstaPlaya) : faker.PickRandom(plazasDisponiblesAbonos);
                
                // Seleccionar un servicio de abono aleatorio
                var servicioAbono = faker.PickRandom(serviciosAbono);
                
                // Calcular duración del abono basado en el servicio
                int duracionDias = servicioAbono.SerDuracionMinutos.HasValue 
                    ? servicioAbono.SerDuracionMinutos.Value / 1440 // Convertir minutos a días
                    : faker.Random.Int(30, 365); // Fallback: entre 30 días y 1 año
                
                // Fecha de inicio del abono (entre 6 meses atrás y 1 mes adelante)
                var fechaInicio = faker.Date.Between(DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow.AddMonths(1));
                // Normalizar la fecha eliminando milisegundos para evitar problemas de precisión
                fechaInicio = new DateTime(fechaInicio.Year, fechaInicio.Month, fechaInicio.Day, fechaInicio.Hour, fechaInicio.Minute, fechaInicio.Second, DateTimeKind.Utc);
                var fechaFin = fechaInicio.AddDays(duracionDias);
                
                // Determinar estado del abono basado en fechas
                EstadoPago estadoAbono;
                if (fechaFin < DateTime.UtcNow)
                    estadoAbono = faker.Random.Bool(0.8f) ? EstadoPago.Finalizado : EstadoPago.Cancelado;
                else if (fechaInicio > DateTime.UtcNow)
                    estadoAbono = EstadoPago.Activo;
                else
                    estadoAbono = faker.Random.Bool(0.7f) ? EstadoPago.Activo : EstadoPago.Pendiente;
                
                // Calcular monto total del abono
                var tarifaActual = context.TarifasServicio
                    .Where(t => t.PlyID == plaza.PlyID && t.SerID == servicioAbono.SerID && t.TasFecFin == null)
                    .FirstOrDefault();
                
                decimal montoTotal = tarifaActual?.TasMonto ?? faker.Random.Decimal(5000, 50000);
                
                // Crear número de pago único para este abono
                var pagNum = siguientePagNum++;
                
                // Crear períodos de abono (máximo 4 períodos para evitar sobrecarga)
                int duracionPeriodo = faker.PickRandom(new[] { 30, 60, 90 }); // Mensual, bimestral o trimestral
                int cantidadPeriodos = Math.Min((int)Math.Ceiling((double)duracionDias / duracionPeriodo), 4);
                decimal montoPorPeriodo = montoTotal / cantidadPeriodos;
                
                // Crear pago principal del abono PRIMERO
                var metodoPagoPrincipal = faker.PickRandom(context.AceptaMetodosPago
                    .Where(ap => ap.PlyID == plaza.PlyID && ap.AmpHab)
                    .ToList());
                
                var pagoPrincipal = new Pago
                {
                    PlyID = plaza.PlyID,
                    PagNum = pagNum,
                    MepID = metodoPagoPrincipal.MepID,
                    PagMonto = montoTotal,
                    PagFyh = fechaInicio
                    // No asignar propiedades de navegación para evitar conflictos de tracking
                };
                
                pagosAbonosList.Add(pagoPrincipal);
                
                // Crear el abono DESPUÉS del pago
                var abono = new Abono
                {
                    PlyID = plaza.PlyID,
                    PlzNum = plaza.PlzNum,
                    AboFyhIni = fechaInicio,
                    AboFyhFin = fechaFin,
                    AboMonto = montoTotal,
                    AboDNI = abonado.AboDNI,
                    PagNum = pagNum,
                    EstadoPago = estadoAbono
                    // No asignar propiedades de navegación para evitar conflictos de tracking
                };
                
                abonosList.Add(abono);
                
                for (int j = 0; j < cantidadPeriodos; j++)
                {
                    var periodoInicio = fechaInicio.AddDays(j * duracionPeriodo);
                    // Normalizar fechas de período eliminando milisegundos
                    periodoInicio = new DateTime(periodoInicio.Year, periodoInicio.Month, periodoInicio.Day, periodoInicio.Hour, periodoInicio.Minute, periodoInicio.Second, DateTimeKind.Utc);
                    var periodoFin = j == cantidadPeriodos - 1 
                        ? fechaFin 
                        : periodoInicio.AddDays(duracionPeriodo - 1);
                    // Normalizar fecha fin también
                    periodoFin = new DateTime(periodoFin.Year, periodoFin.Month, periodoFin.Day, periodoFin.Hour, periodoFin.Minute, periodoFin.Second, DateTimeKind.Utc);
                    
                    // Determinar si el período está pagado
                    bool periodoPagado = false;
                    DateTime? fechaPago = null;
                    int? pagNumPeriodo = null;
                    
                    if (estadoAbono == EstadoPago.Activo || estadoAbono == EstadoPago.Finalizado)
                    {
                        // Solo crear pagos para algunos períodos para evitar sobrecarga
                        if (j < 2) // Solo los primeros 2 períodos pueden tener pagos
                        {
                            // Si el período ya pasó, probablemente esté pagado
                            if (periodoFin < DateTime.UtcNow)
                            {
                                periodoPagado = faker.Random.Bool(0.8f);
                                if (periodoPagado)
                                {
                                    fechaPago = faker.Date.Between(periodoInicio, periodoFin);
                                    pagNumPeriodo = siguientePagNum++;
                                }
                            }
                            else if (periodoInicio <= DateTime.UtcNow && periodoFin >= DateTime.UtcNow)
                            {
                                // Período actual
                                periodoPagado = faker.Random.Bool(0.6f);
                                if (periodoPagado)
                                {
                                    fechaPago = faker.Date.Between(periodoInicio, DateTime.UtcNow);
                                    pagNumPeriodo = siguientePagNum++;
                                }
                            }
                        }
                    }
                    
                    var periodo = new PeriodoAbono
                    {
                        PlyID = plaza.PlyID,
                        PlzNum = plaza.PlzNum,
                        AboFyhIni = fechaInicio,
                        PeriodoNumero = j + 1,
                        PeriodoFechaInicio = periodoInicio,
                        PeriodoFechaFin = periodoFin,
                        PeriodoMonto = montoPorPeriodo,
                        PeriodoPagado = periodoPagado,
                        PeriodoFechaPago = fechaPago,
                        PagNum = pagNumPeriodo
                        // No asignar Abono para evitar conflictos de tracking
                    };
                    
                    periodosAbonoList.Add(periodo);
                    
                    // Crear pago para el período si está pagado
                    if (periodoPagado && pagNumPeriodo.HasValue)
                    {
                        var metodoPago = faker.PickRandom(context.AceptaMetodosPago
                            .Where(ap => ap.PlyID == plaza.PlyID && ap.AmpHab)
                            .ToList());
                        
                        var pagoPeriodo = new Pago
                        {
                            PlyID = plaza.PlyID,
                            PagNum = pagNumPeriodo.Value,
                            MepID = metodoPago.MepID,
                            PagMonto = montoPorPeriodo,
                            PagFyh = fechaPago.Value
                            // No asignar propiedades de navegación para evitar conflictos de tracking
                        };
                        
                        pagosAbonosList.Add(pagoPeriodo);
                    }
                }
                
                // Asociar vehículos con el abono (máximo 2 vehículos por abono)
                var vehiculosAbonado = context.Vehiculos
                    .AsNoTracking()
                    .Where(v => context.Conduces.Any(c => c.VehPtnt == v.VehPtnt && c.ConNU == abonado.ConNU))
                    .ToList();
                
                if (!vehiculosAbonado.Any())
                {
                    // Si no tiene vehículos asociados como conductor, asignar vehículos aleatorios
                    vehiculosAbonado = faker.PickRandom(context.Vehiculos.AsNoTracking().ToList(), faker.Random.Int(1, 2)).ToList();
                }
                
                // Tomar máximo 2 vehículos únicos para evitar duplicados
                var vehiculosUnicos = vehiculosAbonado.Take(2).ToList();
                
                foreach (var vehiculo in vehiculosUnicos)
                {
                    // Verificar que no existe ya esta asociación
                    var claveExiste = vehiculosAbonadosList.Any(va => 
                        va.PlyID == plaza.PlyID && 
                        va.PlzNum == plaza.PlzNum && 
                        va.AboFyhIni == fechaInicio && 
                        va.VehPtnt == vehiculo.VehPtnt);
                    
                    if (!claveExiste)
                    {
                        var vehiculoAbonado = new VehiculoAbonado
                        {
                            PlyID = plaza.PlyID,
                            PlzNum = plaza.PlzNum,
                            AboFyhIni = fechaInicio,
                            VehPtnt = vehiculo.VehPtnt,
                            Abono = abono
                            // No asignar Vehiculo para evitar conflictos de tracking
                        };
                        
                        vehiculosAbonadosList.Add(vehiculoAbonado);
                    }
                }
                
                // Marcar la plaza como ocupada por el abono
                plaza.PlzOcupada = true;
            }
        }
        
        // Eliminar duplicados antes de guardar
        abonosList = abonosList
            .GroupBy(a => new { a.PlyID, a.PlzNum, a.AboFyhIni })
            .Select(g => g.First())
            .ToList();
            
        periodosAbonoList = periodosAbonoList
            .GroupBy(p => new { p.PlyID, p.PlzNum, p.AboFyhIni, p.PeriodoNumero })
            .Select(g => g.First())
            .ToList();
            
        vehiculosAbonadosList = vehiculosAbonadosList
            .GroupBy(v => new { v.PlyID, v.PlzNum, v.AboFyhIni, v.VehPtnt })
            .Select(g => g.First())
            .ToList();
            
        pagosAbonosList = pagosAbonosList
            .GroupBy(p => new { p.PlyID, p.PagNum })
            .Select(g => g.First())
            .ToList();

        // Guardar todos los datos relacionados con abonos EN EL ORDEN CORRECTO
        // 1. Primero los pagos (en lotes pequeños)
        const int batchSize = 50;
        for (int i = 0; i < pagosAbonosList.Count; i += batchSize)
        {
            var batch = pagosAbonosList.Skip(i).Take(batchSize).ToList();
            context.Pagos.AddRange(batch);
            context.SaveChanges();
        }
        
        // 2. Luego los abonos (que referencian los pagos)
        for (int i = 0; i < abonosList.Count; i += batchSize)
        {
            var batch = abonosList.Skip(i).Take(batchSize).ToList();
            context.Abonos.AddRange(batch);
            context.SaveChanges();
            
            // Asignar propiedades de navegación después de guardar
            foreach (var abono in batch)
            {
                abono.Plaza = context.Plazas.Find(abono.PlyID, abono.PlzNum);
                abono.Abonado = context.Abonados.Find(abono.AboDNI);
                abono.Pago = context.Pagos.Find(abono.PlyID, abono.PagNum);
            }
        }
        
        // 3. Después los períodos (que referencian los abonos)
        for (int i = 0; i < periodosAbonoList.Count; i += batchSize)
        {
            var batch = periodosAbonoList.Skip(i).Take(batchSize).ToList();
            context.PeriodosAbono.AddRange(batch);
            context.SaveChanges();
            
            // Asignar propiedades de navegación después de guardar
            foreach (var periodo in batch)
            {
                periodo.Abono = context.Abonos.Find(periodo.PlyID, periodo.PlzNum, periodo.AboFyhIni);
                if (periodo.PagNum.HasValue)
                {
                    periodo.Pago = context.Pagos.Find(periodo.PlyID, periodo.PagNum.Value);
                }
            }
        }
        
        // 4. Finalmente las asociaciones de vehículos
        for (int i = 0; i < vehiculosAbonadosList.Count; i += batchSize)
        {
            var batch = vehiculosAbonadosList.Skip(i).Take(batchSize).ToList();
            context.VehiculosAbonados.AddRange(batch);
            context.SaveChanges();
            
            // Asignar propiedades de navegación después de guardar
            foreach (var vehiculoAbonado in batch)
            {
                vehiculoAbonado.Abono = context.Abonos.Find(vehiculoAbonado.PlyID, vehiculoAbonado.PlzNum, vehiculoAbonado.AboFyhIni);
                vehiculoAbonado.Vehiculo = context.Vehiculos.Find(vehiculoAbonado.VehPtnt);
            }
        }

    }

    private static decimal Redondear(decimal monto)
        => Math.Round(monto, 2, MidpointRounding.AwayFromZero);
}
