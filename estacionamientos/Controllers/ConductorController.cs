using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using System.Security.Claims;

namespace estacionamientos.Controllers
{
    public class ConductorController : Controller
    {
        private readonly AppDbContext _context;
        public ConductorController(AppDbContext context) => _context = context;

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

                // Verificar que la patente no esté vacía
                if (string.IsNullOrWhiteSpace(patente))
                {
                    return Json(new { success = false, error = "La patente es requerida" });
                }

                // Verificar que la marca no esté vacía
                if (string.IsNullOrWhiteSpace(marca))
                {
                    return Json(new { success = false, error = "La marca es requerida" });
                }

                // Verificar que la clasificación existe
                var clasificacion = await _context.ClasificacionesVehiculo
                    .FirstOrDefaultAsync(c => c.ClasVehID == clasificacionId);

                if (clasificacion == null)
                {
                    return Json(new { success = false, error = "Clasificación de vehículo no válida" });
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
                var playas = await _context.Playas
                    .Include(p => p.Horarios)
                        .ThenInclude(h => h.ClasificacionDias)
                    .Include(p => p.Plazas)
                    .Where(p => p.PlyLat.HasValue && p.PlyLon.HasValue)
                    .ToListAsync(); // Removido AsNoTracking para asegurar que las relaciones se carguen

                // Calcular promedio de tarifas por hora para cada playa
                var playasCercanas = new List<object>();
                foreach (var p in playas)
                {
                    // Forzar carga explícita de horarios si no están cargados
                    if (p.Horarios == null)
                    {
                        await _context.Entry(p)
                            .Collection(pl => pl.Horarios)
                            .Query()
                            .Include(h => h.ClasificacionDias)
                            .LoadAsync();
                    }
                    
                    var valorPromedioHora = await CalcularPromedioTarifaHora(p.PlyID);
                    var estaAbierto = EstaAbierto(p);
                    playasCercanas.Add(new
                    {
                        p.PlyID,
                        plyNom = p.PlyNom,
                        plyDir = p.PlyDir,
                        plyTipoPiso = p.PlyTipoPiso,
                        plyValProm = valorPromedioHora,
                        plyLat = p.PlyLat,
                        plyLon = p.PlyLon,
                        distancia = CalcularDistancia(lat, lon, p.PlyLat!.Value, p.PlyLon!.Value),
                        disponibilidad = CalcularDisponibilidad(p),
                        estaAbierto = estaAbierto
                    });
                }

                return Json(playasCercanas);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Endpoint temporal para debug de horarios
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
                    return Json(new { error = "Playa no encontrada" });

                var ahora = DateTime.Now;
                var tipoDia = (ahora.DayOfWeek >= DayOfWeek.Monday && ahora.DayOfWeek <= DayOfWeek.Friday) 
                    ? "Hábil" 
                    : "Fin de semana";

                return Json(new
                {
                    playaID = playa.PlyID,
                    playaNombre = playa.PlyNom,
                    horaActual = ahora.ToString("HH:mm:ss"),
                    diaActual = ahora.DayOfWeek.ToString(),
                    tipoDiaBuscado = tipoDia,
                    totalHorarios = playa.Horarios?.Count ?? 0,
                    horarios = playa.Horarios?.Select(h => new
                    {
                        tipoDia = h.ClasificacionDias?.ClaDiasTipo ?? "NULL",
                        horaInicio = h.HorFyhIni.TimeOfDay.ToString(@"hh\:mm"),
                        horaFin = h.HorFyhFin?.TimeOfDay.ToString(@"hh\:mm") ?? "NULL",
                        coincide = h.ClasificacionDias?.ClaDiasTipo?.Equals(tipoDia, StringComparison.OrdinalIgnoreCase) == true
                    }).ToList(),
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
                var playa = await _context.Playas
                    .Include(p => p.Plazas)
                    .Include(p => p.Horarios)
                        .ThenInclude(h => h.ClasificacionDias)
                    .FirstOrDefaultAsync(p => p.PlyID == id);
                    
                // Asegurar que los horarios estén cargados
                if (playa != null && playa.Horarios == null)
                {
                    await _context.Entry(playa)
                        .Collection(p => p.Horarios)
                        .Query()
                        .Include(h => h.ClasificacionDias)
                        .LoadAsync();
                }

                if (playa == null)
                    return Json(new { error = "Playa no encontrada" });

                // Calcular promedio de tarifas por hora
                var valorPromedioHora = await CalcularPromedioTarifaHora(playa.PlyID);

                // Preparar información de horarios - solo "Lunes a Viernes" y "Sábado Domingo"
                var horariosLunesViernes = new List<object>();
                var horariosFinSemana = new List<object>();
                
                if (playa.Horarios != null)
                {
                    foreach (var h in playa.Horarios)
                    {
                        var tipoDia = h.ClasificacionDias?.ClaDiasTipo ?? "";
                        
                        // Convertir a hora local antes de extraer TimeOfDay
                        DateTime inicioLocal = h.HorFyhIni.Kind == DateTimeKind.Utc 
                            ? h.HorFyhIni.ToLocalTime() 
                            : h.HorFyhIni;
                        DateTime? finLocal = h.HorFyhFin.HasValue
                            ? (h.HorFyhFin.Value.Kind == DateTimeKind.Utc 
                                ? h.HorFyhFin.Value.ToLocalTime() 
                                : h.HorFyhFin.Value)
                            : null;
                        
                        var horarioInfo = new
                        {
                            horaInicio = inicioLocal.TimeOfDay.ToString(@"hh\:mm"),
                            horaFin = finLocal?.TimeOfDay.ToString(@"hh\:mm") ?? "Sin fin"
                        };
                        
                        if (tipoDia.Equals("Hábil", StringComparison.OrdinalIgnoreCase))
                        {
                            horariosLunesViernes.Add(horarioInfo);
                        }
                        else if (tipoDia.Equals("Fin de semana", StringComparison.OrdinalIgnoreCase))
                        {
                            horariosFinSemana.Add(horarioInfo);
                        }
                    }
                }

                // Calcular estado abierto/cerrado
                var estaAbierto = EstaAbierto(playa);

                return Json(new
                {
                    plyID = playa.PlyID,
                    plyNom = playa.PlyNom,
                    plyDir = playa.PlyDir,
                    plyTipoPiso = playa.PlyTipoPiso,
                    plyValProm = valorPromedioHora,
                    plyLat = playa.PlyLat,
                    plyLon = playa.PlyLon,
                    disponibilidad = CalcularDisponibilidad(playa),
                    estaAbierto = estaAbierto,
                    horariosLunesViernes = horariosLunesViernes,
                    horariosFinSemana = horariosFinSemana
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
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

        private static bool EstaAbierto(PlayaEstacionamiento playa)
        {
            try
            {
                // Verificar que existan horarios
                if (playa.Horarios == null || !playa.Horarios.Any())
                {
                    return false;
                }

                // Hora actual del sistema (local)
                var ahora = DateTime.Now;
                var ahoraTimeOfDay = ahora.TimeOfDay;
                var diaActual = ahora.DayOfWeek;
                
                // Determinar tipo de día según la base de datos
                // "Hábil" = Lunes a Viernes (1-5)
                // "Fin de semana" = Sábado y Domingo (0, 6)
                string tipoDiaBuscado = (diaActual >= DayOfWeek.Monday && diaActual <= DayOfWeek.Friday) 
                    ? "Hábil" 
                    : "Fin de semana";
                
                // Buscar horarios del día actual - asegurarse de que ClasificacionDias esté cargado
                var horariosRelevantes = playa.Horarios
                    .Where(h => h.ClasificacionDias != null && 
                               !string.IsNullOrWhiteSpace(h.ClasificacionDias.ClaDiasTipo) &&
                               h.ClasificacionDias.ClaDiasTipo.Equals(tipoDiaBuscado, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!horariosRelevantes.Any())
                {
                    return false;
                }

                // Verificar si la hora actual está dentro de algún rango horario
                foreach (var horario in horariosRelevantes)
                {
                    // Los horarios están guardados con fecha base 2000-01-01 en UTC
                    // Pero pueden tener timezone offset. Necesitamos extraer solo la hora local
                    // Convertir a hora local si está en UTC
                    DateTime horarioInicioLocal = horario.HorFyhIni.Kind == DateTimeKind.Utc 
                        ? horario.HorFyhIni.ToLocalTime() 
                        : horario.HorFyhIni;
                    
                    DateTime? horarioFinLocal = horario.HorFyhFin.HasValue
                        ? (horario.HorFyhFin.Value.Kind == DateTimeKind.Utc 
                            ? horario.HorFyhFin.Value.ToLocalTime() 
                            : horario.HorFyhFin.Value)
                        : null;
                    
                    // Extraer solo la parte de hora (TimeOfDay)
                    var horaInicio = horarioInicioLocal.TimeOfDay;
                    var horaFin = horarioFinLocal?.TimeOfDay;
                    
                    // Si no hay hora fin, asumir que cierra a las 23:59:59
                    if (!horaFin.HasValue)
                    {
                        horaFin = new TimeSpan(23, 59, 59);
                    }
                    
                    // Comparar hora actual con rango de horario
                    // Horario normal: inicio <= ahora <= fin
                    bool estaDentro = ahoraTimeOfDay >= horaInicio && ahoraTimeOfDay <= horaFin.Value;
                    
                    // Si el horario cruza medianoche (inicio > fin), usar lógica especial
                    if (horaInicio > horaFin.Value)
                    {
                        // Horario que cruza medianoche (ej: 22:00 - 02:00)
                        // Está abierto si: ahora >= inicio O ahora <= fin
                        estaDentro = ahoraTimeOfDay >= horaInicio || ahoraTimeOfDay <= horaFin.Value;
                    }
                    
                    if (estaDentro)
                    {
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
