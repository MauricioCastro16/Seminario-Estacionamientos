using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using System.Security.Claims;

namespace estacionamientos.Controllers
{
    public class OcupacionController : Controller
    {
        private readonly AppDbContext _ctx;
        public OcupacionController(AppDbContext ctx) => _ctx = ctx;

        private async Task LoadSelects(int? plySel = null, int? plzSel = null, string? vehSel = null)
        {
            var playas = await _ctx.Playas.AsNoTracking()
                .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new { p.PlyID, Nombre = p.PlyCiu + " - " + p.PlyDir })
                .ToListAsync();

            List<object> plazas;
            if (plySel is null)
            {
                plazas = new List<object>();
            }
            else
            {
                plazas = await _ctx.Plazas
                    .Where(p => p.PlyID == plySel)
                    .OrderBy(p => p.PlzNum)
                    .Select(p => (object)new { p.PlzNum })
                    .ToListAsync();
            }

            var vehiculos = await _ctx.Vehiculos.AsNoTracking()
                .OrderBy(v => v.VehPtnt).Select(v => v.VehPtnt).ToListAsync();

            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", plySel);
            ViewBag.PlzNum = new SelectList(plazas, "PlzNum", "PlzNum", plzSel);
            ViewBag.VehPtnt = new SelectList(vehiculos, vehSel);
        }

        private async Task<bool> PlazaExiste(int plyID, int plzNum)
            => await _ctx.Plazas.AnyAsync(p => p.PlyID == plyID && p.PlzNum == plzNum);

        private async Task<bool> VehiculoExiste(string pat)
            => await _ctx.Vehiculos.AnyAsync(v => v.VehPtnt == pat);

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Playero"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var turno = await _ctx.Turnos
                    .Where(t => t.PlaNU.ToString() == userId && t.TurFyhFin == null)
                    .Include(t => t.Playa)
                    .FirstOrDefaultAsync();

                if (turno == null)
                {
                    return View("NoTurno");
                }

                var q = _ctx.Ocupaciones
                    .Include(o => o.Plaza).ThenInclude(p => p.Clasificacion)
                    .Include(o => o.Plaza).ThenInclude(p => p.Playa)
                    .Include(o => o.Vehiculo).ThenInclude(v => v.Clasificacion)
                    .Include(o => o.Pago)
                    .Where(o => o.PlyID == turno.PlyID)
                    .AsNoTracking();

