using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class ValoracionController : Controller
    {
        private readonly AppDbContext _context;
        public ValoracionController(AppDbContext context) => _context = context;

        private async Task LoadSelects(int? conSelected = null, int? plySelected = null)
        {
            var conductores = await _context.Conductores
                .AsNoTracking().OrderBy(c => c.UsuNyA)
                .Select(c => new { c.UsuNU, c.UsuNyA })
                .ToListAsync();
            var playas = await _context.Playas
                .AsNoTracking().OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new { p.PlyID, Nombre = p.PlyCiu + " - " + p.PlyDir })
                .ToListAsync();

            ViewBag.ConNU = new SelectList(conductores, "UsuNU", "UsuNyA", conSelected);
            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", plySelected);
        }

        public async Task<IActionResult> Index()
        {
            var q = _context.Valoraciones
                .Include(v => v.Conductor)
                .Include(v => v.Playa)
                .AsNoTracking();
            return View(await q.ToListAsync());
        }

        public async Task<IActionResult> Details(int plyID, int conNU)
        {
            var item = await _context.Valoraciones
                .Include(v => v.Conductor)
                .Include(v => v.Playa)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.PlyID == plyID && v.ConNU == conNU);

            return item is null ? NotFound() : View(item);
        }

        public async Task<IActionResult> Create()
        {
            await LoadSelects();
            return View(new Valoracion { ValNumEst = 5 });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Valoracion model)
        {
            if (await _context.Valoraciones.AnyAsync(v => v.PlyID == model.PlyID && v.ConNU == model.ConNU))
                ModelState.AddModelError(string.Empty, "El conductor ya valoró esta playa.");

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.ConNU, model.PlyID);
                return View(model);
            }

            _context.Valoraciones.Add(model);
            await _context.SaveChangesAsync(); // recalcula PlyValProm en SaveChangesAsync
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int plyID, int conNU)
        {
            var item = await _context.Valoraciones.FindAsync(plyID, conNU);
            if (item is null) return NotFound();
            await LoadSelects(item.ConNU, item.PlyID);
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int conNU, Valoracion model)
        {
            if (plyID != model.PlyID || conNU != model.ConNU) return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.ConNU, model.PlyID);
                return View(model);
            }

            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync(); // recalcula
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int plyID, int conNU)
        {
            var item = await _context.Valoraciones
                .Include(v => v.Conductor)
                .Include(v => v.Playa)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.PlyID == plyID && v.ConNU == conNU);
            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int conNU)
        {
            var item = await _context.Valoraciones.FindAsync(plyID, conNU);
            if (item is null) return NotFound();

            _context.Valoraciones.Remove(item);
            await _context.SaveChangesAsync(); // recalcula
            return RedirectToAction(nameof(Index));
        }
    }
}
