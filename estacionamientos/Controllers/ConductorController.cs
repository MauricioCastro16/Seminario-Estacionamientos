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
                    // Obtener todas las ocupaciones de este vehículo con información relacionada
                    var ocupaciones = await _context.Ocupaciones
                        .Include(o => o.Plaza)
                            .ThenInclude(p => p.Playa)
                        .Include(o => o.Pago)
                            .ThenInclude(p => p.MetodoPago)
                        .Where(o => o.VehPtnt == vehiculo.VehPtnt)
                        .OrderByDescending(o => o.OcufFyhIni)
                        .ToListAsync();

                    var historial = ocupaciones.Select(o => new
                    {
                        PlayaNombre = o.Plaza?.Playa?.PlyNom ?? "Desconocida",
                        PlayaDireccion = o.Plaza?.Playa?.PlyDir ?? "",
                        FechaHoraIngreso = o.OcufFyhIni,
                        FechaHoraEgreso = o.OcufFyhFin,
                        Duracion = o.OcufFyhFin.HasValue 
                            ? (o.OcufFyhFin.Value - o.OcufFyhIni).TotalHours 
                            : (double?)null,
                        MontoPagado = o.Pago?.PagMonto ?? 0,
                        MetodoPago = o.Pago?.MetodoPago?.MepNom ?? "Sin pago",
                        PlazaNumero = o.PlzNum,
                        DejoLlaves = o.OcuLlavDej
                    }).ToList<object>();

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

                var entidad = new UbicacionFavorita
                {
                    ConNU = conductorId,
                    UbfApodo = model.Apodo,
                    UbfProv = model.Provincia,
                    UbfCiu = model.Ciudad,
                    UbfDir = model.Direccion,
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
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
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
