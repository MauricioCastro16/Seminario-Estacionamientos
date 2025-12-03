using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using System.Security.Claims;
using System.Collections.Generic;

namespace estacionamientos.Controllers
{
    // DTO para horarios - formato unificado para el frontend
    public class HorarioDTO
    {
        public string TipoDia { get; set; } = string.Empty;
        public string HoraInicio { get; set; } = string.Empty;
        public string? HoraFin { get; set; }
    }

    // DTO para horarios agrupados
    public class HorarioAgrupadoDTO
    {
        public string horaInicio { get; set; } = string.Empty;
        public string horaFin { get; set; } = string.Empty;
    }

    public class ConductorController : Controller
    {
        private readonly AppDbContext _context;
        public ConductorController(AppDbContext context) => _context = context;

        // Método auxiliar para procesar un horario y convertirlo a DTO
        // IMPORTANTE: Los horarios se guardan usando BuildDate que agrega un TimeSpan a una fecha base UTC (2000-01-01)
        // Usar la MISMA lógica que HorarioController.ReadTime: date.TimeOfDay
        private static TimeSpan ReadTime(DateTime date) => date.TimeOfDay;
        
        private HorarioDTO ProcesarHorario(Horario h)
        {
            try
            {
                if (h == null)
                {
                    return new HorarioDTO
                    {
                        TipoDia = "",
                        HoraInicio = "00:00",
                        HoraFin = "23:59"
                    };
                }

                // Usar ReadTime igual que HorarioController - extrae el TimeSpan del DateTime
                var timeOfDayIni = ReadTime(h.HorFyhIni);
                var timeOfDayFin = h.HorFyhFin.HasValue ? ReadTime(h.HorFyhFin.Value) : (TimeSpan?)null;

                // Convertir a string usando el mismo formato que HorarioController (hh:mm en formato 12h para display)
                // Pero usamos HH:mm para formato 24h como se muestra en la vista del dueño
                var horaInicio = $"{timeOfDayIni.Hours:D2}:{timeOfDayIni.Minutes:D2}";
                var horaFin = timeOfDayFin.HasValue 
                    ? $"{timeOfDayFin.Value.Hours:D2}:{timeOfDayFin.Value.Minutes:D2}"
                    : "23:59"; // Si no hay hora fin, usar 23:59 como máximo

                // Log automático
                Console.WriteLine($"=== HORARIOS DEBUG ===");
                Console.WriteLine($"ClaDiasID: {h.ClasificacionDias?.ClaDiasID ?? 0}");
                Console.WriteLine($"TipoDia: {h.ClasificacionDias?.ClaDiasTipo ?? "NULL"}");
                Console.WriteLine($"fechaOriginal: {h.HorFyhIni:yyyy-MM-dd HH:mm:ss} (Kind: {h.HorFyhIni.Kind})");
                Console.WriteLine($"TimeOfDay raw: {timeOfDayIni}");
                Console.WriteLine($"TimeOfDay horas: {timeOfDayIni.Hours}, minutos: {timeOfDayIni.Minutes}");
                Console.WriteLine($"horaInicioConvertida: {horaInicio}");
                Console.WriteLine($"horaFinConvertida: {horaFin}");

                return new HorarioDTO
                {
                    TipoDia = h.ClasificacionDias?.ClaDiasTipo ?? "",
                    HoraInicio = horaInicio,
                    HoraFin = horaFin
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error procesando horario: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return new HorarioDTO
                {
                    TipoDia = "",
                    HoraInicio = "00:00",
                    HoraFin = "23:59"
                };
            }
        }

        // Método auxiliar para determinar si un horario es Lunes-Viernes o Fin de Semana basado en ClaDiasID
        // ID = 1 → Lunes a Viernes
        // ID = 2 → Fin de semana
        private static bool EsLunesViernes(int? claDiasID, string tipoDia)
        {
            if (claDiasID == 1) return true;
            if (claDiasID == 2) return false;
            
            // Fallback a texto si no hay ID
            if (string.IsNullOrWhiteSpace(tipoDia)) return false;
            var tipoLower = tipoDia.ToLowerInvariant();
            return tipoLower.Contains("ábil") || 
                   tipoLower.Contains("habil") || 
                   tipoLower.Contains("lunes") ||
                   tipoLower.Contains("viernes") ||
                   tipoLower.Contains("días hábiles") ||
                   tipoLower.Contains("dias habiles");
        }

        private static bool EsFinSemana(int? claDiasID, string tipoDia)
        {
            if (claDiasID == 2) return true;
            if (claDiasID == 1) return false;
            
            // Fallback a texto si no hay ID
            if (string.IsNullOrWhiteSpace(tipoDia)) return false;
            var tipoLower = tipoDia.ToLowerInvariant();
            return (tipoLower.Contains("fin") && tipoLower.Contains("semana")) ||
                   tipoLower.Contains("sábado") ||
                   tipoLower.Contains("sabado") ||
                   tipoLower.Contains("domingo") ||
                   tipoLower.Contains("fines de semana");
        }

        public async Task<IActionResult> Index()
            => View(await _context.Conductores.AsNoTracking().ToListAsync());

        public async Task<IActionResult> Details(int id)
        {
            var entity = await _context.Conductores.AsNoTracking().FirstOrDefaultAsync(e => e.UsuNU == id);
            return entity is null ? NotFound() : View(entity);
        }

        public IActionResult Create() => View(new Conductor());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Conductor model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.Conductores.Add(model); // Inserta en Usuario y luego en Conductor (TPT)
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _context.Conductores.FindAsync(id);
            return entity is null ? NotFound() : View(entity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Conductor model)
        {
            if (id != model.UsuNU) return BadRequest();
            if (!ModelState.IsValid) return View(model);
            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Conductores.AsNoTracking().FirstOrDefaultAsync(e => e.UsuNU == id);
            return entity is null ? NotFound() : View(entity);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entity = await _context.Conductores.FindAsync(id);
            if (entity is null) return NotFound();
            _context.Conductores.Remove(entity);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Método para mostrar el mapa del conductor
        public async Task<IActionResult> Mapa()
        {
            try
            {
                // Verificar si el conductor tiene un vehículo seleccionado o favorito
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var conductorId = int.Parse(userId);

                // Verificar si hay un vehículo seleccionado en la sesión
                var vehiculoSeleccionado = HttpContext.Session.GetString("vehiculoSeleccionado");
                bool tieneVehiculo = false;

                if (!string.IsNullOrEmpty(vehiculoSeleccionado))
                {
                    // Verificar que el vehículo seleccionado existe y pertenece al conductor
                    var conduceSeleccionado = await _context.Conduces
                        .Where(c => c.ConNU == conductorId && c.VehPtnt == vehiculoSeleccionado)
                        .AsNoTracking()
                        .AnyAsync();
                    
                    tieneVehiculo = conduceSeleccionado;
                }

                // Si no hay vehículo seleccionado en sesión, verificar si tiene un favorito
                if (!tieneVehiculo)
                {
                    var conduceFavorito = await _context.Conduces
                        .Where(c => c.ConNU == conductorId && c.Favorito)
                        .AsNoTracking()
                        .FirstOrDefaultAsync();
                    
                    if (conduceFavorito != null)
                    {
                        tieneVehiculo = true;
                        // Si hay un favorito pero no está en sesión, guardarlo en sesión automáticamente
                        if (string.IsNullOrEmpty(vehiculoSeleccionado))
                        {
                            HttpContext.Session.SetString("vehiculoSeleccionado", conduceFavorito.VehPtnt);
                        }
                    }
                }

                // Si no tiene vehículo seleccionado ni favorito, verificar si tiene vehículos registrados
                if (!tieneVehiculo)
                {
                    var tieneVehiculosRegistrados = await _context.Conduces
                        .Where(c => c.ConNU == conductorId)
                        .AsNoTracking()
                        .AnyAsync();
                    
                    // Si no tiene vehículos registrados, redirigir a Vehiculos para que agregue uno
                    if (!tieneVehiculosRegistrados)
                    {
                        TempData["Mensaje"] = "Debes agregar y seleccionar un vehículo para usar el mapa.";
                        TempData["TipoMensaje"] = "warning";
                        return RedirectToAction("Vehiculos", "Conductor");
                    }
                    
                    // Si tiene vehículos pero ninguno seleccionado ni favorito, redirigir a Vehiculos para que seleccione uno
                    TempData["Mensaje"] = "Debes seleccionar un vehículo para usar el mapa.";
                    TempData["TipoMensaje"] = "info";
                    return RedirectToAction("Vehiculos", "Conductor");
                }

                // Obtener todas las playas con coordenadas para mostrar en el mapa
                var playas = await _context.Playas
                    .Include(p => p.Horarios)
                        .ThenInclude(h => h.ClasificacionDias)
                    .Include(p => p.Plazas)
                    .Where(p => p.PlyLat.HasValue && p.PlyLon.HasValue)
                    .AsNoTracking()
                    .ToListAsync();

                ViewBag.PlayasCount = playas.Count;
                return View(playas);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new List<PlayaEstacionamiento>());
            }
        }

        // Método para mostrar el historial de visitas (Gestión)
        public async Task<IActionResult> Gestion()
        {
            try
            {
                // Obtener el ID del conductor actual desde las claims
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var conductorId = int.Parse(userId);

                // Obtener todos los vehículos del conductor
                var vehiculos = await _context.Conduces
                    .Include(c => c.Vehiculo)
                        .ThenInclude(v => v.Clasificacion)
                    .Where(c => c.ConNU == conductorId)
                    .Select(c => c.Vehiculo)
                    .Distinct()
                    .ToListAsync();

                // Crear un diccionario para almacenar el historial por vehículo (usando patente como clave)
                var historialPorVehiculo = new Dictionary<string, List<object>>();

                foreach (var vehiculo in vehiculos)
                {
                    var historial = new List<object>();

                    // Obtener todas las ocupaciones de este vehículo con información relacionada
                    var ocupaciones = await _context.Ocupaciones
                        .Include(o => o.Plaza)
                            .ThenInclude(p => p.Playa)
                        .Include(o => o.Pago)
                            .ThenInclude(p => p.MetodoPago)
                        .Where(o => o.VehPtnt == vehiculo.VehPtnt)
                        .OrderByDescending(o => o.OcufFyhIni)
                        .ToListAsync();

                    // Obtener servicios extra realizados de este vehículo
                    var serviciosExtras = await _context.ServiciosExtrasRealizados
                        .Include(s => s.ServicioProveido)
                            .ThenInclude(sp => sp.Servicio)
                        .Include(s => s.ServicioProveido)
                            .ThenInclude(sp => sp.Playa)
                        .Include(s => s.Pago)
                            .ThenInclude(p => p.MetodoPago)
                        .Where(s => s.VehPtnt == vehiculo.VehPtnt)
                        .ToListAsync();

                    // Obtener abonos de este vehículo (solo abonos activos o recientes)
                    var vehiculosAbonados = await _context.VehiculosAbonados
                        .Include(va => va.Abono)
                            .ThenInclude(a => a.Plaza)
                                .ThenInclude(p => p.Playa)
                        .Include(va => va.Abono)
                            .ThenInclude(a => a.Pago)
                                .ThenInclude(p => p.MetodoPago)
                        .Where(va => va.VehPtnt == vehiculo.VehPtnt)
                        .ToListAsync();

                    foreach (var o in ocupaciones)
                    {
                        // Buscar servicios extra asociados a esta ocupación:
                        // 1. Mismo pago (mismo PagNum y PlyID) - mismo vehículo
                        // 2. O durante el tiempo de la ocupación (misma playa, mismo vehículo, durante la ocupación)
                        var serviciosMismoPago = new List<ServicioExtraRealizado>();
                        if (o.Pago != null)
                        {
                            serviciosMismoPago = serviciosExtras
                                .Where(s => s.PlyID == o.PlyID && 
                                           s.VehPtnt == o.VehPtnt &&
                                           s.PagNum == o.Pago.PagNum &&
                                           s.PagNum != null)
                                .ToList();
                        }

                        var serviciosDuranteOcupacion = serviciosExtras
                            .Where(s => s.PlyID == o.PlyID && 
                                       s.VehPtnt == o.VehPtnt &&
                                       s.ServExFyHIni >= o.OcufFyhIni && 
                                       (o.OcufFyhFin == null || s.ServExFyHIni <= o.OcufFyhFin.Value))
                            .ToList();

                        // Combinar ambos grupos (sin duplicados)
                        var serviciosDeEstaOcupacion = serviciosMismoPago
                            .Union(serviciosDuranteOcupacion)
                            .GroupBy(s => new { s.PlyID, s.SerID, s.VehPtnt, s.ServExFyHIni })
                            .Select(g => g.First())
                            .ToList();

                        // Obtener nombres de servicios extra (TODOS los servicios)
                        var serviciosNombres = serviciosDeEstaOcupacion
                            .Select(s => s.ServicioProveido?.Servicio?.SerNom ?? "Servicio Extra")
                            .Distinct()
                            .ToList();

                        // Calcular monto total (ocupación + servicios extra pagados del mismo pago)
                        var montoServicios = serviciosMismoPago
                            .Where(s => s.Pago != null)
                            .Sum(s => s.Pago.PagMonto);
                        var montoTotal = (o.Pago?.PagMonto ?? 0) + montoServicios;

                        // Determinar si hay servicios en curso
                        var tieneServiciosEnCurso = serviciosDeEstaOcupacion.Any(s => s.ServExEstado == "En curso");
                        var tieneServiciosPendientes = serviciosDeEstaOcupacion.Any(s => s.ServExEstado == "Pendiente");

                        // Determinar si hay múltiples servicios (estacionamiento + al menos 1 servicio extra)
                        var cantidadServicios = serviciosNombres.Count;
                        var mostrarVarios = cantidadServicios > 0; // Si hay al menos 1 servicio extra además del estacionamiento
                        
                        // Crear lista completa de servicios incluyendo "Estacionamiento" cuando hay servicios extra
                        var todosLosServicios = new List<string> { "Estacionamiento" };
                        todosLosServicios.AddRange(serviciosNombres);

                        historial.Add(new
                        {
                            PlayaNombre = o.Plaza?.Playa?.PlyNom ?? "Desconocida",
                            PlayaDireccion = o.Plaza?.Playa?.PlyDir ?? "",
                            FechaHoraIngreso = o.OcufFyhIni,
                            FechaHoraEgreso = o.OcufFyhFin,
                            Duracion = o.OcufFyhFin.HasValue 
                                ? (o.OcufFyhFin.Value - o.OcufFyhIni).TotalHours 
                                : (double?)null,
                            MontoPagado = montoTotal,
                            MetodoPago = o.Pago?.MetodoPago?.MepNom ?? "Sin pago",
                            PlazaNumero = o.PlzNum,
                            DejoLlaves = o.OcuLlavDej,
                            Tipo = "Estacionamiento",
                            ServiciosNombres = todosLosServicios, // Incluir "Estacionamiento" + servicios extra
                            MostrarVarios = mostrarVarios,
                            TieneServiciosEnCurso = tieneServiciosEnCurso,
                            TieneServiciosPendientes = tieneServiciosPendientes
                        });
                    }

                    // Agregar servicios extra que NO están asociados a ninguna ocupación
                    // Un servicio está asociado si:
                    // 1. Comparte el mismo PagNum y PlyID con una ocupación (mismo vehículo), O
                    // 2. Está en el rango de tiempo de una ocupación (misma playa, mismo vehículo)
                    var serviciosSinOcupacion = serviciosExtras
                        .Where(s => {
                            // Verificar si está asociado por PagNum
                            var asociadoPorPago = ocupaciones.Any(o => 
                                o.Pago != null && 
                                o.PlyID == s.PlyID && 
                                o.VehPtnt == s.VehPtnt &&
                                o.Pago.PagNum == s.PagNum && 
                                s.PagNum != null);
                            
                            // Verificar si está asociado por tiempo
                            var asociadoPorTiempo = ocupaciones.Any(o => 
                                o.PlyID == s.PlyID && 
                                o.VehPtnt == s.VehPtnt &&
                                s.ServExFyHIni >= o.OcufFyhIni && 
                                (o.OcufFyhFin == null || s.ServExFyHIni <= o.OcufFyhFin.Value));
                            
                            return !asociadoPorPago && !asociadoPorTiempo;
                        })
                        .ToList();

                    foreach (var s in serviciosSinOcupacion)
                    {
                        historial.Add(new
                        {
                            PlayaNombre = s.ServicioProveido?.Playa?.PlyNom ?? "Desconocida",
                            PlayaDireccion = s.ServicioProveido?.Playa?.PlyDir ?? "",
                            FechaHoraIngreso = s.ServExFyHIni,
                            FechaHoraEgreso = (DateTime?)null,
                            Duracion = (double?)null,
                            MontoPagado = s.Pago?.PagMonto ?? 0,
                            MetodoPago = s.Pago?.MetodoPago?.MepNom ?? "Sin pago",
                            PlazaNumero = (int?)null,
                            DejoLlaves = false,
                            Tipo = s.ServicioProveido?.Servicio?.SerNom ?? "Servicio Extra",
                            ServiciosNombres = new List<string>(),
                            MostrarVarios = false,
                            TieneServiciosEnCurso = s.ServExEstado == "En curso",
                            TieneServiciosPendientes = s.ServExEstado == "Pendiente"
                        });
                    }

                    // Agregar abonos
                    foreach (var va in vehiculosAbonados)
                    {
                        historial.Add(new
                        {
                            PlayaNombre = va.Abono.Plaza?.Playa?.PlyNom ?? "Desconocida",
                            PlayaDireccion = va.Abono.Plaza?.Playa?.PlyDir ?? "",
                            FechaHoraIngreso = va.Abono.AboFyhIni,
                            FechaHoraEgreso = va.Abono.AboFyhFin,
                            Duracion = va.Abono.AboFyhFin.HasValue 
                                ? (va.Abono.AboFyhFin.Value - va.Abono.AboFyhIni).TotalDays 
                                : (double?)null,
                            MontoPagado = 0, // No mostrar monto
                            MetodoPago = "", // No mostrar método de pago
                            PlazaNumero = va.PlzNum,
                            DejoLlaves = false,
                            Tipo = "Abono",
                            ServiciosNombres = new List<string>(),
                            MostrarVarios = false,
                            TieneServiciosEnCurso = false,
                            TieneServiciosPendientes = false,
                            EstadoAbono = va.Abono.EstadoPago.ToString() // EstadoPago es un enum
                        });
                    }

                    // Ordenar todo el historial por fecha de ingreso descendente
                    historial = historial.OrderByDescending(h => ((dynamic)h).FechaHoraIngreso).ToList();

                    historialPorVehiculo[vehiculo.VehPtnt] = historial;
                }

                ViewBag.HistorialPorVehiculo = historialPorVehiculo;
                return View(vehiculos);
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new List<Vehiculo>());
            }
        }

