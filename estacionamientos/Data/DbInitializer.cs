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
            new ClasificacionDias { ClaDiasID = 1, ClaDiasTipo = "Días hábiles", ClaDiasDesc = "Lunes a Viernes" },
            new ClasificacionDias { ClaDiasID = 2, ClaDiasTipo = "Fines de semana", ClaDiasDesc = "Sábados y Domingos" },
            new ClasificacionDias { ClaDiasID = 3, ClaDiasTipo = "Feriados", ClaDiasDesc = "Feriados y días no laborables" }
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

        // Resetear la secuencia de UsuNU para que el próximo valor sea 9 (después de los 8 administradores)
        // Esto evita conflictos cuando se crean nuevos usuarios
        try
        {
            context.Database.ExecuteSqlRaw("SELECT setval('\"Usuario_UsuNU_seq\"', (SELECT MAX(\"UsuNU\") FROM \"Usuario\"), true);");
        }
        catch
        {
            // Si falla (por ejemplo, si la secuencia tiene otro nombre), no es crítico
            // El cálculo manual de UsuNU en los controladores debería manejar esto
                                }
                            }
}
