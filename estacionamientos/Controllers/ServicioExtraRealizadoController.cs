using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace estacionamientos.Controllers
{
    [Authorize(Roles = "Playero")]
    public class ServicioExtraRealizadoController : Controller
    {
        private readonly AppDbContext _ctx;
        public ServicioExtraRealizadoController(AppDbContext ctx) => _ctx = ctx;

        // 🔹 Cargar clasificaciones de vehículo
        private async Task LoadClasificacionesVehiculo(int? selected = null)
        {
            var clasificaciones = await _ctx.ClasificacionesVehiculo
                .OrderBy(c => c.ClasVehTipo)
                .AsNoTracking()
                .Select(c => new
                {
                    c.ClasVehID,
                    c.ClasVehTipo,
                    c.ClasVehDesc
                })
                .ToListAsync();

            ViewBag.ClasVehID = new SelectList(clasificaciones, "ClasVehID", "ClasVehTipo", selected);
            ViewBag.ClasificacionesDetalle = clasificaciones;
        }

        // 🔹 Cargar servicios extra habilitados para una playa
        private async Task LoadServiciosHabilitados(int plyID, int? serSel = null)
        {
            var servicios = await _ctx.ServiciosProveidos
                .Include(sp => sp.Servicio)
                .Where(sp => sp.PlyID == plyID &&
                             sp.SerProvHab &&
                             sp.Servicio.SerTipo == "ServicioExtra")
                .AsNoTracking()
                .OrderBy(sp => sp.Servicio.SerNom)
                .Select(sp => new
                {
                    sp.SerID,
                    sp.Servicio.SerNom,
                    sp.Servicio.SerDesc
                })
                .ToListAsync();

            ViewBag.SerID = new SelectList(servicios, "SerID", "SerNom", serSel);
            ViewBag.ServiciosDetalle = servicios;
        }

        // 🔹 Cargar lista de vehículos por patente
        private async Task LoadVehiculos(string? selected = null)
        {
            var vehs = await _ctx.Vehiculos.AsNoTracking()
                .OrderBy(v => v.VehPtnt)
                .Select(v => v.VehPtnt)
                .ToListAsync();

            ViewBag.VehPtnt = new SelectList(vehs, selected);
            ViewBag.VehiculosList = vehs;
        }

        // 🔹 Validar que exista tarifa vigente para la combinación playa + servicio + tipo vehículo
        private async Task<bool> ValidarTarifaVigente(int plyID, int serID, int clasVehID)
        {
            var ahora = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

            return await _ctx.TarifasServicio
                .Include(t => t.ServicioProveido)
                .AnyAsync(t =>
                    t.PlyID == plyID &&
                    t.SerID == serID &&
                    t.ClasVehID == clasVehID &&
                    (t.TasFecFin == null || t.TasFecFin > ahora) &&
                    t.TasFecIni <= ahora &&
                    t.ServicioProveido.SerProvHab);
        }

        // 🔹 Verificación asíncrona desde AJAX
        [HttpGet]
        public async Task<IActionResult> VerificarTarifaVigente(int plyID, int serID, int clasVehID)
        {
            var valido = await ValidarTarifaVigente(plyID, serID, clasVehID);
            return Json(valido);
        }

        // 🔹 GET: Create
        public async Task<IActionResult> Create(int? plyID = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var plaNU))
                return BadRequest("ID de usuario inválido");

            // Turno activo del playero
            var turno = await _ctx.Turnos
                .FirstOrDefaultAsync(t => t.PlaNU == plaNU && t.TurFyhFin == null);

            if (turno == null)
            {
                TempData["Error"] = "Debe tener un turno activo para registrar servicios extra.";
                return RedirectToAction("Index", "Ocupacion");
            }

            // Cargar servicios extra habilitados para la playa del turno
            await LoadServiciosHabilitados(turno.PlyID);

            // Obtener nombre de la playa
            var playaNombre = await _ctx.Playas
                .Where(p => p.PlyID == turno.PlyID)
                .Select(p => p.PlyNom)
                .FirstOrDefaultAsync();

            ViewBag.SelectedPlyID = turno.PlyID;
            ViewBag.SelectedPlyNombre = playaNombre;

            await LoadVehiculos();
            await LoadClasificacionesVehiculo();

            return View(new ServicioExtraRealizado
            {
                ServExFyHIni = DateTime.Now,
                PlyID = turno.PlyID
            });
        }

        // 🔹 POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServicioExtraRealizado model, int ClasVehID)
        {
            if (!ModelState.IsValid)
            {
                await LoadServiciosHabilitados(model.PlyID);
                await LoadVehiculos(model.VehPtnt);
                await LoadClasificacionesVehiculo(ClasVehID);
                return View(model);
            }

            // Validar tarifa vigente
            var existeTarifa = await ValidarTarifaVigente(model.PlyID, model.SerID, ClasVehID);
            if (!existeTarifa)
            {
                ModelState.AddModelError("", "No existe una tarifa vigente para el servicio y tipo de vehículo seleccionados.");
                await LoadServiciosHabilitados(model.PlyID);
                await LoadVehiculos(model.VehPtnt);
                await LoadClasificacionesVehiculo(ClasVehID);
                return View(model);
            }

            // 🔹 Verificar que el servicio provisto exista
            var existeServicio = await _ctx.ServiciosProveidos
                .AnyAsync(sp => sp.PlyID == model.PlyID && sp.SerID == model.SerID && sp.SerProvHab);

            if (!existeServicio)
            {
                ModelState.AddModelError("", "La playa no ofrece este servicio.");
                await LoadServiciosHabilitados(model.PlyID);
                await LoadVehiculos(model.VehPtnt);
                await LoadClasificacionesVehiculo(ClasVehID);
                return View(model);
            }

            // 🔹 Verificar que el vehículo exista; si no, crear un registro mínimo con marca por defecto
            var existeVeh = await _ctx.Vehiculos.AnyAsync(v => v.VehPtnt == model.VehPtnt);
            if (!existeVeh)
            {
                var nuevoVeh = new Vehiculo
                {
                    VehPtnt = model.VehPtnt,
                    VehMarc = "No especificado",
                    ClasVehID = ClasVehID
                };

                try
                {
                    _ctx.Vehiculos.Add(nuevoVeh);
                    await _ctx.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"No se pudo crear el vehículo automáticamente: {ex.Message}");
                    await LoadServiciosHabilitados(model.PlyID);
                    await LoadVehiculos(model.VehPtnt);
                    await LoadClasificacionesVehiculo(ClasVehID);
                    return View(model);
                }
            }

            // 🔹 Definir estado inicial y hora
            model.ServExFyHIni = DateTime.UtcNow;
            model.ServExEstado = "Pendiente";

            // ⚠️ Importante: NO asignar objetos de navegación, solo las FK
            model.ServicioProveido = null;
            model.Vehiculo = null;

            try
            {
                model.ServExFyHIni = DateTime.UtcNow;
                model.ServExEstado = string.IsNullOrEmpty(model.ServExEstado) ? "Pendiente" : model.ServExEstado;

                _ctx.Entry(model).State = EntityState.Added;
                await _ctx.SaveChangesAsync();

                TempData["Saved"] = true;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al guardar el registro: {ex.Message}");
                await LoadServiciosHabilitados(model.PlyID);
                await LoadVehiculos(model.VehPtnt);
                await LoadClasificacionesVehiculo(ClasVehID);
                return View(model);
            }

        }

        // 🔹 INDEX: lista los servicios extra del turno activo del playero
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var plaNU))
                return BadRequest("ID de usuario inválido");

            // Buscar turno activo del playero
            var turno = await _ctx.Turnos
                .Include(t => t.Playa)
                .FirstOrDefaultAsync(t => t.PlaNU == plaNU && t.TurFyhFin == null);

            if (turno == null)
            {
                TempData["Error"] = "Debe tener un turno activo para ver los servicios extra realizados.";
                return RedirectToAction("Index", "Ocupacion");
            }

            try
            {
                var lista = await _ctx.ServiciosExtrasRealizados
                    .Include(s => s.ServicioProveido).ThenInclude(sp => sp.Servicio)
                    .Include(s => s.Vehiculo)
                    .AsNoTracking()
                    .Where(s => s.PlyID == turno.PlyID)
                    .OrderByDescending(s => s.ServExFyHIni)
                    .ToListAsync();

                ViewBag.PlayaNombre = turno.Playa.PlyNom;
                ViewBag.TurnoInicio = turno.TurFyhIni;

                return View(lista);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Error en Index: {ex.Message}");
                TempData["Error"] = $"Error al cargar los servicios extra: {ex.Message}";
                return View(new List<ServicioExtraRealizado>());
            }
        }

    }
}