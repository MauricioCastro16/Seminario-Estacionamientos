using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class HorarioController : Controller
    {
        private readonly AppDbContext _ctx;
        public HorarioController(AppDbContext ctx) => _ctx = ctx;

        // Helpers: combos
        private async Task LoadSelects(int? plySel = null, int? claSel = null)
        {
            var playas = await _ctx.Playas.AsNoTracking()
                .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new { p.PlyID, Nombre = p.PlyCiu + " - " + p.PlyDir })
                .ToListAsync();

            var clasifs = await _ctx.ClasificacionesDias.AsNoTracking()
                .OrderBy(c => c.ClaDiasID)
                .Select(c => new { c.ClaDiasID, c.ClaDiasTipo })
                .ToListAsync();

            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", plySel);
            ViewBag.ClaDiasID = new SelectList(clasifs, "ClaDiasID", "ClaDiasTipo", claSel);
        }

        public async Task<IActionResult> Index()
        {
            var q = _ctx.Horarios
                .Include(h => h.Playa)
                .Include(h => h.ClasificacionDias)
                .AsNoTracking();

            return View(await q.ToListAsync());
        }

        public async Task<IActionResult> Details(int plyID, int claDiasID, DateTime horFyhIni)
        {
            var item = await _ctx.Horarios
                .Include(h => h.Playa)
                .Include(h => h.ClasificacionDias)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.PlyID == plyID && h.ClaDiasID == claDiasID && h.HorFyhIni == horFyhIni);

            return item is null ? NotFound() : View(item);
        }

        public async Task<IActionResult> Create()
        {
            await LoadSelects();
            return View(new Horario { HorFyhIni = DateTime.Today.AddHours(8) });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Horario model)
        {
            // Evitar solapados exactos con misma PK
            var exists = await _ctx.Horarios.AnyAsync(h =>
                h.PlyID == model.PlyID && h.ClaDiasID == model.ClaDiasID && h.HorFyhIni == model.HorFyhIni);

            if (exists)
                ModelState.AddModelError(string.Empty, "Ya existe un horario con ese inicio para esa playa y esa clasificación.");

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.ClaDiasID);
                return View(model);
            }

            _ctx.Horarios.Add(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int plyID, int claDiasID, DateTime horFyhIni)
        {
            var item = await _ctx.Horarios.FindAsync(plyID, claDiasID, horFyhIni);
            if (item is null) return NotFound();

            await LoadSelects(item.PlyID, item.ClaDiasID);
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int claDiasID, DateTime horFyhIni, Horario model)
        {
            // PK fija (si querés cambiarla, hacé delete+create)
            if (plyID != model.PlyID || claDiasID != model.ClaDiasID || horFyhIni != model.HorFyhIni)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.ClaDiasID);
                return View(model);
            }

            _ctx.Entry(model).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int plyID, int claDiasID, DateTime horFyhIni)
        {
            var item = await _ctx.Horarios
                .Include(h => h.Playa)
                .Include(h => h.ClasificacionDias)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.PlyID == plyID && h.ClaDiasID == claDiasID && h.HorFyhIni == horFyhIni);

            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int claDiasID, DateTime horFyhIni)
        {
            var item = await _ctx.Horarios.FindAsync(plyID, claDiasID, horFyhIni);
            if (item is null) return NotFound();

            _ctx.Horarios.Remove(item);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
