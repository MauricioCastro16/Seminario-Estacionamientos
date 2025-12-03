using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class PlazaEstacionamientoController : BaseController
    {
        private readonly AppDbContext _ctx;
        public PlazaEstacionamientoController(AppDbContext ctx) => _ctx = ctx;

        // ===========================
        // Helpers (combos / viewbags)
        // ===========================
        private async Task LoadClasificaciones(List<int>? selected = null)

        {
            var clasifs = await _ctx.ClasificacionesVehiculo
                .OrderBy(c => c.ClasVehTipo)
                .ToListAsync();

            ViewBag.Clasificaciones = new MultiSelectList(clasifs, "ClasVehID", "ClasVehTipo", selected);

        }

        private async Task LoadPlayas(int? selected = null)
        {
            var playas = await _ctx.Playas.AsNoTracking()
                .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new { p.PlyID, Nombre = p.PlyCiu + " - " + p.PlyDir })
                .ToListAsync();

            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", selected);
        }

        public async Task<IActionResult> Index()
        {
            var q = _ctx.Plazas.Include(p => p.Playa).AsNoTracking();
            return View(await q.ToListAsync());
        }

        public async Task<IActionResult> Details(int plyID, int plzNum)
        {
            var item = await _ctx.Plazas.Include(p => p.Playa).AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlyID == plyID && p.PlzNum == plzNum);
            return item is null ? NotFound() : View(item);
        }

        [HttpGet("/PlazaEstacionamiento/GetPlazasMapa")]
        public async Task<JsonResult> GetPlazasMapa(int plyID)
        {
            var plazas = await _ctx.Plazas
                .Where(p => p.PlyID == plyID)
                .OrderBy(p => p.Piso).ThenBy(p => p.PlzNum)
                .Select(p => new
                {
                    plzNum = p.PlzNum,
                    piso = p.Piso,
                    hab = p.PlzHab,
                    nombre = p.PlzNombre,
                    ocupada = _ctx.Ocupaciones.Any(o => o.PlyID == p.PlyID && o.PlzNum == p.PlzNum && o.OcufFyhFin == null)
                })
                .ToListAsync();

            return Json(plazas);
        }





        [HttpGet("Playas/{plyID}/[controller]")]
        public async Task<IActionResult> ConfigurarPlazas(int plyID)
        {
            var playa = await _ctx.Playas
                .Include(p => p.Plazas)
                    .ThenInclude(pl => pl.Clasificaciones)
                        .ThenInclude(pc => pc.Clasificacion)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlyID == plyID);


            if (playa == null) return NotFound();

            ViewBag.PlyID = playa.PlyID;
            ViewBag.PlyNom = playa.PlyNom;
            ViewBag.DefaultCantidad = 1;
            ViewBag.DefaultPiso = (int?)null; // si querés, usalo en la vista como valor por defecto

            await LoadClasificaciones();

            // Verificar abonos activos para cada plaza
            var fechaActual = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            var fechaActualDate = fechaActual.Date;

            // Cargar abonos activos en memoria para evitar problemas de traducción LINQ
            var abonosActivos = await _ctx.Abonos
                .AsNoTracking()
                .Where(a => a.PlyID == plyID
                         && a.EstadoPago != estacionamientos.Models.EstadoPago.Cancelado
                         && a.EstadoPago != estacionamientos.Models.EstadoPago.Finalizado
                         && fechaActualDate >= a.AboFyhIni.Date
                         && (a.AboFyhFin == null || a.AboFyhFin.Value.Date >= fechaActualDate))
                .ToListAsync();

            // Crear diccionario de abonos activos por plaza
            var dictAbonosActivos = new Dictionary<string, bool>();
            foreach (var plaza in playa.Plazas)
            {
                var key = $"{plaza.PlyID}_{plaza.PlzNum}";
                var tieneAbonoActivo = abonosActivos.Any(a => a.PlyID == plaza.PlyID && a.PlzNum == plaza.PlzNum);
                dictAbonosActivos[key] = tieneAbonoActivo;
            }

            ViewBag.AbonoActivo = dictAbonosActivos;

            SetBreadcrumb(
                new BreadcrumbItem { Title = "Playas", Url = Url.Action("Index", "PlayaEstacionamiento")! },
                new BreadcrumbItem { Title = $"Configurar Plazas ({playa.PlyNom})", Url = Url.Action("ConfigurarPlazas", "PlazaEstacionamiento", new {plyID = playa.PlyID})! }
            );
            return View(playa.Plazas.OrderBy(z => z.PlzNum));
        }

        [HttpPost("Playas/{plyID}/[controller]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfigurarPlazas(
            int plyID,
            int cantidad = 1,
            bool? plzTecho = null,
            decimal? plzAlt = null,
            List<int>? clasVehID = null,
            int? piso = null
        )

        {
            var playa = await _ctx.Playas
                .Include(p => p.Plazas)
                .FirstOrDefaultAsync(p => p.PlyID == plyID);

            if (playa == null) return NotFound();

            // si no hay techo, no hay altura
            if (plzTecho == false)
                plzAlt = null;

            // Validar altura según techo
            bool alturaValida =
                (plzTecho == true && plzAlt.HasValue && plzAlt.Value >= 2m) ||
                (plzTecho == false && plzAlt == null);

            // (Opcional) validar rango de piso
            // if (piso.HasValue && (piso.Value < -5 || piso.Value > 50))
            //     ModelState.AddModelError("piso", "El piso debe estar entre -5 y 50.");

            if (cantidad < 1 || plzTecho == null || !alturaValida /*|| !ModelState.IsValid*/)
            {
                ViewBag.PlyID = playa.PlyID;
                ViewBag.PlyNom = playa.PlyNom;
                ViewBag.DefaultCantidad = 1;
                ViewBag.DefaultPiso = piso;

                await _ctx.Entry(playa)
                    .Collection(p => p.Plazas)
                    .Query()
                    .Include(pl => pl.Clasificaciones)
                        .ThenInclude(pc => pc.Clasificacion)
                    .LoadAsync();


                var plazas = playa.Plazas.OrderBy(z => z.PlzNum).ToList();

                if (plzTecho == true && (!plzAlt.HasValue || plzAlt.Value < 2m))
                {
                    ModelState.AddModelError("plzAlt", "La altura mínima permitida es 2m.");
                }
                else if (plzTecho == null)
                {
                    ModelState.AddModelError("plzTecho", "Debe indicar si la plaza tiene techo.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Todos los campos son obligatorios.");
                }

                await LoadClasificaciones(clasVehID);
                return View(plazas);
            }

            if (clasVehID == null || !clasVehID.Any())
            {
                ViewBag.PlyID = playa.PlyID;
                ViewBag.PlyNom = playa.PlyNom;
                ViewBag.DefaultCantidad = 1;
                ViewBag.DefaultPiso = piso;

                await LoadClasificaciones();
                ModelState.AddModelError("clasVehID", "Debe seleccionar al menos una clasificación.");

                await _ctx.Entry(playa)
                    .Collection(p => p.Plazas)
                    .Query()
                    .Include(pl => pl.Clasificaciones)
                        .ThenInclude(pc => pc.Clasificacion)
                    .LoadAsync();

                return View(playa.Plazas.OrderBy(z => z.PlzNum));
            }


            // calcular desde qué número crear
            int nextNum = playa.Plazas.Any() ? playa.Plazas.Max(pl => pl.PlzNum) + 1 : 1;

            for (int i = 0; i < cantidad; i++)
            {
            var plaza = new PlazaEstacionamiento
            {
                PlyID = plyID,
                PlzNum = nextNum + i,
                PlzTecho = plzTecho.Value,
                PlzAlt = plzTecho.Value ? plzAlt : null,
                PlzHab = true,
                PlzNombre = null,
                Piso = piso
            };


            // 🔹 agregar las clasificaciones seleccionadas
            foreach (var clasId in clasVehID)
            {
                plaza.Clasificaciones.Add(new PlazaClasificacion
                {
                    PlyID = plyID,
                    PlzNum = plaza.PlzNum,
                    ClasVehID = clasId
                });
            }

            _ctx.Plazas.Add(plaza);

            }

            await _ctx.SaveChangesAsync();

            // Verificar y actualizar estado de la playa
            await VerificarYActualizarEstadoPlaya(plyID);

            return RedirectToAction(nameof(ConfigurarPlazas), new { plyID = plyID });
        }

        /// <summary>
        /// Verifica si una playa tiene al menos 1 método de pago habilitado y 1 plaza,
        /// y actualiza el estado a Vigente si cumple los requisitos.
        /// </summary>
        private async Task VerificarYActualizarEstadoPlaya(int plyID)
        {
            var playa = await _ctx.Playas.FindAsync(plyID);
            if (playa == null) return;

            // Verificar si tiene al menos 1 método de pago habilitado
            var tieneMetodoPago = await _ctx.AceptaMetodosPago
                .AnyAsync(a => a.PlyID == plyID && a.AmpHab);

            // Verificar si tiene al menos 1 plaza
            var tienePlaza = await _ctx.Plazas
                .AnyAsync(p => p.PlyID == plyID);

            // Si cumple ambos requisitos, actualizar a Vigente
            if (tieneMetodoPago && tienePlaza && playa.PlyEstado == estacionamientos.Models.EstadoPlaya.Oculto)
            {
                playa.PlyEstado = estacionamientos.Models.EstadoPlaya.Vigente;
                await _ctx.SaveChangesAsync();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInline(
            int plyID,
            int plzNum,
            bool plzTecho,
            decimal? plzAlt,
            string? plzNombre,
            int? piso,
            int[] clasVehID
        )
        {
            var plaza = await _ctx.Plazas
                .Include(p => p.Clasificaciones) // 🔹 importante para poder limpiar y reasignar
                .FirstOrDefaultAsync(p => p.PlyID == plyID && p.PlzNum == plzNum);

            if (plaza is null)
            {
                TempData["Error"] = $"No se encontró la plaza {plzNum}.";
                return RedirectToAction(nameof(ConfigurarPlazas), new { plyID });
            }

            // 🚨 Validación: no permitir editar una plaza con abono activo
            var fechaActual = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            var fechaActualDate = fechaActual.Date;
            
            // Un abono solo está activo si la fecha actual es >= fecha de inicio (ya comenzó)
            var tieneAbonoActivo = await _ctx.Abonos
                .AnyAsync(a => a.PlyID == plyID 
                            && a.PlzNum == plzNum
                            && a.EstadoPago != estacionamientos.Models.EstadoPago.Cancelado
                            && a.EstadoPago != estacionamientos.Models.EstadoPago.Finalizado
                            && fechaActualDate >= a.AboFyhIni.Date
                            && (a.AboFyhFin == null || a.AboFyhFin.Value.Date >= fechaActualDate));

            if (tieneAbonoActivo)
            {
                TempData["Error"] = $"No se puede editar la plaza {plzNum} porque tiene un abono activo.";
                TempData["MensajeCss"] = "danger";
                return RedirectToAction(nameof(ConfigurarPlazas), new { plyID });
            }

            if (clasVehID == null || !clasVehID.Any())
            {
                TempData["Error"] = "Debe seleccionar al menos una clasificación.";
                return RedirectToAction(nameof(ConfigurarPlazas), new { plyID });
            }


            // Validaciones
            if (!plzTecho) plzAlt = null;
            if (plzTecho && (!plzAlt.HasValue || plzAlt.Value < 2m))
            {
                TempData["Error"] = "La altura mínima permitida es 2m";
                return RedirectToAction(nameof(ConfigurarPlazas), new { plyID });
            }

            // Actualizar datos básicos
            plaza.PlzTecho = plzTecho;
            plaza.PlzAlt = plzAlt;
            plaza.PlzNombre = string.IsNullOrWhiteSpace(plzNombre) ? null : plzNombre.Trim();
            plaza.Piso = piso;

            // 🔹 Actualizar clasificaciones
            plaza.Clasificaciones.Clear();
            if (clasVehID != null)
            {
                foreach (var id in clasVehID)
                {
                    plaza.Clasificaciones.Add(new PlazaClasificacion
                    {
                        PlyID = plyID,
                        PlzNum = plzNum,
                        ClasVehID = id
                    });
                }
            }

            await _ctx.SaveChangesAsync();

            TempData["Ok"] = $"Plaza {plzNum} actualizada.";
            return RedirectToAction(nameof(ConfigurarPlazas), new { plyID });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInline(int plyID, int plzNum)
        {
            var plaza = await _ctx.Plazas.FindAsync(plyID, plzNum);
            if (plaza is null)
            {
                TempData["Error"] = $"No se encontró la plaza {plzNum}.";
                return RedirectToAction(nameof(ConfigurarPlazas), new { plyID });
            }

            // 🚨 Validación: no permitir eliminar una plaza con abono activo
            var fechaActual = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            var fechaActualDate = fechaActual.Date;
            
            // Un abono solo está activo si la fecha actual es >= fecha de inicio (ya comenzó)
            var tieneAbonoActivo = await _ctx.Abonos
                .AnyAsync(a => a.PlyID == plyID 
                            && a.PlzNum == plzNum
                            && a.EstadoPago != estacionamientos.Models.EstadoPago.Cancelado
                            && a.EstadoPago != estacionamientos.Models.EstadoPago.Finalizado
                            && fechaActualDate >= a.AboFyhIni.Date
                            && (a.AboFyhFin == null || a.AboFyhFin.Value.Date >= fechaActualDate));

            if (tieneAbonoActivo)
            {
                TempData["Error"] = $"No se puede eliminar la plaza {plzNum} porque tiene un abono activo.";
                TempData["MensajeCss"] = "danger";
                return RedirectToAction(nameof(ConfigurarPlazas), new { plyID });
            }

            _ctx.Plazas.Remove(plaza);
            await _ctx.SaveChangesAsync();

            TempData["Ok"] = $"Plaza {plzNum} eliminada.";
            return RedirectToAction(nameof(ConfigurarPlazas), new { plyID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleHabilitadaDueño(int plyID, int plzNum)
        {
            var plaza = await _ctx.Plazas.FindAsync(plyID, plzNum);
            if (plaza is null)
            {
                TempData["Error"] = $"No se encontró la plaza {plzNum}.";
                TempData["MensajeCss"] = "danger";
                return RedirectToAction(nameof(ConfigurarPlazas), new { plyID });
            }

            // 🚨 Validación: no permitir inhabilitar una plaza con abono activo
            var fechaActual = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            var fechaActualDate = fechaActual.Date;
            
            // Un abono solo está activo si la fecha actual es >= fecha de inicio (ya comenzó)
            var tieneAbonoActivo = await _ctx.Abonos
                .AnyAsync(a => a.PlyID == plyID 
                            && a.PlzNum == plzNum
                            && a.EstadoPago != estacionamientos.Models.EstadoPago.Cancelado
                            && a.EstadoPago != estacionamientos.Models.EstadoPago.Finalizado
                            && fechaActualDate >= a.AboFyhIni.Date
                            && (a.AboFyhFin == null || a.AboFyhFin.Value.Date >= fechaActualDate));

            if (tieneAbonoActivo && plaza.PlzHab)
            {
                TempData["Error"] = $"No se puede inhabilitar la plaza {plzNum} porque tiene un abono activo.";
                TempData["MensajeCss"] = "danger";
                return RedirectToAction(nameof(ConfigurarPlazas), new { plyID });
            }

            plaza.PlzHab = !plaza.PlzHab;
            await _ctx.SaveChangesAsync();

            TempData["Ok"] = $"Plaza {plzNum} {(plaza.PlzHab ? "habilitada" : "deshabilitada")}.";
            TempData["MensajeCss"] = plaza.PlzHab ? "success" : "danger";

            return RedirectToAction(nameof(ConfigurarPlazas), new { plyID });
        }

    }
}
