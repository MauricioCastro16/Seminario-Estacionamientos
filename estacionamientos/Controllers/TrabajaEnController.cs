using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class TrabajaEnController : Controller
    {
        private readonly AppDbContext _ctx;
        public TrabajaEnController(AppDbContext ctx) => _ctx = ctx;

        private async Task LoadSelects(int? plySel = null, int? plaSel = null)
        {
            var playas = await _ctx.Playas.AsNoTracking()
                .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new { p.PlyID, Nombre = p.PlyCiu + " - " + p.PlyDir })
                .ToListAsync();

            var playeros = await _ctx.Playeros.AsNoTracking()
                .OrderBy(p => p.UsuNyA)
                .Select(p => new { p.UsuNU, p.UsuNyA })
                .ToListAsync();

            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", plySel);
            ViewBag.PlaNU = new SelectList(playeros, "UsuNU", "UsuNyA", plaSel);
        }

        public async Task<IActionResult> Index()
        {
            var q = _ctx.Trabajos
                .Include(t => t.Playa)
                .Include(t => t.Playero)
                .Where(t => t.TrabEnActual)
                .AsNoTracking();
            return View(await q.ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            await LoadSelects();
            return View(new TrabajaEn());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TrabajaEn model)
        {
            if (await _ctx.Trabajos.AnyAsync(x => x.PlyID == model.PlyID && x.PlaNU == model.PlaNU))
                ModelState.AddModelError(string.Empty, "Ese playero ya está asignado a esa playa.");

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.PlaNU);
                return View(model);
            }

            model.TrabEnActual = true;
            _ctx.Trabajos.Add(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int plyID, int plaNU)
        {
            var item = await _ctx.Trabajos
                .Include(t => t.Playa)
                .Include(t => t.Playero)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.PlyID == plyID && t.PlaNU == plaNU);

            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int plaNU)
        {
            var item = await _ctx.Trabajos.FindAsync(plyID, plaNU);
            if (item is null) return NotFound();

            item.TrabEnActual = false;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