                return View(await q.ToListAsync());
            }

            var qAll = _ctx.Ocupaciones
                .Include(o => o.Plaza).ThenInclude(p => p.Clasificacion)
                .Include(o => o.Plaza).ThenInclude(p => p.Playa)
                .Include(o => o.Vehiculo).ThenInclude(v => v.Clasificacion)
                .Include(o => o.Pago)
                .AsNoTracking();

            return View(await qAll.ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarIngreso(int plyID, int plzNum, string vehPtnt)
        {
            var ocup = new Ocupacion
            {
                PlyID = plyID,
                PlzNum = plzNum,
                VehPtnt = vehPtnt,
                OcufFyhIni = DateTime.UtcNow,
                OcufFyhFin = null
            };

            _ctx.Ocupaciones.Add(ocup);
            await _ctx.SaveChangesAsync();
            TempData["Success"] = $"Vehículo {vehPtnt} ingresó a la plaza {plzNum}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarEgreso(int plyID, int plzNum, string vehPtnt)
        {
            var ocup = await _ctx.Ocupaciones
                .FirstOrDefaultAsync(o => o.PlyID == plyID && o.PlzNum == plzNum && o.VehPtnt == vehPtnt && o.OcufFyhFin == null);

            if (ocup == null)
            {
                TempData["Error"] = "No se encontró una ocupación activa para este vehículo.";
                return RedirectToAction(nameof(Index));
            }

            await using var tx = await _ctx.Database.BeginTransactionAsync();

            ocup.OcufFyhFin = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();

            // si no quedan ocupaciones activas en esa plaza, marcarla como libre
            var sigueOcupada = await _ctx.Ocupaciones.AnyAsync(o =>
                o.PlyID == plyID && o.PlzNum == plzNum && o.OcufFyhFin == null);

            if (!sigueOcupada)
            {
                var plaza = await _ctx.Plazas.FindAsync(plyID, plzNum);
                if (plaza != null && plaza.PlzOcupada)
                {
                    plaza.PlzOcupada = false;
                    _ctx.Plazas.Update(plaza);
                    await _ctx.SaveChangesAsync();
                }
            }

            await tx.CommitAsync();

            TempData["Success"] = $"Vehículo {vehPtnt} egresó de la plaza {plzNum}.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int plyID, int plzNum, string vehPtnt, DateTime ocufFyhIni)
        {
            var item = await _ctx.Ocupaciones
                .Include(o => o.Plaza).ThenInclude(p => p.Playa)
                .Include(o => o.Vehiculo)
                .Include(o => o.Pago)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.PlyID == plyID && o.PlzNum == plzNum && o.VehPtnt == vehPtnt && o.OcufFyhIni == ocufFyhIni);

            return item is null ? NotFound() : View(item);
        }

        [HttpGet]
        public async Task<JsonResult> GetPlazasDisponibles(int plyID, int clasVehID)
        {
            var plazas = await _ctx.Plazas
                .Where(p => p.PlyID == plyID
                        && p.ClasVehID == clasVehID
                        && p.PlzHab == true
                        && !_ctx.Ocupaciones.Any(o => o.PlyID == p.PlyID
                                                    && o.PlzNum == p.PlzNum
                                                    && o.OcufFyhFin == null))
                .OrderBy(p => p.PlzNum)
                .Select(p => new {
                    plzNum = p.PlzNum,
                    label = $"Plaza {p.PlzNum}"
                })
                .ToListAsync();

            return Json(plazas);
        }

        public async Task<IActionResult> Create()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.IsInRole("Playero"))
            {
                var turno = await _ctx.Turnos
                    .Where(t => t.PlaNU.ToString() == userId && t.TurFyhFin == null)
                    .FirstOrDefaultAsync();

                if (turno == null)
                {
                    TempData["Error"] = "Debe tener un turno activo para registrar ingresos.";
                    return RedirectToAction(nameof(Index));
                }

                var playaNombre = await _ctx.Playas
                    .Where(p => p.PlyID == turno.PlyID)
                    .Select(p => p.PlyNom)
                    .FirstOrDefaultAsync();

                ViewBag.PlayaNombre = playaNombre;

                await LoadSelects(turno.PlyID);

                ViewBag.Clasificaciones = new SelectList(
                    await _ctx.ClasificacionesVehiculo
                        .OrderBy(c => c.ClasVehTipo)
                        .ToListAsync(),
                    "ClasVehID", "ClasVehTipo"
                );

                return View(new Ocupacion
                {
                    OcufFyhIni = DateTime.UtcNow,
                    PlyID = turno.PlyID
                });
            }

            await LoadSelects();
            ViewBag.Clasificaciones = new SelectList(
                await _ctx.ClasificacionesVehiculo.OrderBy(c => c.ClasVehTipo).ToListAsync(),
                "ClasVehID", "ClasVehTipo"
            );
            return View(new Ocupacion { OcufFyhIni = DateTime.UtcNow });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ocupacion model, int ClasVehID)
        {
            if (ClasVehID == 0)
            {
                TempData["Error"] = "Debe seleccionar una clasificación válida.";
                return RedirectToAction(nameof(Index));
            }

            var plazaValida = await _ctx.Plazas.AnyAsync(p =>
                p.PlyID == model.PlyID &&
                p.PlzNum == model.PlzNum &&
                p.ClasVehID == ClasVehID &&
                p.PlzHab == true);

            if (!plazaValida)
            {
                TempData["Error"] = "La plaza seleccionada no es válida para esta clasificación.";
                return RedirectToAction(nameof(Index));
            }

            var yaOcupada = await _ctx.Ocupaciones.AnyAsync(o =>
                o.PlyID == model.PlyID &&
                o.PlzNum == model.PlzNum &&
                o.OcufFyhFin == null);

            if (yaOcupada)
            {
                TempData["Error"] = "La plaza ya está ocupada.";
                return RedirectToAction(nameof(Index));
            }

            if (!await PlazaExiste(model.PlyID, model.PlzNum))
            {
                TempData["Error"] = "La plaza no existe en la playa seleccionada.";
                return RedirectToAction(nameof(Index));
            }

            // Alta automática de vehículo si no existe / actualización de clase
            var vehiculo = await _ctx.Vehiculos.FirstOrDefaultAsync(v => v.VehPtnt == model.VehPtnt);
            if (vehiculo == null)
            {
                vehiculo = new Vehiculo
                {
                    VehPtnt = model.VehPtnt,
                    ClasVehID = ClasVehID,
                    VehMarc = "Desconocida"
                };
                _ctx.Vehiculos.Add(vehiculo);
                await _ctx.SaveChangesAsync();
            }
            else if (vehiculo.ClasVehID == 0 && ClasVehID != 0)
            {
                vehiculo.ClasVehID = ClasVehID;
                _ctx.Update(vehiculo);
                await _ctx.SaveChangesAsync();
            }

            // Guardar ocupación + marcar plaza ocupada (transacción)
            await using var tx = await _ctx.Database.BeginTransactionAsync();

            model.OcufFyhIni = DateTime.UtcNow;
            model.OcufFyhFin = null;
            _ctx.Ocupaciones.Add(model);
            await _ctx.SaveChangesAsync();

            var plaza = await _ctx.Plazas.FindAsync(model.PlyID, model.PlzNum);
            if (plaza != null && !plaza.PlzOcupada)
            {
                plaza.PlzOcupada = true;
                _ctx.Plazas.Update(plaza);
                await _ctx.SaveChangesAsync();
            }

            await tx.CommitAsync();

            TempData["Success"] = $"Vehículo {model.VehPtnt} ingresó a la plaza {model.PlzNum}.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int plyID, int plzNum, string vehPtnt, DateTime ocufFyhIni)
        {
            var item = await _ctx.Ocupaciones.FindAsync(plyID, plzNum, vehPtnt, ocufFyhIni);
            if (item is null) return NotFound();

            await LoadSelects(item.PlyID, item.PlzNum, item.VehPtnt);
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int plzNum, string vehPtnt, DateTime ocufFyhIni, Ocupacion model)
        {
            if (plyID != model.PlyID || plzNum != model.PlzNum || vehPtnt != model.VehPtnt || ocufFyhIni != model.OcufFyhIni)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.PlzNum, model.VehPtnt);
                return View(model);
            }

            _ctx.Entry(model).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int plyID, int plzNum, string vehPtnt, DateTime ocufFyhIni)
        {
            var item = await _ctx.Ocupaciones
                .Include(o => o.Plaza).ThenInclude(p => p.Playa)
                .Include(o => o.Vehiculo)
                .Include(o => o.Pago)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.PlyID == plyID && o.PlzNum == plzNum && o.VehPtnt == vehPtnt && o.OcufFyhIni == ocufFyhIni);

            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int plzNum, string vehPtnt, DateTime ocufFyhIni)
        {
            var item = await _ctx.Ocupaciones.FindAsync(plyID, plzNum, vehPtnt, ocufFyhIni);
            if (item is null) return NotFound();

            _ctx.Ocupaciones.Remove(item);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