        // Método para mostrar vehículos del conductor
        public async Task<IActionResult> Vehiculos()
        {
            try
            {
                // Obtener el ID del conductor actual desde las claims
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var conductorId = int.Parse(userId);

                // Debug: Log para verificar el conductorId
                Console.WriteLine($"Conductor ID: {conductorId}");

                // Obtener los vehículos del conductor actual
                var conduces = await _context.Conduces
                    .Include(c => c.Vehiculo)
                        .ThenInclude(v => v.Clasificacion)
                    .Where(c => c.ConNU == conductorId)
                    .AsNoTracking()
                    .ToListAsync();

                // Debug: Log para verificar cuántos vehículos se encontraron
                Console.WriteLine($"Vehículos encontrados: {conduces.Count}");
                foreach (var conduce in conduces)
                {
                    Console.WriteLine($"Vehículo: {conduce.VehPtnt}");
                }

                // Obtener el vehículo favorito para preselección
                var vehiculoFavorito = conduces.FirstOrDefault(c => c.Favorito)?.VehPtnt;

                // Obtener clasificaciones de vehículos para el modal
                var clasificaciones = await _context.ClasificacionesVehiculo
                    .OrderBy(c => c.ClasVehTipo)
                    .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = c.ClasVehID.ToString(),
                        Text = c.ClasVehTipo
                    })
                    .ToListAsync();

                ViewBag.VehiculoFavorito = vehiculoFavorito;
                ViewBag.Conduces = conduces;
                ViewBag.ClasVehID = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(clasificaciones, "Value", "Text");

