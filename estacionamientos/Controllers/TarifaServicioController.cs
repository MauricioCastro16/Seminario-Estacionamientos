using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using System.Linq;



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
                .Select(p => new { p.PlyID, Nombre = p.PlyNom })
                .ToListAsync();

            // Si hay una playa seleccionada, solo mostrar los servicios de esa playa
            IQueryable<Servicio> serviciosQuery;

            if (plySel.HasValue)
            {
                serviciosQuery =
                    from s in _ctx.Servicios.AsNoTracking()
                    join sp in _ctx.ServiciosProveidos.AsNoTracking()
                        on s.SerID equals sp.SerID
                    where sp.PlyID == plySel.Value && sp.SerProvHab == true
                    select s;
            }
            else
            {
                serviciosQuery = _ctx.Servicios.AsNoTracking();
            }

            var servicios = await serviciosQuery
                .OrderBy(s => s.SerNom)
                .ToListAsync();

            var clases = await _ctx.ClasificacionesVehiculo.AsNoTracking()
                .OrderBy(c => c.ClasVehTipo).ToListAsync();

            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", plySel);
            ViewBag.SerID = new SelectList(servicios, "SerID", "SerNom", serSel);
            ViewBag.ClasVehID = new SelectList(clases, "ClasVehID", "ClasVehTipo", clasSel);
        }

        // Helper: normaliza DateTime a UTC
        private DateTime ToUtc(DateTime dt) => DateTime.SpecifyKind(dt, DateTimeKind.Utc);

        // INDEX
        public async Task<IActionResult> Index()
        {
            var q = _ctx.TarifasServicio
                .Include(t => t.ServicioProveido).ThenInclude(sp => sp.Servicio)
                .Include(t => t.ServicioProveido).ThenInclude(sp => sp.Playa)
                .Include(t => t.ClasificacionVehiculo)
                .AsNoTracking()
                .OrderByDescending(t => t.TasFecFin == null || t.TasFecFin > DateTime.UtcNow) // vigentes arriba
                .ThenBy(t => t.ServicioProveido.Playa.PlyNom)
                .ThenBy(t => t.ServicioProveido.Servicio.SerNom);

            return View(await q.ToListAsync());
        }

        // DETAILS
        public async Task<IActionResult> Details(int plyID, int serID, int clasVehID, DateTime tasFecIni)
        {
            tasFecIni = ToUtc(tasFecIni);

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
        public async Task<IActionResult> Create(int? plySel = null)
        {
            await LoadSelects(plySel);
            return View(new TarifaServicio { TasFecIni = DateTime.Today });
        }


        // CREATE POST
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TarifaServicio model)
        {
            try
            {
                model.TasFecIni = ToUtc(model.TasFecIni);
                if (model.TasFecFin.HasValue)
                    model.TasFecFin = ToUtc(model.TasFecFin.Value);

                // 🔴 Validación: monto debe ser > 0
                if (model.TasMonto <= 0)
                    ModelState.AddModelError("TasMonto", "El monto debe ser mayor a 0.");

                var existeSP = await _ctx.ServiciosProveidos
                    .AnyAsync(sp => sp.PlyID == model.PlyID && sp.SerID == model.SerID);

                if (!existeSP)
                    ModelState.AddModelError("", "La playa no ofrece ese servicio.");

                var vigente = await _ctx.TarifasServicio
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t =>
                        t.PlyID == model.PlyID &&
                        t.SerID == model.SerID &&
                        t.ClasVehID == model.ClasVehID &&
                        t.TasFecFin == null);

             if (vigente != null)
                 ModelState.AddModelError("", "Ya existe una tarifa vigente para esta playa, servicio y clase de vehículo.");

                if (!ModelState.IsValid)
                {
                    await LoadSelects(model.PlyID, model.SerID, model.ClasVehID);
                    return View(model);
                }

                model.TasFecFin = null;
                _ctx.TarifasServicio.Add(model);
                await _ctx.SaveChangesAsync();

                TempData["Saved"] = true; // 👈 bandera para JS

                return RedirectToAction(nameof(Create), new { plySel = model.PlyID });

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
            tasFecIni = ToUtc(tasFecIni);

            var item = await _ctx.TarifasServicio.FindAsync(plyID, serID, clasVehID, tasFecIni);
            if (item is null) return NotFound();

            await LoadSelects(item.PlyID, item.SerID, item.ClasVehID);
            return View(item);
        }

        // EDIT POST
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int serID, int clasVehID, DateTime tasFecIni, TarifaServicio model)
        {
            tasFecIni = ToUtc(tasFecIni);

            if (plyID != model.PlyID || serID != model.SerID || clasVehID != model.ClasVehID || tasFecIni != ToUtc(model.TasFecIni))
                return BadRequest();

            model.TasFecIni = ToUtc(model.TasFecIni);
            if (model.TasFecFin.HasValue)
                model.TasFecFin = ToUtc(model.TasFecFin.Value);

            // 🔴 Validación: monto debe ser > 0
            if (model.TasMonto <= 0)
                ModelState.AddModelError("TasMonto", "El monto debe ser mayor a 0.");

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.SerID, model.ClasVehID);
                return View(model);
            }

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


        // DELETE GET
        public async Task<IActionResult> Delete(int plyID, int serID, int clasVehID, DateTime tasFecIni)
        {
            tasFecIni = ToUtc(tasFecIni);

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

        // DELETE POST
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int serID, int clasVehID, DateTime tasFecIni)
        {
            tasFecIni = ToUtc(tasFecIni);

            var item = await _ctx.TarifasServicio.FindAsync(plyID, serID, clasVehID, tasFecIni);
            if (item is null) return NotFound();

            // 🔴 Forzar cierre siempre, aunque ya tenga TasFecFin
            item.TasFecFin = ToUtc(DateTime.Now);

            _ctx.Update(item);
            await _ctx.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

    }
}
