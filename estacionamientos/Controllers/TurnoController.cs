using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class TurnoController : Controller
    {
        private readonly AppDbContext _ctx;
        public TurnoController(AppDbContext ctx) => _ctx = ctx;

        private async Task LoadSelects(int? plaSel = null, int? plySel = null)
        {
            var playeros = await _ctx.Playeros.AsNoTracking()
                .OrderBy(p => p.UsuNyA)
                .Select(p => new { p.UsuNU, p.UsuNyA })
                .ToListAsync();

            var playas = await _ctx.Playas.AsNoTracking()
                .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new { p.PlyID, Nombre = p.PlyCiu + " - " + p.PlyDir })
                .ToListAsync();

            ViewBag.PlaNU = new SelectList(playeros, "UsuNU", "UsuNyA", plaSel);
            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", plySel);
        }

        private Task<bool> TrabajaEnAsync(int plyID, int plaNU)
            => _ctx.Trabajos.AnyAsync(t => t.PlyID == plyID && t.PlaNU == plaNU);

        public async Task<IActionResult> Index()
        {
            var q = _ctx.Turnos
                .Include(t => t.Playero)
                .Include(t => t.Playa)
                .AsNoTracking();
            return View(await q.ToListAsync());
        }

        public async Task<IActionResult> Details(int plyID, int plaNU, DateTime turFyhIni)
        {
            var item = await _ctx.Turnos
                .Include(t => t.Playero)
                .Include(t => t.Playa)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.PlyID == plyID && t.PlaNU == plaNU && t.TurFyhIni == turFyhIni);

            return item is null ? NotFound() : View(item);
        }

        public async Task<IActionResult> Create()
        {
            await LoadSelects();
            return View(new Turno { TurFyhIni = DateTime.Now });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Turno model)
        {
            if (!await TrabajaEnAsync(model.PlyID, model.PlaNU))
                ModelState.AddModelError(string.Empty, "El playero no trabaja en esa playa.");

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlaNU, model.PlyID);
                return View(model);
            }

            _ctx.Turnos.Add(model);
            await _ctx.SaveChangesAsync(); // FK compuesta + restricción lo garantiza también en DB
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int plyID, int plaNU, DateTime turFyhIni)
        {
            var item = await _ctx.Turnos.FindAsync(plyID, plaNU, turFyhIni);
            if (item is null) return NotFound();

            await LoadSelects(item.PlaNU, item.PlyID);
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int plaNU, DateTime turFyhIni, Turno model)
        {
            // PK fija; si querés permitir moverlo, hacelo como delete+create
            if (plyID != model.PlyID || plaNU != model.PlaNU || turFyhIni != model.TurFyhIni) return BadRequest();

            if (!await TrabajaEnAsync(model.PlyID, model.PlaNU))
                ModelState.AddModelError(string.Empty, "El playero no trabaja en esa playa.");

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlaNU, model.PlyID);
                return View(model);
            }

            _ctx.Entry(model).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int plyID, int plaNU, DateTime turFyhIni)
        {
            var item = await _ctx.Turnos
                .Include(t => t.Playero)
                .Include(t => t.Playa)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.PlyID == plyID && t.PlaNU == plaNU && t.TurFyhIni == turFyhIni);

            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int plaNU, DateTime turFyhIni)
        {
            var item = await _ctx.Turnos.FindAsync(plyID, plaNU, turFyhIni);
            if (item is null) return NotFound();

            _ctx.Turnos.Remove(item);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
