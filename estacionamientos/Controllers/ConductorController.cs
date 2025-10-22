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

                ViewBag.VehiculoFavorito = vehiculoFavorito;
                ViewBag.Conduces = conduces;

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
                    .AsNoTracking()
                    .ToListAsync();

                // Para debug: show all playas without distance filter
                var playasCercanas = playas
                    .Select(p => new
                    {
                        p.PlyID,
                        p.PlyNom,
                        p.PlyDir,
                        p.PlyTipoPiso,
                        p.PlyValProm,
                        p.PlyLat,
                        p.PlyLon,
                        Distancia = CalcularDistancia(lat, lon, p.PlyLat!.Value, p.PlyLon!.Value),
                        Disponibilidad = CalcularDisponibilidad(p),
                        EstaAbierto = EstaAbierto(p)
                    })
                    .ToList();

                return Json(playasCercanas);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
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
                    .FirstOrDefaultAsync(p => p.PlyID == id);

                if (playa == null)
                    return Json(new { error = "Playa no encontrada" });

                return Json(new
                {
                    playa.PlyID,
                    playa.PlyNom,
                    playa.PlyDir,
                    playa.PlyTipoPiso,
                    playa.PlyValProm,
                    playa.PlyLat,
                    playa.PlyLon,
                    Disponibilidad = CalcularDisponibilidad(playa),
                    EstaAbierto = EstaAbierto(playa)
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
            var ahora = DateTime.Now.TimeOfDay;
            var diaActual = DateTime.Now.DayOfWeek;
            
            // Mapear DayOfWeek a números (0 = Domingo, 1 = Lunes, etc.)
            var diaNumero = diaActual == DayOfWeek.Sunday ? 0 : (int)diaActual;
            
            var horarioHoy = playa.Horarios
                .FirstOrDefault(h => h.ClasificacionDias?.ClaDiasTipo == 
                    (diaNumero >= 1 && diaNumero <= 5 ? "Entre semana" : "Fin de semana"));

            if (horarioHoy == null) return false;

            // Convertir DateTime a TimeOfDay para comparar
            var horaInicio = horarioHoy.HorFyhIni.TimeOfDay;
            var horaFin = horarioHoy.HorFyhFin?.TimeOfDay ?? TimeSpan.FromDays(1);

            return ahora >= horaInicio && ahora <= horaFin;
        }
    }
}