                return View(conduces.Select(c => c.Vehiculo).ToList());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en Vehiculos: {ex.Message}");
                ViewBag.Error = ex.Message;
                return View(new List<Vehiculo>());
            }
        }

        // API para obtener detalles de un vehículo específico
        [HttpGet]
        public async Task<IActionResult> GetVehiculoDetalle(string patente)
        {
            try
            {
                var vehiculo = await _context.Vehiculos
                    .Include(v => v.Clasificacion)
                    .FirstOrDefaultAsync(v => v.VehPtnt == patente);

                if (vehiculo == null)
                {
                    return Json(new { error = "Vehículo no encontrado" });
                }

                return Json(new
                {
                    patente = vehiculo.VehPtnt,
                    marca = vehiculo.VehMarc,
                    tipo = vehiculo.Clasificacion?.ClasVehTipo ?? "Sin clasificación",
                    descripcion = vehiculo.Clasificacion?.ClasVehDesc ?? "Sin descripción"
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // API para obtener el vehículo favorito del conductor
        [HttpGet]
        public async Task<IActionResult> GetVehiculoFavorito()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Json(new { error = "Usuario no autenticado" });
                }

                var conductorId = int.Parse(userId);

                // Obtener el vehículo favorito del conductor
                var conduceFavorito = await _context.Conduces
                    .Include(c => c.Vehiculo)
                        .ThenInclude(v => v.Clasificacion)
                    .Where(c => c.ConNU == conductorId && c.Favorito)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (conduceFavorito?.Vehiculo == null)
                {
                    return Json(new { error = "No hay vehículo favorito" });
                }

                return Json(new
                {
                    patente = conduceFavorito.Vehiculo.VehPtnt,
                    marca = conduceFavorito.Vehiculo.VehMarc,
                    tipo = conduceFavorito.Vehiculo.Clasificacion?.ClasVehTipo ?? "Sin clasificación"
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // API para obtener el vehículo seleccionado en la sesión (o favorito si no hay selección)
        [HttpGet]
        public async Task<IActionResult> GetVehiculoSeleccionado()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Json(new { error = "Usuario no autenticado" });
                }

                var conductorId = int.Parse(userId);

                // Primero verificar si hay un vehículo seleccionado en la sesión
                var vehiculoSeleccionado = HttpContext.Session.GetString("vehiculoSeleccionado");
                
                if (!string.IsNullOrEmpty(vehiculoSeleccionado))
                {
                    // Buscar el vehículo seleccionado
                    var conduceSeleccionado = await _context.Conduces
                        .Include(c => c.Vehiculo)
                            .ThenInclude(v => v.Clasificacion)
                        .Where(c => c.ConNU == conductorId && c.VehPtnt == vehiculoSeleccionado)
                        .AsNoTracking()
                        .FirstOrDefaultAsync();

                    if (conduceSeleccionado?.Vehiculo != null)
                    {
                        return Json(new
                        {
                            patente = conduceSeleccionado.Vehiculo.VehPtnt,
                            marca = conduceSeleccionado.Vehiculo.VehMarc,
                            tipo = conduceSeleccionado.Vehiculo.Clasificacion?.ClasVehTipo ?? "Sin clasificación",
                            esFavorito = conduceSeleccionado.Favorito
                        });
                    }
                }

                // Si no hay vehículo seleccionado en sesión, usar el favorito
                var conduceFavorito = await _context.Conduces
                    .Include(c => c.Vehiculo)
                        .ThenInclude(v => v.Clasificacion)
                    .Where(c => c.ConNU == conductorId && c.Favorito)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (conduceFavorito?.Vehiculo == null)
                {
                    return Json(new { error = "No hay vehículo disponible" });
                }

                // Guardar el favorito como seleccionado en la sesión
                HttpContext.Session.SetString("vehiculoSeleccionado", conduceFavorito.Vehiculo.VehPtnt);

                return Json(new
                {
                    patente = conduceFavorito.Vehiculo.VehPtnt,
                    marca = conduceFavorito.Vehiculo.VehMarc,
                    tipo = conduceFavorito.Vehiculo.Clasificacion?.ClasVehTipo ?? "Sin clasificación",
                    esFavorito = true
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // API para seleccionar un vehículo en la sesión
        [HttpPost]
        public async Task<IActionResult> SeleccionarVehiculo(string patente)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Json(new { error = "Usuario no autenticado" });
                }

                var conductorId = int.Parse(userId);

                // Verificar que el conductor tenga acceso a este vehículo
                var conduce = await _context.Conduces
                    .Include(c => c.Vehiculo)
                        .ThenInclude(v => v.Clasificacion)
                    .Where(c => c.ConNU == conductorId && c.VehPtnt == patente)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (conduce?.Vehiculo == null)
                {
                    return Json(new { error = "Vehículo no encontrado o sin acceso" });
                }

                // Guardar en la sesión
                HttpContext.Session.SetString("vehiculoSeleccionado", patente);

                return Json(new
                {
                    success = true,
                    patente = conduce.Vehiculo.VehPtnt,
                    marca = conduce.Vehiculo.VehMarc,
                    tipo = conduce.Vehiculo.Clasificacion?.ClasVehTipo ?? "Sin clasificación",
                    esFavorito = conduce.Favorito
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // API para desvincular un vehículo del conductor
        [HttpPost]
        public async Task<IActionResult> DesvincularVehiculo(string patente)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Json(new { success = false, error = "Usuario no autenticado" });
                }

                var conductorId = int.Parse(userId);

                // Buscar la relación conductor-vehículo
                var conduce = await _context.Conduces
                    .Include(c => c.Vehiculo)
                    .FirstOrDefaultAsync(c => c.ConNU == conductorId && c.VehPtnt == patente);

                if (conduce == null)
                {
                    return Json(new { success = false, error = "Vehículo no encontrado o sin acceso" });
                }

                // Contar vehículos antes de desvincular
                var totalVehiculos = await _context.Conduces
                    .CountAsync(c => c.ConNU == conductorId);

                bool eraFavorito = conduce.Favorito;

                // Remover la relación
                _context.Conduces.Remove(conduce);
                await _context.SaveChangesAsync();

                // Si era favorito o si queda solo un vehículo, marcar el restante como favorito
                var vehiculosRestantes = await _context.Conduces
                    .CountAsync(c => c.ConNU == conductorId);
                
                if (eraFavorito || vehiculosRestantes == 1)
                {
                    var nuevoFavorito = await _context.Conduces
                        .FirstOrDefaultAsync(c => c.ConNU == conductorId);
                    
                    if (nuevoFavorito != null)
                    {
                        nuevoFavorito.Favorito = true;
                        await _context.SaveChangesAsync();
                        
                        // Actualizar la sesión con el nuevo favorito
                        HttpContext.Session.SetString("vehiculoSeleccionado", nuevoFavorito.VehPtnt);
                    }
                }

                // Limpiar la sesión si el vehículo desvinculado era el seleccionado
                var vehiculoSeleccionado = HttpContext.Session.GetString("vehiculoSeleccionado");
                if (vehiculoSeleccionado == patente)
                {
                    HttpContext.Session.Remove("vehiculoSeleccionado");
                }

                return Json(new { 
                    success = true, 
                    message = "Vehículo desvinculado correctamente",
                    eraFavorito = eraFavorito
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // API para crear un nuevo vehículo y asociarlo al conductor
        [HttpPost]
        public async Task<IActionResult> CrearVehiculo(string patente, string marca, int clasificacionId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Json(new { success = false, error = "Usuario no autenticado" });
                }

                var conductorId = int.Parse(userId);

                if (string.IsNullOrWhiteSpace(patente))
                {
                    return Json(new { success = false, error = "La patente es obligatoria" });
                }

                if (string.IsNullOrWhiteSpace(marca))
                {
                    return Json(new { success = false, error = "La marca es obligatoria" });
                }

                if (clasificacionId <= 0)
                {
                    return Json(new { success = false, error = "Debes seleccionar un tipo de vehículo" });
                }

                var clasificacion = await _context.ClasificacionesVehiculo
                    .FirstOrDefaultAsync(c => c.ClasVehID == clasificacionId);

                if (clasificacion == null)
                {
                    return Json(new { success = false, error = "Debes seleccionar un tipo de vehículo" });
                }

                // Verificar si el vehículo ya existe
                var vehiculoExistente = await _context.Vehiculos
                    .FirstOrDefaultAsync(v => v.VehPtnt == patente);

                if (vehiculoExistente != null)
                {
                    // Si el vehículo ya existe, verificar si ya está asociado a este conductor
                    var yaAsociado = await _context.Conduces
                        .AnyAsync(c => c.ConNU == conductorId && c.VehPtnt == patente);

                    if (yaAsociado)
                    {
                        return Json(new { success = false, error = "Ya tienes este vehículo asociado" });
                    }

                    // Si existe pero no está asociado, crear la relación
                    var nuevaRelacion = new Conduce
                    {
                        ConNU = conductorId,
                        VehPtnt = patente,
                        Favorito = false // Se marcará como favorito si es el primero
                    };

                    _context.Conduces.Add(nuevaRelacion);
                }
                else
                {
                    // Crear nuevo vehículo
                    var nuevoVehiculo = new Vehiculo
                    {
                        VehPtnt = patente,
                        VehMarc = marca,
                        ClasVehID = clasificacionId
                    };

                    _context.Vehiculos.Add(nuevoVehiculo);

                    // Crear la relación conductor-vehículo
                    var nuevaRelacion = new Conduce
                    {
                        ConNU = conductorId,
                        VehPtnt = patente,
                        Favorito = false // Se marcará como favorito si es el primero
                    };

                    _context.Conduces.Add(nuevaRelacion);
                }

                await _context.SaveChangesAsync();

                // Si es el primer vehículo del conductor, marcarlo como favorito
                var totalVehiculos = await _context.Conduces
                    .CountAsync(c => c.ConNU == conductorId);

                if (totalVehiculos == 1)
                {
                    var primerVehiculo = await _context.Conduces
                        .FirstOrDefaultAsync(c => c.ConNU == conductorId);
                    
                    if (primerVehiculo != null)
                    {
                        primerVehiculo.Favorito = true;
                        await _context.SaveChangesAsync();
                        
                        // Seleccionarlo en la sesión
                        HttpContext.Session.SetString("vehiculoSeleccionado", patente);
                    }
                }

                return Json(new { 
                    success = true, 
                    message = "Vehículo agregado correctamente",
                    patente = patente,
                    marca = marca,
                    clasificacion = clasificacion.ClasVehTipo
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }


        // API para cambiar el vehículo favorito
        [HttpPost]
        public async Task<IActionResult> CambiarFavorito(string patente)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Json(new { success = false, error = "Usuario no autenticado" });
                }

                var conductorId = int.Parse(userId);

                // Obtener todos los vehículos del conductor
                var conduces = await _context.Conduces
                    .Where(c => c.ConNU == conductorId)
                    .ToListAsync();

                // Verificar si el vehículo ya es favorito
                var conduceSeleccionado = conduces.FirstOrDefault(c => c.VehPtnt == patente);
                bool yaEraFavorito = conduceSeleccionado?.Favorito ?? false;

                // Quitar el favorito de todos los vehículos
                foreach (var conduce in conduces)
                {
                    conduce.Favorito = false;
                }

                // Si no era favorito, marcarlo como favorito
                if (!yaEraFavorito && conduceSeleccionado != null)
                {
                    conduceSeleccionado.Favorito = true;
                }

                await _context.SaveChangesAsync();

                string mensaje = yaEraFavorito ? "Vehículo removido de favoritos" : "Vehículo marcado como favorito";
                
                // Si se marcó como favorito, también seleccionarlo en la sesión
                if (!yaEraFavorito && conduceSeleccionado != null)
                {
                    HttpContext.Session.SetString("vehiculoSeleccionado", patente);
                }
                
                // Obtener información del vehículo para el frontend
                var vehiculoInfo = new { marca = "N/A", tipo = "N/A" };
                if (!yaEraFavorito && conduceSeleccionado?.Vehiculo != null)
                {
                    vehiculoInfo = new { 
                        marca = conduceSeleccionado.Vehiculo.VehMarc,
                        tipo = conduceSeleccionado.Vehiculo.Clasificacion?.ClasVehTipo ?? "Sin clasificación"
                    };
                }
                
                return Json(new { 
                    success = true, 
                    message = mensaje,
                    esFavorito = !yaEraFavorito,
                    marca = vehiculoInfo.marca,
                    tipo = vehiculoInfo.tipo
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // API endpoint para obtener playas cercanas
        [HttpGet]
        public async Task<IActionResult> GetPlayasCercanas(decimal lat, decimal lon, int radioKm = 10)
        {
            try
            {
                // INCLUIR horarios directamente en el query inicial - SIN AsNoTracking para asegurar carga correcta
                // Filtrar playas que tengan coordenadas
                // Primero intentar obtener solo playas Vigentes, si no hay ninguna, incluir también las Ocultas
                var playasVigentesCount = await _context.Playas
                    .CountAsync(p => p.PlyLat.HasValue && p.PlyLon.HasValue && p.PlyEstado == EstadoPlaya.Vigente);
                
                // Si no hay playas Vigentes, incluir también las Ocultas (para debugging/desarrollo)
                var playas = playasVigentesCount > 0
                    ? await _context.Playas
                        .Include(p => p.Plazas)
                        .Include(p => p.Horarios)
                            .ThenInclude(h => h.ClasificacionDias)
                        .Where(p => p.PlyLat.HasValue && p.PlyLon.HasValue && p.PlyEstado == EstadoPlaya.Vigente)
                        .ToListAsync()
                    : await _context.Playas
                        .Include(p => p.Plazas)
                        .Include(p => p.Horarios)
                            .ThenInclude(h => h.ClasificacionDias)
                        .Where(p => p.PlyLat.HasValue && p.PlyLon.HasValue && (p.PlyEstado == EstadoPlaya.Vigente || p.PlyEstado == EstadoPlaya.Oculto))
                        .ToListAsync();
                
                if (playasVigentesCount == 0 && playas.Count > 0)
                {
                    Console.WriteLine("⚠️ No se encontraron playas Vigentes, mostrando también playas Ocultas para debugging");
                }

                System.Diagnostics.Debug.WriteLine($"GetPlayasCercanas - Playas encontradas en BD: {playas.Count}");
                Console.WriteLine($"GetPlayasCercanas - Playas encontradas en BD: {playas.Count}");
                
                // Log adicional para debugging
                if (playas.Count == 0)
                {
                    var todasPlayas = await _context.Playas.CountAsync();
                    var playasConCoordenadas = await _context.Playas.CountAsync(p => p.PlyLat.HasValue && p.PlyLon.HasValue);
                    var playasVigentes = await _context.Playas.CountAsync(p => p.PlyEstado == EstadoPlaya.Vigente);
                    var playasOcultas = await _context.Playas.CountAsync(p => p.PlyEstado == EstadoPlaya.Oculto);
                    
                    Console.WriteLine($"DEBUG - Total playas en BD: {todasPlayas}");
                    Console.WriteLine($"DEBUG - Playas con coordenadas: {playasConCoordenadas}");
                    Console.WriteLine($"DEBUG - Playas Vigentes: {playasVigentes}");
                    Console.WriteLine($"DEBUG - Playas Ocultas: {playasOcultas}");
                }
                
                // FORZAR carga explícita de horarios para cada playa si no se cargaron
                foreach (var p in playas)
                {
                    if (p.Horarios == null || !p.Horarios.Any())
                    {
                        await _context.Entry(p)
                            .Collection(pl => pl.Horarios)
                            .Query()
                            .Include(h => h.ClasificacionDias)
                            .LoadAsync();
                    }
                }

                // Calcular promedio de tarifas por hora para cada playa
                var playasCercanas = new List<object>();
                foreach (var p in playas)
                {
                    try
                    {
                        // Convertir horarios a formato unificado DTO
                        var horariosDto = new List<HorarioDTO>();
                        try
                        {
                            if (p.Horarios != null && p.Horarios.Any())
                            {
                                foreach (var h in p.Horarios)
                                {
                                    if (h == null) continue;
                                    
                                    // Asegurar que ClasificacionDias esté cargado
                                    if (h.ClasificacionDias == null)
                                    {
                                        await _context.Entry(h)
                                            .Reference(x => x.ClasificacionDias)
                                            .LoadAsync();
                                    }
                                    
                                    // Procesar horario usando método auxiliar (sin conversiones UTC/Local)
                                    var horarioDto = ProcesarHorario(h);
                                    if (horarioDto != null)
                                    {
                                        horariosDto.Add(horarioDto);
                                    }
                                }
                            }
                        }
                        catch (Exception exHorarios)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error procesando horarios para playa {p.PlyID}: {exHorarios.Message}");
                            horariosDto = new List<HorarioDTO>(); // Continuar sin horarios (array vacío, NO null)
                        }
                        
                        // Calcular promedio con manejo de errores
                        decimal valorPromedioHora = 0;
                        try
                        {
                            valorPromedioHora = await CalcularPromedioTarifaHora(p.PlyID);
                        }
                        catch (Exception exPromedio)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error en CalcularPromedioTarifaHora para playa {p.PlyID}: {exPromedio.Message}");
                            valorPromedioHora = 0;
                        }
                        
                        // Calcular estado abierto/cerrado con manejo de errores
                        bool estaAbierto = false;
                        try
                        {
                            estaAbierto = EstaAbierto(p);
                        }
                        catch (Exception exEstaAbierto)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error en EstaAbierto para playa {p.PlyID}: {exEstaAbierto.Message}");
                            estaAbierto = false; // Por defecto, asumir cerrado si hay error
                        }
                        
                        // Calcular disponibilidad con manejo de errores
                        int disponibilidad = 0;
                        try
                        {
                            disponibilidad = CalcularDisponibilidad(p);
                        }
                        catch (Exception exDisponibilidad)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error en CalcularDisponibilidad para playa {p.PlyID}: {exDisponibilidad.Message}");
                            disponibilidad = 0;
                        }
                        
                        // Calcular distancia con manejo de errores
                        double distancia = 0;
                        try
                        {
                            distancia = CalcularDistancia(lat, lon, p.PlyLat!.Value, p.PlyLon!.Value);
                        }
                        catch (Exception exDistancia)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error en CalcularDistancia para playa {p.PlyID}: {exDistancia.Message}");
                            distancia = 0;
                        }
                        
                    // Convertir coordenadas a formato decimal correcto si están almacenadas como enteros
                    decimal? latCorrecta = p.PlyLat;
                    decimal? lonCorrecta = p.PlyLon;
                    
                    // Si las coordenadas son muy grandes (sin punto decimal), convertirlas
                    if (p.PlyLat.HasValue && Math.Abs(p.PlyLat.Value) > 1000)
                    {
                        latCorrecta = p.PlyLat.Value / 1000000m;
                        Console.WriteLine($"⚠️ Coordenada lat corregida: {p.PlyLat.Value} -> {latCorrecta.Value}");
                    }
                    if (p.PlyLon.HasValue && Math.Abs(p.PlyLon.Value) > 1000)
                    {
                        lonCorrecta = p.PlyLon.Value / 1000000m;
                        Console.WriteLine($"⚠️ Coordenada lon corregida: {p.PlyLon.Value} -> {lonCorrecta.Value}");
                    }
                    
                    playasCercanas.Add(new
                    {
                        p.PlyID,
                            plyNom = p.PlyNom ?? "",
                            plyDir = p.PlyDir ?? "",
                            plyTipoPiso = p.PlyTipoPiso ?? "",
                        plyValProm = valorPromedioHora,
                        plyLat = latCorrecta,
                        plyLon = lonCorrecta,
                            distancia = distancia,
                            disponibilidad = disponibilidad,
                            estaAbierto = estaAbierto,
                            horarios = horariosDto // Incluir horarios en formato unificado
                        });
                    }
                    catch (Exception exPlaya)
                    {
                        // Si hay error procesando una playa, loguear y continuar con la siguiente
                        System.Diagnostics.Debug.WriteLine($"Error procesando playa {p.PlyID}: {exPlaya.Message} - {exPlaya.StackTrace}");
                        // Continuar con la siguiente playa en lugar de fallar todo
                        continue;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"GetPlayasCercanas - Total playas encontradas: {playas.Count}, Playas procesadas: {playasCercanas.Count}");
                return Json(playasCercanas);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetPlayasCercanas - ERROR: {ex.Message} - {ex.StackTrace}");
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
        // --------- FAVORITOS: DTO interno ---------
        public class CrearUbicacionFavoritaRequest
        {
            public string Apodo { get; set; } = string.Empty;
            public string Provincia { get; set; } = string.Empty;
            public string Ciudad { get; set; } = string.Empty;
            public string Direccion { get; set; } = string.Empty;
            public string? Tipo { get; set; }   // "Playa" o null/otro para ubicaciones
            public decimal Lat { get; set; }
            public decimal Lon { get; set; }
            public int? PlyID { get; set; }   // ID de la playa si es tipo "Playa"
        }

        // --------- FAVORITOS: obtener todas las ubicaciones del conductor ---------
        [HttpGet]
        public async Task<IActionResult> GetUbicacionesFavoritas()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Json(new { error = "Usuario no autenticado" });
            }

            var conductorId = int.Parse(userId);

            // Obtener ubicaciones favoritas (no playas)
            var ubicacionesFavoritas = await _context.UbicacionesFavoritas
                .Where(u => u.ConNU == conductorId)
                .AsNoTracking()
                .Select(u => new
                {
                    apodo = u.UbfApodo,
                    provincia = u.UbfProv,
                    ciudad = u.UbfCiu,
                    direccion = u.UbfDir,
                    tipo = "Ubicación",
                    UbfLat = u.UbfLat,
                    UbfLon = u.UbfLon,
                    plyID = (int?)null
                })
                .ToListAsync();

            // Obtener playas favoritas desde Valoracion
            var playasFavoritas = await _context.Valoraciones
                .Include(v => v.Playa)
                .Where(v => v.ConNU == conductorId && v.ValFav == true)
                .AsNoTracking()
                .Select(v => new
                {
                    apodo = v.ValApodo ?? v.Playa.PlyNom,
                    provincia = v.Playa.PlyProv,
                    ciudad = v.Playa.PlyCiu,
                    direccion = v.Playa.PlyDir,
                    tipo = "Playa",
                    UbfLat = v.Playa.PlyLat ?? 0,
                    UbfLon = v.Playa.PlyLon ?? 0,
                    plyID = (int?)v.PlyID
                })
                .ToListAsync();

            // Combinar ambas listas
            var todasLasFavoritas = ubicacionesFavoritas.Concat(playasFavoritas).ToList();

            return Json(todasLasFavoritas);
        }

        // --------- FAVORITOS: crear una ubicación favorita desde el mapa ---------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarUbicacionFavorita([FromBody] CrearUbicacionFavoritaRequest model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Json(new { success = false, error = "Usuario no autenticado" });
                }

                var conductorId = int.Parse(userId);

                // Si es una playa, guardar en Valoracion
                if (model.Tipo == "Playa" && model.PlyID.HasValue)
                {
                    if (string.IsNullOrWhiteSpace(model.Apodo))
                    {
                        var playa = await _context.Playas.FindAsync(model.PlyID.Value);
                        model.Apodo = playa?.PlyNom ?? $"Playa-{DateTime.Now:HHmmss}";
                    }

                    // Buscar si ya existe una valoración para esta playa
                    var valoracionExistente = await _context.Valoraciones
                        .FirstOrDefaultAsync(v => v.PlyID == model.PlyID.Value && v.ConNU == conductorId);

                    if (valoracionExistente != null)
                    {
                        // Actualizar la valoración existente
                        valoracionExistente.ValFav = true;
                        valoracionExistente.ValApodo = model.Apodo;
                        // Si no tiene estrellas, asignar un valor por defecto
                        if (valoracionExistente.ValNumEst == 0)
                        {
                            valoracionExistente.ValNumEst = 5;
                        }
                    }
                    else
                    {
                        // Crear nueva valoración
                        var nuevaValoracion = new Valoracion
                        {
                            PlyID = model.PlyID.Value,
                            ConNU = conductorId,
                            ValFav = true,
                            ValApodo = model.Apodo,
                            ValNumEst = 5  // Valor por defecto
                        };
                        _context.Valoraciones.Add(nuevaValoracion);
                    }

                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Playa de estacionamiento favorita guardada correctamente."
                    });
                }
                else
                {
                    // Es una ubicación normal, guardar en UbicacionFavorita
                if (string.IsNullOrWhiteSpace(model.Apodo))
                {
                    model.Apodo = $"Favorito-{DateTime.Now:HHmmss}";
                }

                // Validar y truncar apodo si excede el límite
                if (model.Apodo.Length > 50)
                {
                    model.Apodo = model.Apodo.Substring(0, 50);
                }

                // Validar duplicado por (ConNU, Apodo)
                bool yaExiste = await _context.UbicacionesFavoritas
                    .AnyAsync(u => u.ConNU == conductorId && u.UbfApodo == model.Apodo);

                if (yaExiste)
                {
                    return Json(new
                    {
                        success = false,
                        error = "Ya tienes una ubicación con ese apodo."
                    });
                }

                // Validar y proporcionar valores por defecto para campos requeridos
                var provincia = string.IsNullOrWhiteSpace(model.Provincia) ? "Chaco" : model.Provincia.Trim();
                var ciudad = string.IsNullOrWhiteSpace(model.Ciudad) ? "Resistencia" : model.Ciudad.Trim();
                var direccion = string.IsNullOrWhiteSpace(model.Direccion) 
                    ? $"Lat: {model.Lat}, Lon: {model.Lon}" 
                    : model.Direccion.Trim();

                // Truncar campos si exceden el límite
                if (provincia.Length > 50) provincia = provincia.Substring(0, 50);
                if (ciudad.Length > 80) ciudad = ciudad.Substring(0, 80);
                if (direccion.Length > 120) direccion = direccion.Substring(0, 120);

                var entidad = new UbicacionFavorita
                {
                    ConNU = conductorId,
                    UbfApodo = model.Apodo,
                    UbfProv = provincia,
                    UbfCiu = ciudad,
                    UbfDir = direccion,
                    UbfLat = model.Lat,
                    UbfLon = model.Lon
                };

                _context.UbicacionesFavoritas.Add(entidad);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Ubicación favorita guardada correctamente."
                });
                }
            }
            catch (DbUpdateException dbEx)
            {
                // Capturar excepciones de base de datos con más detalle
                var innerException = dbEx.InnerException?.Message ?? dbEx.Message;
                return Json(new { 
                    success = false, 
                    error = $"Error al guardar en la base de datos: {innerException}" 
                });
            }
            catch (Exception ex)
            {
                // Capturar cualquier otra excepción
                var innerException = ex.InnerException?.Message ?? ex.Message;
                return Json(new { 
                    success = false, 
                    error = $"Error: {innerException}" 
                });
            }
        }

        // --------- FAVORITOS: eliminar una favorita por apodo ---------
        [HttpDelete]
        public async Task<IActionResult> EliminarUbicacionFavorita(string apodo, string? tipo = null, int? plyID = null)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Json(new { success = false, error = "Usuario no autenticado" });
                }

                var conductorId = int.Parse(userId);

                // Si es una playa, buscar en Valoracion
                if (tipo == "Playa")
                {
                    Valoracion? valoracion;
                    
                    // Si tenemos el plyID, usarlo para identificar la playa específica
                    if (plyID.HasValue)
                    {
                        valoracion = await _context.Valoraciones
                            .FirstOrDefaultAsync(v => v.ConNU == conductorId && v.PlyID == plyID.Value && v.ValFav == true);
                    }
                    else
                    {
                        // Si no hay plyID, buscar por apodo (puede haber ambigüedad si hay múltiples con el mismo apodo)
                        valoracion = await _context.Valoraciones
                            .FirstOrDefaultAsync(v => v.ConNU == conductorId && v.ValFav == true && v.ValApodo == apodo);
                    }

                    if (valoracion == null)
                    {
                        return Json(new { success = false, error = "Playa favorita no encontrada" });
                    }

                    // Solo quitar el favorito, no eliminar la valoración completa
                    valoracion.ValFav = false;
                    valoracion.ValApodo = null;
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Playa de estacionamiento favorita eliminada." });
                }
                else
                {
                    // Es una ubicación normal
                var entity = await _context.UbicacionesFavoritas
                    .FindAsync(conductorId, apodo);

                if (entity == null)
                {
                    return Json(new { success = false, error = "Ubicación no encontrada" });
                }

                _context.UbicacionesFavoritas.Remove(entity);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Ubicación favorita eliminada." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
        // --------- FAVORITOS: editar (renombrar) una ubicación favorita ---------
        [HttpPost]
        public async Task<IActionResult> EditarUbicacionFavorita([FromBody] EditarUbicacionFavoritaRequest model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                    return Json(new { success = false, error = "Usuario no autenticado" });

                var conductorId = int.Parse(userId);

                if (string.IsNullOrWhiteSpace(model.ApodoActual) || string.IsNullOrWhiteSpace(model.NuevoApodo))
                    return Json(new { success = false, error = "Los nombres no pueden estar vacíos." });

                model.NuevoApodo = model.NuevoApodo.Trim();

                // Si es una playa, buscar en Valoracion
                if (model.Tipo == "Playa")
                {
                    Valoracion? valoracion;
                    
                    // Si tenemos el plyID, usarlo para identificar la playa específica
                    if (model.PlyID.HasValue)
                    {
                        valoracion = await _context.Valoraciones
                            .FirstOrDefaultAsync(v => v.ConNU == conductorId && v.PlyID == model.PlyID.Value && v.ValFav == true);
                    }
                    else
                    {
                        // Si no hay plyID, buscar por apodo (puede haber ambigüedad si hay múltiples con el mismo apodo)
                        valoracion = await _context.Valoraciones
                            .FirstOrDefaultAsync(v => v.ConNU == conductorId && v.ValFav == true && v.ValApodo == model.ApodoActual);
                    }

                    if (valoracion == null)
                        return Json(new { success = false, error = "No se encontró la playa favorita." });

                    // Verificar duplicado (excluyendo la playa actual si tenemos plyID)
                    bool yaExiste = await _context.Valoraciones
                        .AnyAsync(v => v.ConNU == conductorId && v.ValFav == true && v.ValApodo == model.NuevoApodo && 
                                      (!model.PlyID.HasValue || v.PlyID != model.PlyID.Value));

                    if (yaExiste)
                        return Json(new { success = false, error = "Ya tenés otra playa con ese nombre." });

                    // Actualizar el apodo
                    valoracion.ValApodo = model.NuevoApodo;
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Playa de estacionamiento renombrada correctamente." });
                }
                else
                {
                    // Es una ubicación normal
                var favorito = await _context.UbicacionesFavoritas
                    .FirstOrDefaultAsync(u => u.ConNU == conductorId && u.UbfApodo == model.ApodoActual);

                if (favorito == null)
                    return Json(new { success = false, error = "No se encontró la ubicación favorita." });

                // Verificar duplicado
                bool yaExiste = await _context.UbicacionesFavoritas
                    .AnyAsync(u => u.ConNU == conductorId && u.UbfApodo == model.NuevoApodo);

                if (yaExiste)
                    return Json(new { success = false, error = "Ya tenés otra ubicación con ese nombre." });

                // Crear una nueva con el nuevo apodo
                var nueva = new UbicacionFavorita
                {
                    ConNU = favorito.ConNU,
                    UbfApodo = model.NuevoApodo,
                    UbfProv = favorito.UbfProv,
                    UbfCiu = favorito.UbfCiu,
                    UbfDir = favorito.UbfDir,
                    UbfLat = favorito.UbfLat,
                    UbfLon = favorito.UbfLon
                };

                _context.UbicacionesFavoritas.Add(nueva);

                // Eliminar la anterior
                _context.UbicacionesFavoritas.Remove(favorito);

                // Guardar cambios
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Ubicación renombrada correctamente." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }


        public class EditarUbicacionFavoritaRequest
        {
            public string ApodoActual { get; set; } = string.Empty;
            public string NuevoApodo { get; set; } = string.Empty;
            public string? Tipo { get; set; }   // "Playa" o null para ubicaciones
            public int? PlyID { get; set; }   // ID de la playa si es tipo "Playa"
        }


        // Endpoint temporal para debug de horarios - MEJORADO para ver en navegador
        [HttpGet]
        public async Task<IActionResult> DebugHorariosPlaya(int id)
        {
            try
            {
                var playa = await _context.Playas
                    .Include(p => p.Horarios)
                        .ThenInclude(h => h.ClasificacionDias)
                    .FirstOrDefaultAsync(p => p.PlyID == id);

                if (playa == null)
                    return Json(new { error = "Playa no encontrada", playaID = id });

                var ahora = DateTime.Now;
                var tipoDia = (ahora.DayOfWeek >= DayOfWeek.Monday && ahora.DayOfWeek <= DayOfWeek.Friday) 
                    ? "Hábil" 
                    : "Fin de semana";

                // Información DETALLADA de cada horario - CONSISTENTE con GetDetallePlaya
                // NO hacer conversiones UTC/Local, usar solo TimeOfDay
                var horariosDetalle = playa.Horarios != null && playa.Horarios.Any()
                    ? playa.Horarios.Select(h => 
                    {
                        // Usar TimeOfDay directamente - SIN conversiones
                        var timeOfDayIni = h.HorFyhIni.TimeOfDay;
                        var timeOfDayFin = h.HorFyhFin?.TimeOfDay;
                        
                        return new
                        {
                            claDiasID = h.ClasificacionDias?.ClaDiasID ?? 0,
                            tipoDia = h.ClasificacionDias?.ClaDiasTipo ?? "NULL",
                            // VALORES RAW desde la BD
                            fechaOriginal = h.HorFyhIni.ToString("yyyy-MM-dd HH:mm:ss"),
                            horFyhIniKind = h.HorFyhIni.Kind.ToString(),
                            timeOfDay = timeOfDayIni.ToString(@"hh\:mm\:ss"),
                            horFyhFinRaw = h.HorFyhFin?.ToString("yyyy-MM-dd HH:mm:ss") ?? "NULL",
                            horFyhFinTimeOfDay = timeOfDayFin?.ToString(@"hh\:mm\:ss") ?? "NULL",
                            // VALORES CONVERTIDOS (usando TimeOfDay directamente)
                            horaInicioConvertida = timeOfDayIni.ToString(@"HH\:mm"),
                            horaFinConvertida = timeOfDayFin != null ? timeOfDayFin.Value.ToString(@"HH\:mm") : "23:59",
                            coincide = h.ClasificacionDias?.ClaDiasTipo?.Equals(tipoDia, StringComparison.OrdinalIgnoreCase) == true
                        };
                    }).ToList()
                    : new List<object>().Select(x => new { claDiasID = 0, tipoDia = "", fechaOriginal = "", horFyhIniKind = "", timeOfDay = "", horFyhFinRaw = "", horFyhFinTimeOfDay = "", horaInicioConvertida = "", horaFinConvertida = "", coincide = false }).ToList();

                return Json(new
                {
                    playaID = playa.PlyID,
                    playaNombre = playa.PlyNom,
                    horaActual = ahora.ToString("HH:mm:ss"),
                    diaActual = ahora.DayOfWeek.ToString(),
                    tipoDiaBuscado = tipoDia,
                    totalHorarios = playa.Horarios?.Count ?? 0,
                    horarios = horariosDetalle,
                    estaAbierto = EstaAbierto(playa)
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // API endpoint para obtener detalles de una playa específica
        [HttpGet]
        public async Task<IActionResult> GetDetallePlaya(int id)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetDetallePlaya - Recibido ID: {id} (tipo: {id.GetType()})");
                
                if (id <= 0)
                {
                    return Json(new { error = "ID de playa inválido" });
                }
                
                // INCLUIR horarios directamente en el query inicial - SIN AsNoTracking para asegurar carga correcta
                var playa = await _context.Playas
                    .Include(p => p.Plazas)
                    .Include(p => p.Horarios)
                        .ThenInclude(h => h.ClasificacionDias)
                    .FirstOrDefaultAsync(p => p.PlyID == id);
                    
                if (playa == null)
                {
                    System.Diagnostics.Debug.WriteLine($"GetDetallePlaya - Playa con ID {id} no encontrada");
                    return Json(new { error = "Playa no encontrada" });
                }
                
                // FORZAR carga explícita de horarios si no se cargaron
                if (playa.Horarios == null || !playa.Horarios.Any())
                {
                    await _context.Entry(playa)
                        .Collection(p => p.Horarios)
                        .Query()
                        .Include(h => h.ClasificacionDias)
                        .LoadAsync();
                }
                
                System.Diagnostics.Debug.WriteLine($"GetDetallePlaya - Playa encontrada: {playa.PlyNom}, Horarios: {playa.Horarios?.Count ?? 0}");
                
                // DEBUG: Verificar que los horarios se cargaron correctamente
                if (playa.Horarios != null && playa.Horarios.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"GetDetallePlaya - Primer horario sample: HorFyhIni={playa.Horarios.First().HorFyhIni}, TimeOfDay={playa.Horarios.First().HorFyhIni.TimeOfDay}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"GetDetallePlaya - ⚠️ No hay horarios cargados para la playa {playa.PlyID}");
                }

                // Calcular promedio de tarifas por hora
                var valorPromedioHora = await CalcularPromedioTarifaHora(playa.PlyID);

                // Convertir todos los horarios a formato unificado DTO
                Console.WriteLine("=== HORARIOS DEBUG ===");
                Console.WriteLine($"GetDetallePlaya - Total horarios en BD: {playa.Horarios?.Count ?? 0}");
                
                var horariosDto = new List<HorarioDTO>();
                if (playa.Horarios != null && playa.Horarios.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"GetDetallePlaya - Procesando {playa.Horarios.Count} horarios");
                    foreach (var h in playa.Horarios)
                    {
                        try
                        {
                            if (h == null) continue;
                            
                            // Asegurar que ClasificacionDias esté cargado
                            if (h.ClasificacionDias == null)
                            {
                                await _context.Entry(h)
                                    .Reference(x => x.ClasificacionDias)
                                    .LoadAsync();
                            }
                            
                            Console.WriteLine($"  Procesando horario - ClaDiasID: {h.ClasificacionDias?.ClaDiasID ?? 0}, TipoDia: {h.ClasificacionDias?.ClaDiasTipo ?? "NULL"}");
                            
                            // Procesar horario usando método auxiliar (sin conversiones UTC/Local)
                            var horarioDto = ProcesarHorario(h);
                            if (horarioDto != null && !string.IsNullOrWhiteSpace(horarioDto.HoraInicio))
                            {
                                horariosDto.Add(horarioDto);
                                Console.WriteLine($"  ✅ Horario agregado a DTO - TipoDia: {horarioDto.TipoDia}, Inicio: {horarioDto.HoraInicio}, Fin: {horarioDto.HoraFin}");
                            }
                            else
                            {
                                Console.WriteLine($"  ⚠️ Horario NO agregado - HoraInicio vacía o null");
                            }
                        }
                        catch (Exception exHorario)
                        {
                            Console.WriteLine($"  ❌ Error procesando horario: {exHorario.Message}");
                            System.Diagnostics.Debug.WriteLine($"Error procesando horario individual en GetDetallePlaya: {exHorario.Message} - {exHorario.StackTrace}");
                            // Continuar con el siguiente horario
                            continue;
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ GetDetallePlaya - No hay horarios o la colección es null");
                    System.Diagnostics.Debug.WriteLine($"GetDetallePlaya - No hay horarios o la colección es null");
                }
                
                Console.WriteLine($"GetDetallePlaya - Horarios procesados: {horariosDto.Count}");
                System.Diagnostics.Debug.WriteLine($"GetDetallePlaya - Horarios cargados: {playa.Horarios?.Count ?? 0}, HorariosDTO: {horariosDto.Count}");

                // DEBUG: Información detallada de los horarios para diagnóstico
                var debugHorarios = new List<object>();
                if (playa.Horarios != null && playa.Horarios.Any())
                {
                    foreach (var h in playa.Horarios)
                    {
                        try
                        {
                            var timeOfDayIni = h.HorFyhIni.TimeOfDay;
                            var timeOfDayFin = h.HorFyhFin?.TimeOfDay;
                            
                            debugHorarios.Add(new
                            {
                                claDiasID = h.ClasificacionDias?.ClaDiasID ?? 0,
                                tipoDia = h.ClasificacionDias?.ClaDiasTipo ?? "NULL",
                                fechaOriginal = h.HorFyhIni.ToString("yyyy-MM-dd HH:mm:ss"),
                                horFyhIniKind = h.HorFyhIni.Kind.ToString(),
                                timeOfDay = $"{timeOfDayIni.Hours:D2}:{timeOfDayIni.Minutes:D2}:{timeOfDayIni.Seconds:D2}",
                                horaInicioConvertida = $"{timeOfDayIni.Hours:D2}:{timeOfDayIni.Minutes:D2}",
                                horFyhFinRaw = h.HorFyhFin?.ToString("yyyy-MM-dd HH:mm:ss") ?? "NULL",
                                horFyhFinTimeOfDay = timeOfDayFin.HasValue 
                                    ? $"{timeOfDayFin.Value.Hours:D2}:{timeOfDayFin.Value.Minutes:D2}:{timeOfDayFin.Value.Seconds:D2}"
                                    : "NULL",
                                horaFinConvertida = timeOfDayFin.HasValue 
                                    ? $"{timeOfDayFin.Value.Hours:D2}:{timeOfDayFin.Value.Minutes:D2}"
                                    : "23:59"
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error creando debugHorarios para horario: {ex.Message}");
                            // Continuar con el siguiente horario
                            continue;
                        }
                    }
                }

                // Agrupaciones por tipo de día usando ClaDiasID directamente desde los horarios originales
                // PROCESAR DIRECTAMENTE DESDE LOS HORARIOS ORIGINALES - NO FILTRAR
                var horariosLV = new List<HorarioAgrupadoDTO>();
                var horariosFS = new List<HorarioAgrupadoDTO>();
                
                Console.WriteLine($"=== AGRUPACIÓN HORARIOS ===");
                Console.WriteLine($"Total horarios en BD para agrupar: {playa.Horarios?.Count ?? 0}");
                
                if (playa.Horarios != null && playa.Horarios.Any())
                {
                    foreach (var horarioOriginal in playa.Horarios)
                    {
                        try
                        {
                            // Asegurar que ClasificacionDias esté cargado
                            if (horarioOriginal.ClasificacionDias == null)
                            {
                                await _context.Entry(horarioOriginal)
                                    .Reference(x => x.ClasificacionDias)
                                    .LoadAsync();
                            }
                            
                            var claDiasID = horarioOriginal.ClasificacionDias?.ClaDiasID;
                            var tipoDia = horarioOriginal.ClasificacionDias?.ClaDiasTipo ?? "";
                            
                            // LOG DETALLADO DEL HORARIO ORIGINAL ANTES DE PROCESAR
                            Console.WriteLine($"  === PROCESANDO HORARIO ORIGINAL ===");
                            Console.WriteLine($"  ClaDiasID: {claDiasID}, TipoDia: {tipoDia}");
                            Console.WriteLine($"  HorFyhIni RAW: {horarioOriginal.HorFyhIni}");
                            Console.WriteLine($"  HorFyhIni Kind: {horarioOriginal.HorFyhIni.Kind}");
                            Console.WriteLine($"  HorFyhIni ToString: {horarioOriginal.HorFyhIni:yyyy-MM-dd HH:mm:ss}");
                            
                            // Usar ReadTime igual que HorarioController - ANTES de ProcesarHorario
                            var timeOfDayIni = ReadTime(horarioOriginal.HorFyhIni);
                            var timeOfDayFin = horarioOriginal.HorFyhFin.HasValue ? ReadTime(horarioOriginal.HorFyhFin.Value) : (TimeSpan?)null;
                            
                            Console.WriteLine($"  TimeOfDay Inicio: {timeOfDayIni} (Horas: {timeOfDayIni.Hours}, Minutos: {timeOfDayIni.Minutes}, TotalHoras: {timeOfDayIni.TotalHours})");
                            if (timeOfDayFin.HasValue)
                            {
                                Console.WriteLine($"  TimeOfDay Fin: {timeOfDayFin.Value} (Horas: {timeOfDayFin.Value.Hours}, Minutos: {timeOfDayFin.Value.Minutes})");
                            }
                            else
                            {
                                Console.WriteLine($"  TimeOfDay Fin: NULL");
                            }
                            
                            // Procesar el horario para obtener las horas - SIEMPRE procesar, no filtrar
                            var horarioDto = ProcesarHorario(horarioOriginal);
                            
                            // Validar que tenga horas válidas
                            if (string.IsNullOrWhiteSpace(horarioDto.HoraInicio) || (horarioDto.HoraInicio == "00:00" && timeOfDayIni.TotalHours > 0))
                            {
                                Console.WriteLine($"  ⚠️ ADVERTENCIA: Horario procesado tiene horaInicio='{horarioDto.HoraInicio}' pero TimeOfDay tiene {timeOfDayIni.TotalHours} horas");
                                // Corregir usando el TimeOfDay directo
                                horarioDto.HoraInicio = $"{timeOfDayIni.Hours:D2}:{timeOfDayIni.Minutes:D2}";
                            }
                            if (string.IsNullOrWhiteSpace(horarioDto.HoraFin))
                            {
                                if (timeOfDayFin.HasValue)
                                {
                                    horarioDto.HoraFin = $"{timeOfDayFin.Value.Hours:D2}:{timeOfDayFin.Value.Minutes:D2}";
                                }
                                else
                                {
                                    Console.WriteLine($"  ⚠️ Horario sin horaFin, usando 23:59");
                                    horarioDto.HoraFin = "23:59";
                                }
                            }
                            
                            Console.WriteLine($"  ✅ Horario procesado FINAL - Inicio: {horarioDto.HoraInicio}, Fin: {horarioDto.HoraFin}");
                            
                            if (EsLunesViernes(claDiasID, tipoDia))
                            {
                                horariosLV.Add(new HorarioAgrupadoDTO
                                {
                                    horaInicio = horarioDto.HoraInicio,
                                    horaFin = horarioDto.HoraFin
                                });
                                Console.WriteLine($"  ✅ Agregado a L-V: {horarioDto.HoraInicio} - {horarioDto.HoraFin}");
                            }
                            else if (EsFinSemana(claDiasID, tipoDia))
                            {
                                horariosFS.Add(new HorarioAgrupadoDTO
                                {
                                    horaInicio = horarioDto.HoraInicio,
                                    horaFin = horarioDto.HoraFin
                                });
                                Console.WriteLine($"  ✅ Agregado a F-S: {horarioDto.HoraInicio} - {horarioDto.HoraFin}");
                            }
                            else
                            {
                                Console.WriteLine($"  ⚠️ Horario NO clasificado - ClaDiasID: {claDiasID}, TipoDia: {tipoDia}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"  ❌ Error procesando horario para agrupar: {ex.Message}");
                            continue;
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"  ⚠️ No hay horarios para agrupar");
                }
                
                Console.WriteLine($"=== RESUMEN AGRUPACIÓN ===");
                Console.WriteLine($"Horarios L-V: {horariosLV.Count}");
                Console.WriteLine($"Horarios F-S: {horariosFS.Count}");
                foreach (var h in horariosLV)
                {
                    Console.WriteLine($"  L-V: {h.horaInicio} - {h.horaFin}");
                }
                foreach (var h in horariosFS)
                {
                    Console.WriteLine($"  F-S: {h.horaInicio} - {h.horaFin}");
                }

                // Asegurar que las plazas estén cargadas para calcular disponibilidad
                if (playa.Plazas == null)
                {
                    await _context.Entry(playa)
                        .Collection(p => p.Plazas)
                        .LoadAsync();
                }

                // Calcular disponibilidad
                var disponibilidad = CalcularDisponibilidad(playa);

                // Calcular estado abierto/cerrado
                var estaAbierto = EstaAbierto(playa);

                // Construir resultado asegurando que todos los campos estén presentes
                // GARANTIZAR que los arrays nunca sean null - usar arrays vacíos si no hay datos
                var resultado = new
                {
                    plyID = playa.PlyID,
                    plyNom = playa.PlyNom ?? "",
                    plyDir = playa.PlyDir ?? "",
                    plyTipoPiso = playa.PlyTipoPiso ?? "",
                    plyValProm = valorPromedioHora,
                    plyLat = playa.PlyLat,
                    plyLon = playa.PlyLon,
                    disponibilidad = disponibilidad,
                    estaAbierto = estaAbierto,
                    horarios = horariosDto ?? new List<HorarioDTO>(), // Formato unificado - nunca null
                    horariosLunesViernes = horariosLV, // Ya garantizado que no es null
                    horariosFinSemana = horariosFS, // Ya garantizado que no es null
                    // DEBUG: Información detallada para diagnóstico (solo en desarrollo)
                    _debugHorarios = debugHorarios
                };
                
                // LOG FINAL CRÍTICO - Verificar qué se está enviando
                Console.WriteLine($"=== JSON FINAL QUE SE ENVÍA ===");
                Console.WriteLine($"horariosLunesViernes.Count: {horariosLV.Count}");
                Console.WriteLine($"horariosFinSemana.Count: {horariosFS.Count}");
                Console.WriteLine($"horarios.Count: {horariosDto.Count}");
                
                if (horariosLV.Count > 0)
                {
                    Console.WriteLine($"PRIMER HORARIO L-V: horaInicio='{horariosLV[0].horaInicio}', horaFin='{horariosLV[0].horaFin}'");
                    Console.WriteLine($"TODOS HORARIOS L-V:");
                    foreach (var h in horariosLV)
                    {
                        Console.WriteLine($"  - horaInicio='{h.horaInicio}', horaFin='{h.horaFin}'");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ NO HAY HORARIOS L-V PARA ENVIAR");
                }
                
                if (horariosFS.Count > 0)
                {
                    Console.WriteLine($"PRIMER HORARIO F-S: horaInicio='{horariosFS[0].horaInicio}', horaFin='{horariosFS[0].horaFin}'");
                    Console.WriteLine($"TODOS HORARIOS F-S:");
                    foreach (var h in horariosFS)
                    {
                        Console.WriteLine($"  - horaInicio='{h.horaInicio}', horaFin='{h.horaFin}'");
                    }
                }
                else
                {
                    Console.WriteLine($"⚠️ NO HAY HORARIOS F-S PARA ENVIAR");
                }

                // Log para debug
                Console.WriteLine($"=== RESUMEN FINAL ===");
                Console.WriteLine($"PlayID: {playa.PlyID}");
                Console.WriteLine($"Total horarios DTO: {horariosDto.Count}");
                Console.WriteLine($"Horarios L-V: {horariosLV.Count}");
                Console.WriteLine($"Horarios F-S: {horariosFS.Count}");
                Console.WriteLine($"Esta abierto: {estaAbierto}");
                
                System.Diagnostics.Debug.WriteLine($"GetDetallePlaya - PlayID: {playa.PlyID}, Total horarios: {horariosDto.Count}, L-V: {horariosLV.Count}, F-S: {horariosFS.Count}, Abierto: {estaAbierto}");
                
                // Log detallado de horarios
                Console.WriteLine("  Horarios DTO:");
                foreach (var h in horariosDto)
                {
                    Console.WriteLine($"    - TipoDia='{h.TipoDia}', Inicio='{h.HoraInicio}', Fin='{h.HoraFin}'");
                    System.Diagnostics.Debug.WriteLine($"  Horario: TipoDia='{h.TipoDia}', Inicio='{h.HoraInicio}', Fin='{h.HoraFin}'");
                }
                
                Console.WriteLine("  Horarios L-V agrupados:");
                foreach (var h in horariosLV)
                {
                    Console.WriteLine($"    - Inicio='{h.horaInicio}', Fin='{h.horaFin}'");
                    System.Diagnostics.Debug.WriteLine($"  LV: Inicio='{h.horaInicio}', Fin='{h.horaFin}'");
                }
                
                Console.WriteLine("  Horarios F-S agrupados:");
                foreach (var h in horariosFS)
                {
                    Console.WriteLine($"    - Inicio='{h.horaInicio}', Fin='{h.horaFin}'");
                    System.Diagnostics.Debug.WriteLine($"  FS: Inicio='{h.horaInicio}', Fin='{h.horaFin}'");
                }

                return Json(resultado);
            }
            catch (Exception ex)
            {
                // Retornar error detallado para debug
                return Json(new { 
                    error = ex.Message, 
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message 
                });
            }
        }

        // Métodos auxiliares
        private static double CalcularDistancia(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            const double R = 6371; // Radio de la Tierra en km
            var dLat = ToRadians((double)(lat2 - lat1));
            var dLon = ToRadians((double)(lon2 - lon1));
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double ToRadians(double degrees) => degrees * (Math.PI / 180);

        private static int CalcularDisponibilidad(PlayaEstacionamiento playa)
        {
            if (!playa.Plazas.Any()) return 0;
            var totalPlazas = playa.Plazas.Count;
            var plazasDisponibles = playa.Plazas.Count(p => p.PlzHab && !p.PlzOcupada);
            return (int)Math.Round((double)plazasDisponibles / totalPlazas * 100);
        }

        private bool EstaAbierto(PlayaEstacionamiento playa)
        {
            try
            {
                // FORZAR carga de horarios SIEMPRE (sin condiciones)
                _context.Entry(playa)
                    .Collection(p => p.Horarios)
                    .Query()
                    .Include(h => h.ClasificacionDias)
                    .Load();
                
                // Verificar que existan horarios
                if (playa.Horarios == null || !playa.Horarios.Any())
                {
                    return false;
                }

                // Hora actual del sistema - usar TimeOfDay directamente
                var ahora = DateTime.Now;
                var actual = ahora.TimeOfDay;
                var diaActual = ahora.DayOfWeek;
                
                // Determinar si hoy es día hábil (Lunes a Viernes) o fin de semana (Sábado / Domingo)
                bool esDiaHabil = diaActual >= DayOfWeek.Monday && diaActual <= DayOfWeek.Friday;
                
                // Buscar horarios del día actual usando ClaDiasID
                var horariosRelevantes = new List<Horario>();
                
                foreach (var h in playa.Horarios)
                {
                    // Asegurar que ClasificacionDias esté cargado
                    if (h.ClasificacionDias == null)
                    {
                        _context.Entry(h)
                            .Reference(x => x.ClasificacionDias)
                            .Load();
                    }
                    
                    var tipoDia = h.ClasificacionDias?.ClaDiasTipo ?? string.Empty;
                    var tipoDiaID = h.ClasificacionDias?.ClaDiasID;
                    
                    // Usar ClaDiasID para determinar el tipo (1 = L-V, 2 = F-S)
                    bool esHabilHorario = EsLunesViernes(tipoDiaID, tipoDia);
                    bool esFinSemanaHorario = EsFinSemana(tipoDiaID, tipoDia);

                    if (esDiaHabil && esHabilHorario)
                    {
                        horariosRelevantes.Add(h);
                        var inicioStr = h.HorFyhIni.TimeOfDay.ToString(@"HH\:mm");
                        var finStr = h.HorFyhFin != null ? h.HorFyhFin.Value.TimeOfDay.ToString(@"HH\:mm") : "23:59";
                        System.Diagnostics.Debug.WriteLine($"EstaAbierto - Agregado horario HÁBIL: {inicioStr} - {finStr}");
                    }
                    else if (!esDiaHabil && esFinSemanaHorario)
                    {
                        horariosRelevantes.Add(h);
                        var inicioStr = h.HorFyhIni.TimeOfDay.ToString(@"HH\:mm");
                        var finStr = h.HorFyhFin != null ? h.HorFyhFin.Value.TimeOfDay.ToString(@"HH\:mm") : "23:59";
                        System.Diagnostics.Debug.WriteLine($"EstaAbierto - Agregado horario FIN DE SEMANA: {inicioStr} - {finStr}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"EstaAbierto - Horarios relevantes encontrados: {horariosRelevantes.Count}");

                if (!horariosRelevantes.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"EstaAbierto - No hay horarios relevantes para el día actual");
                    return false;
                }

                // Verificar si la hora actual está dentro de algún rango horario
                // Comparar TimeOfDay directamente, SIN conversiones
                foreach (var horario in horariosRelevantes)
                {
                    // Usar TimeOfDay directamente - NO hacer conversiones
                    var inicio = horario.HorFyhIni.TimeOfDay;
                    var fin = horario.HorFyhFin != null 
                        ? horario.HorFyhFin.Value.TimeOfDay
                        : new TimeSpan(23, 59, 0);
                    
                    System.Diagnostics.Debug.WriteLine($"EstaAbierto - Comparando: actual={actual}, inicio={inicio}, fin={fin}");
                    
                    // Comparación directa: actual >= inicio && actual <= fin
                    bool dentro = actual >= inicio && actual <= fin;
                    
                    if (inicio > fin)
                    {
                        // Horario que cruza medianoche (ej: 22:00 - 06:00)
                        dentro = actual >= inicio || actual <= fin;
                    }
                    
                    if (dentro)
                    {
                        System.Diagnostics.Debug.WriteLine($"EstaAbierto - Playa ABIERTA (dentro del rango {inicio} - {fin})");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                // Log del error para debug
                System.Diagnostics.Debug.WriteLine($"Error en EstaAbierto para playa {playa.PlyID}: {ex.Message}");
                return false;
            }
        }

        // Endpoint para obtener tarifas y servicios disponibles de una playa
        [HttpGet]
        public async Task<IActionResult> GetTarifasYServicios(int id, string? tipoVehiculo = null)
        {
            try
            {
                var ahora = DateTime.UtcNow;

                // Obtener servicios disponibles (habilitados)
                var servicios = await _context.ServiciosProveidos
                    .Include(sp => sp.Servicio)
                    .Where(sp => sp.PlyID == id && sp.SerProvHab)
                    .Select(sp => new
                    {
                        serID = sp.SerID,
                        serNom = sp.Servicio.SerNom,
                        serDesc = sp.Servicio.SerDesc,
                        serTipo = sp.Servicio.SerTipo,
                        serDuracionMinutos = sp.Servicio.SerDuracionMinutos
                    })
                    .Distinct()
                    .ToListAsync();

                // Obtener tarifas vigentes agrupadas por servicio y clasificación de vehículo
                var tarifasRaw = await _context.TarifasServicio
                    .Include(t => t.ServicioProveido)
                        .ThenInclude(sp => sp.Servicio)
                    .Include(t => t.ClasificacionVehiculo)
                    .Where(t => t.PlyID == id &&
                               t.TasFecIni <= ahora &&
                               (t.TasFecFin == null || t.TasFecFin >= ahora) &&
                               t.ServicioProveido.SerProvHab)
                    .ToListAsync();

                // Filtrar por tipo de vehículo si se proporciona
                if (!string.IsNullOrEmpty(tipoVehiculo))
                {
                    tarifasRaw = tarifasRaw
                        .Where(t => t.ClasificacionVehiculo != null && 
                                   t.ClasificacionVehiculo.ClasVehTipo == tipoVehiculo)
                        .ToList();
                }

                var tarifas = tarifasRaw
                    .GroupBy(t => new { t.SerID, ClasVehTipo = t.ClasificacionVehiculo?.ClasVehTipo ?? "Sin clasificación" })
                    .Select(g => new
                    {
                        serID = g.Key.SerID,
                        clasificacionVehiculo = g.Key.ClasVehTipo,
                        monto = g.OrderByDescending(t => t.TasFecIni).First().TasMonto
                    })
                    .ToList();

                // Filtrar servicios que tienen tarifas para el tipo de vehículo (si se especificó)
                if (!string.IsNullOrEmpty(tipoVehiculo))
                {
                    var serviciosConTarifasParaVehiculo = tarifas.Select(t => t.serID).Distinct().ToList();
                    servicios = servicios.Where(s => serviciosConTarifasParaVehiculo.Contains(s.serID)).ToList();
                }

                // Agrupar tarifas por servicio
                var serviciosConTarifas = servicios.Select(s => new
                {
                    s.serID,
                    s.serNom,
                    s.serDesc,
                    s.serTipo,
                    s.serDuracionMinutos,
                    tarifas = tarifas
                        .Where(t => t.serID == s.serID)
                        .Select(t => new { t.clasificacionVehiculo, t.monto })
                        .ToList()
                }).ToList();

                return Json(new { servicios = serviciosConTarifas });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Endpoint para obtener valoraciones de una playa
        [HttpGet]
        public async Task<IActionResult> GetValoraciones(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var conductorId = userId != null ? int.Parse(userId) : (int?)null;

                // Obtener todas las valoraciones de la playa
                var valoraciones = await _context.Valoraciones
                    .Include(v => v.Conductor)
                    .Where(v => v.PlyID == id)
                    .OrderByDescending(v => v.ValNumEst)
                    .ThenByDescending(v => v.ValFav)
                    .Select(v => new
                    {
                        conNU = v.ConNU,
                        conductorNombre = v.Conductor.UsuNyA,
                        valNumEst = v.ValNumEst,
                        valFav = v.ValFav,
                        valComentario = v.ValComentario,
                        esMia = conductorId.HasValue && v.ConNU == conductorId.Value
                    })
                    .ToListAsync();

                // Calcular promedio
                var promedio = valoraciones.Any() 
                    ? (decimal)valoraciones.Average(v => v.valNumEst) 
                    : 0m;

                // Separar mi valoración de las demás
                var miValoracion = conductorId.HasValue
                    ? valoraciones.FirstOrDefault(v => v.esMia)
                    : null;

                var otrasValoraciones = valoraciones
                    .Where(v => !v.esMia)
                    .Select(v => new
                    {
                        v.conductorNombre,
                        v.valNumEst,
                        v.valFav,
                        v.valComentario
                    })
                    .ToList();

                return Json(new
                {
                    valoraciones = otrasValoraciones,
                    promedio = promedio,
                    miValoracion = miValoracion != null ? new
                    {
                        valNumEst = miValoracion.valNumEst,
                        valFav = miValoracion.valFav,
                        valComentario = miValoracion.valComentario
                    } : null
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Endpoint para guardar/actualizar valoración
        [HttpPost]
        public async Task<IActionResult> GuardarValoracion([FromBody] GuardarValoracionRequest model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Json(new { success = false, error = "Usuario no autenticado" });
                }

                var conductorId = int.Parse(userId);

                // Buscar si ya existe una valoración
                var valoracionExistente = await _context.Valoraciones
                    .FirstOrDefaultAsync(v => v.PlyID == model.PlyID && v.ConNU == conductorId);

                if (valoracionExistente != null)
                {
                    // Actualizar
                    valoracionExistente.ValNumEst = model.ValNumEst;
                    valoracionExistente.ValFav = model.ValFav;
                    valoracionExistente.ValComentario = model.ValComentario;
                    // Si se quita el favorito, limpiar el apodo
                    if (!model.ValFav)
                    {
                        valoracionExistente.ValApodo = null;
                    }
                    _context.Entry(valoracionExistente).State = EntityState.Modified;
                }
                else
                {
                    // Crear nueva
                    var nuevaValoracion = new Valoracion
                    {
                        PlyID = model.PlyID,
                        ConNU = conductorId,
                        ValNumEst = model.ValNumEst,
                        ValFav = model.ValFav,
                        ValComentario = model.ValComentario
                    };
                    _context.Valoraciones.Add(nuevaValoracion);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Valoración guardada correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Endpoint para eliminar valoración
        [HttpDelete]
        public async Task<IActionResult> EliminarValoracion(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Json(new { success = false, error = "Usuario no autenticado" });
                }

                var conductorId = int.Parse(userId);

                var valoracion = await _context.Valoraciones
                    .FirstOrDefaultAsync(v => v.PlyID == id && v.ConNU == conductorId);

                if (valoracion == null)
                {
                    return Json(new { success = false, error = "Valoración no encontrada" });
                }

                _context.Valoraciones.Remove(valoracion);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Valoración eliminada correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        public class GuardarValoracionRequest
        {
            public int PlyID { get; set; }
            public int ValNumEst { get; set; }
            public bool ValFav { get; set; }
            public string? ValComentario { get; set; }
        }

        private async Task<decimal> CalcularPromedioTarifaHora(int plyID)
        {
            try
            {
                // Buscar servicios de tipo "Estacionamiento" con duración de 60 minutos (1 hora)
                var serviciosHora = await _context.ServiciosProveidos
                    .Include(sp => sp.Servicio)
                    .Where(sp => sp.PlyID == plyID &&
                                sp.SerProvHab &&
                                sp.Servicio.SerTipo == "Estacionamiento" &&
                                sp.Servicio.SerDuracionMinutos == 60)
                    .Select(sp => sp.SerID)
                    .Distinct()
                    .ToListAsync();

                if (!serviciosHora.Any())
                    return 0;

                // Obtener todas las tarifas vigentes por hora para esta playa
                var ahora = DateTime.UtcNow;
                var tarifasHora = await _context.TarifasServicio
                    .Where(t => t.PlyID == plyID &&
                               serviciosHora.Contains(t.SerID) &&
                               t.TasFecIni <= ahora &&
                               (t.TasFecFin == null || t.TasFecFin >= ahora))
                    .Select(t => t.TasMonto)
                    .ToListAsync();

                if (!tarifasHora.Any())
                    return 0;

                return Math.Round(tarifasHora.Average(), 2);
            }
            catch
            {
                return 0;
            }
        }
    }
}
