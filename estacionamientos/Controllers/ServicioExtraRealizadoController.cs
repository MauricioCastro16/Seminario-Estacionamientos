using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class ServicioExtraRealizadoController : Controller
    {
        private readonly AppDbContext _ctx;
        public ServicioExtraRealizadoController(AppDbContext ctx) => _ctx = ctx;

        // -------- Helpers (combos) --------
        private async Task LoadPlayas(int? plySel = null)
        {
            var playas = await _ctx.Playas.AsNoTracking()
                .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new { p.PlyID, Nombre = p.PlyCiu + " - " + p.PlyDir })
                .ToListAsync();
            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", plySel);
        }

        private async Task LoadServiciosHabilitados(int plyID, int? serSel = null)
        {
            var servicios = await _ctx.ServiciosProveidos
                .Where(sp => sp.PlyID == plyID && sp.SerProvHab)
                .Include(sp => sp.Servicio)
                .AsNoTracking()
                .OrderBy(sp => sp.Servicio.SerNom)
                .Select(sp => new { sp.SerID, sp.Servicio.SerNom })
                .ToListAsync();

            ViewBag.SerID = new SelectList(servicios, "SerID", "SerNom", serSel);
        }

        private async Task LoadVehiculos(string? selected = null)
        {
            var vehs = await _ctx.Vehiculos.AsNoTracking()
                .OrderBy(v => v.VehPtnt)
                .Select(v => v.VehPtnt)
                .ToListAsync();
            ViewBag.VehPtnt = new SelectList(vehs, selected);
        }

        private async Task LoadPagosDePlaya(int plyID, int? pagSel = null)
        {
            var pagos = await _ctx.Pagos.AsNoTracking()
                .Where(p => p.PlyID == plyID)
                .OrderByDescending(p => p.PagFyh)
                .Select(p => new { p.PagNum, Texto = p.PagNum + " - " + p.PagFyh.ToString("g") })
                .ToListAsync();

            ViewBag.PagNum = new SelectList(pagos, "PagNum", "Texto", pagSel);
        }

        private Task<bool> ServicioHabilitadoEnPlaya(int plyID, int serID)
            => _ctx.ServiciosProveidos.AnyAsync(sp => sp.PlyID == plyID && sp.SerID == serID && sp.SerProvHab);

        private Task<bool> PagoExisteEnPlaya(int plyID, int? pagNum)
            => pagNum is null
                ? Task.FromResult(true)
                : _ctx.Pagos.AnyAsync(p => p.PlyID == plyID && p.PagNum == pagNum.Value);

        // -------- CRUD --------
        public async Task<IActionResult> Index()
        {
            var q = _ctx.ServiciosExtrasRealizados
                .Include(se => se.ServicioProveido).ThenInclude(sp => sp.Servicio)
                .Include(se => se.ServicioProveido).ThenInclude(sp => sp.Playa)
                .Include(se => se.Vehiculo)
                .Include(se => se.Pago)
                .AsNoTracking();

            return View(await q.ToListAsync());
        }

        public async Task<IActionResult> Details(int plyID, int serID, string vehPtnt, DateTime servExFyHIni)
        {
            var item = await _ctx.ServiciosExtrasRealizados
                .Include(se => se.ServicioProveido).ThenInclude(sp => sp.Servicio)
                .Include(se => se.ServicioProveido).ThenInclude(sp => sp.Playa)
                .Include(se => se.Vehiculo)
                .Include(se => se.Pago)
                .AsNoTracking()
                .FirstOrDefaultAsync(se =>
                    se.PlyID == plyID && se.SerID == serID &&
                    se.VehPtnt == vehPtnt && se.ServExFyHIni == servExFyHIni);

            return item is null ? NotFound() : View(item);
        }

        public async Task<IActionResult> Create()
        {
            await LoadPlayas();
            ViewBag.SerID = new SelectList(Enumerable.Empty<SelectListItem>()); // hasta elegir playa
            await LoadVehiculos();
            ViewBag.PagNum = new SelectList(Enumerable.Empty<SelectListItem>());
            return View(new ServicioExtraRealizado { ServExFyHIni = DateTime.Now });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServicioExtraRealizado model)
        {
            if (!await ServicioHabilitadoEnPlaya(model.PlyID, model.SerID))
                ModelState.AddModelError(nameof(model.SerID), "La playa no ofrece este servicio o no está habilitado.");

            if (!await PagoExisteEnPlaya(model.PlyID, model.PagNum))
                ModelState.AddModelError(nameof(model.PagNum), "El pago no existe en esta playa.");

            if (!ModelState.IsValid)
            {
                await LoadPlayas(model.PlyID);
                await LoadServiciosHabilitados(model.PlyID, model.SerID);
                await LoadVehiculos(model.VehPtnt);
                await LoadPagosDePlaya(model.PlyID, model.PagNum);
                return View(model);
            }

            _ctx.ServiciosExtrasRealizados.Add(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int plyID, int serID, string vehPtnt, DateTime servExFyHIni)
        {
            var item = await _ctx.ServiciosExtrasRealizados.FindAsync(plyID, serID, vehPtnt, servExFyHIni);
            if (item is null) return NotFound();

            await LoadPlayas(item.PlyID);
            await LoadServiciosHabilitados(item.PlyID, item.SerID);
            await LoadVehiculos(item.VehPtnt);
            await LoadPagosDePlaya(item.PlyID, item.PagNum);
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int serID, string vehPtnt, DateTime servExFyHIni, ServicioExtraRealizado model)
        {
            // PK fija: si querés permitir cambiarla, hacé delete+create
            if (plyID != model.PlyID || serID != model.SerID || vehPtnt != model.VehPtnt || servExFyHIni != model.ServExFyHIni)
                return BadRequest();

            if (!await ServicioHabilitadoEnPlaya(model.PlyID, model.SerID))
                ModelState.AddModelError(nameof(model.SerID), "La playa no ofrece este servicio o no está habilitado.");

            if (!await PagoExisteEnPlaya(model.PlyID, model.PagNum))
                ModelState.AddModelError(nameof(model.PagNum), "El pago no existe en esta playa.");

            if (!ModelState.IsValid)
            {
                await LoadPlayas(model.PlyID);
                await LoadServiciosHabilitados(model.PlyID, model.SerID);
                await LoadVehiculos(model.VehPtnt);
                await LoadPagosDePlaya(model.PlyID, model.PagNum);
                return View(model);
            }

            _ctx.Entry(model).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int plyID, int serID, string vehPtnt, DateTime servExFyHIni)
        {
            var item = await _ctx.ServiciosExtrasRealizados
                .Include(se => se.ServicioProveido).ThenInclude(sp => sp.Servicio)
                .Include(se => se.ServicioProveido).ThenInclude(sp => sp.Playa)
                .Include(se => se.Vehiculo)
                .Include(se => se.Pago)
                .AsNoTracking()
                .FirstOrDefaultAsync(se =>
                    se.PlyID == plyID && se.SerID == serID &&
                    se.VehPtnt == vehPtnt && se.ServExFyHIni == servExFyHIni);

            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int serID, string vehPtnt, DateTime servExFyHIni)
        {
            var item = await _ctx.ServiciosExtrasRealizados.FindAsync(plyID, serID, vehPtnt, servExFyHIni);
            if (item is null) return NotFound();

            _ctx.ServiciosExtrasRealizados.Remove(item);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // -------- AJAX helpers --------

        // GET: /ServicioExtraRealizado/ServiciosPorPlaya?plyID=1
        public async Task<IActionResult> ServiciosPorPlaya(int plyID)
        {
            var servicios = await _ctx.ServiciosProveidos
                .Where(sp => sp.PlyID == plyID && sp.SerProvHab)
                .Include(sp => sp.Servicio)
                .OrderBy(sp => sp.Servicio.SerNom)
                .Select(sp => new { sp.SerID, sp.Servicio.SerNom })
                .ToListAsync();

            return Json(servicios);
        }

        // GET: /ServicioExtraRealizado/PagosPorPlaya?plyID=1
        public async Task<IActionResult> PagosPorPlaya(int plyID)
        {
            var pagos = await _ctx.Pagos
                .Where(p => p.PlyID == plyID)
                .OrderByDescending(p => p.PagFyh)
                .Select(p => new { p.PagNum, Texto = p.PagNum + " - " + p.PagFyh.ToString("g") })
                .ToListAsync();

            return Json(pagos);
        }
    }
}
