using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class TarifaServicioController : Controller
    {
        private readonly AppDbContext _ctx;
        public TarifaServicioController(AppDbContext ctx) => _ctx = ctx;

        private async Task LoadSelects(int? plySel = null, int? serSel = null, int? clasSel = null)
        {
            var playas = await _ctx.Playas.AsNoTracking()
                .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new { p.PlyID, Nombre = p.PlyCiu + " - " + p.PlyDir })
                .ToListAsync();

            var servicios = await _ctx.Servicios.AsNoTracking()
                .OrderBy(s => s.SerNom).ToListAsync();

            var clases = await _ctx.ClasificacionesVehiculo.AsNoTracking()
                .OrderBy(c => c.ClasVehTipo).ToListAsync();

            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", plySel);
            ViewBag.SerID = new SelectList(servicios, "SerID", "SerNom", serSel);
            ViewBag.ClasVehID = new SelectList(clases, "ClasVehID", "ClasVehTipo", clasSel);
        }

        // INDEX
        public async Task<IActionResult> Index()
        {
            var q = _ctx.TarifasServicio
                .Include(t => t.ServicioProveido).ThenInclude(sp => sp.Servicio)
                .Include(t => t.ServicioProveido).ThenInclude(sp => sp.Playa)
                .Include(t => t.ClasificacionVehiculo)
                .AsNoTracking();

            return View(await q.ToListAsync());
        }

        // DETAILS
        public async Task<IActionResult> Details(int plyID, int serID, int clasVehID, DateTime tasFecIni)
        {
            var item = await _ctx.TarifasServicio
                .Include(t => t.ServicioProveido).ThenInclude(sp => sp.Servicio)
                .Include(t => t.ServicioProveido).ThenInclude(sp => sp.Playa)
                .Include(t => t.ClasificacionVehiculo)
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.PlyID == plyID &&
                    t.SerID == serID &&
                    t.ClasVehID == clasVehID &&
                    t.TasFecIni == tasFecIni);

            return item is null ? NotFound() : View(item);
        }

        // CREATE GET
        public async Task<IActionResult> Create()
        {
            await LoadSelects();
            return View(new TarifaServicio { TasFecIni = DateTime.Today });
        }

        // CREATE POST
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TarifaServicio model)
        {
            try
            {
                // Normalizar fechas a UTC
                model.TasFecIni = DateTime.SpecifyKind(model.TasFecIni, DateTimeKind.Utc);
                if (model.TasFecFin.HasValue)
                    model.TasFecFin = DateTime.SpecifyKind(model.TasFecFin.Value, DateTimeKind.Utc);

                // Validar que la playa ofrezca el servicio
                var existeSP = await _ctx.ServiciosProveidos
                    .AnyAsync(sp => sp.PlyID == model.PlyID && sp.SerID == model.SerID);

                if (!existeSP)
                    ModelState.AddModelError("", "La playa no ofrece ese servicio.");

                if (!ModelState.IsValid)
                {
                    await LoadSelects(model.PlyID, model.SerID, model.ClasVehID);
                    return View(model);
                }

                // Cerrar la tarifa vigente (si existe)
                var vigente = await _ctx.TarifasServicio
                    .FirstOrDefaultAsync(t =>
                        t.PlyID == model.PlyID &&
                        t.SerID == model.SerID &&
                        t.ClasVehID == model.ClasVehID &&
                        t.TasFecFin == null);

                if (vigente != null)
                {
                    vigente.TasFecFin = model.TasFecIni.AddSeconds(-1);
                    _ctx.Update(vigente);
                }

                model.TasFecFin = null; // nueva tarifa queda vigente
                _ctx.TarifasServicio.Add(model);

                await _ctx.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                await LoadSelects(model.PlyID, model.SerID, model.ClasVehID);
                return View(model);
            }
        }

        // EDIT GET
        public async Task<IActionResult> Edit(int plyID, int serID, int clasVehID, DateTime tasFecIni)
        {
            tasFecIni = DateTime.SpecifyKind(tasFecIni, DateTimeKind.Utc);

            var item = await _ctx.TarifasServicio.FindAsync(plyID, serID, clasVehID, tasFecIni);
            if (item is null) return NotFound();

            await LoadSelects(item.PlyID, item.SerID, item.ClasVehID);
            return View(item);
        }

        // EDIT POST
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int serID, int clasVehID, DateTime tasFecIni, TarifaServicio model)
        {
            if (plyID != model.PlyID || serID != model.SerID || clasVehID != model.ClasVehID || tasFecIni != model.TasFecIni)
                return BadRequest();

            // Normalizar fechas a UTC
            model.TasFecIni = DateTime.SpecifyKind(model.TasFecIni, DateTimeKind.Utc);
            if (model.TasFecFin.HasValue)
                model.TasFecFin = DateTime.SpecifyKind(model.TasFecFin.Value, DateTimeKind.Utc);

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.SerID, model.ClasVehID);
                return View(model);
            }

            // Verificar solapamientos si cambió TasFecFin
            if (model.TasFecFin != null)
            {
                bool solapa = await _ctx.TarifasServicio.AnyAsync(t =>
                    t.PlyID == model.PlyID &&
                    t.SerID == model.SerID &&
                    t.ClasVehID == model.ClasVehID &&
                    t.TasFecIni > model.TasFecIni &&
                    (t.TasFecFin == null || t.TasFecFin > model.TasFecFin));

                if (solapa)
                {
                    ModelState.AddModelError("", "Las fechas ingresadas generan solapamiento con otra tarifa.");
                    await LoadSelects(model.PlyID, model.SerID, model.ClasVehID);
                    return View(model);
                }
            }

            _ctx.Entry(model).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // DELETE GET (Cerrar)
        public async Task<IActionResult> Delete(int plyID, int serID, int clasVehID, DateTime tasFecIni)
        {
            var item = await _ctx.TarifasServicio
                .Include(t => t.ServicioProveido).ThenInclude(sp => sp.Servicio)
                .Include(t => t.ServicioProveido).ThenInclude(sp => sp.Playa)
                .Include(t => t.ClasificacionVehiculo)
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.PlyID == plyID &&
                    t.SerID == serID &&
                    t.ClasVehID == clasVehID &&
                    t.TasFecIni == tasFecIni);

            return item is null ? NotFound() : View(item);
        }

        // DELETE POST (Cerrar = asignar fecha fin)
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int serID, int clasVehID, DateTime tasFecIni)
        {
            var item = await _ctx.TarifasServicio.FindAsync(plyID, serID, clasVehID, tasFecIni);
            if (item is null) return NotFound();

            if (item.TasFecFin == null)
            {
                item.TasFecFin = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
                _ctx.Update(item);
                await _ctx.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
