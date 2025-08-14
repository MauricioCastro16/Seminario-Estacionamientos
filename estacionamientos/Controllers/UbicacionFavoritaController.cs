using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class UbicacionFavoritaController : Controller
    {
        private readonly AppDbContext _context;
        public UbicacionFavoritaController(AppDbContext context) => _context = context;

        // Helpers: combo de conductores
        private async Task LoadConductoresSelectList(int? selected = null)
        {
            var conductores = await _context.Conductores
                .AsNoTracking()
                .OrderBy(c => c.UsuNyA)
                .Select(c => new { c.UsuNU, c.UsuNyA })
                .ToListAsync();

            ViewBag.ConNU = new SelectList(conductores, "UsuNU", "UsuNyA", selected);
        }

        // GET: /UbicacionFavorita
        public async Task<IActionResult> Index()
        {
            var q = _context.UbicacionesFavoritas
                    .Include(u => u.Conductor)
                    .AsNoTracking();
            return View(await q.ToListAsync());
        }

        // GET: /UbicacionFavorita/Details?conNU=1&ubfApodo=Casa
        public async Task<IActionResult> Details(int conNU, string ubfApodo)
        {
            var item = await _context.UbicacionesFavoritas
                .Include(u => u.Conductor)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.ConNU == conNU && u.UbfApodo == ubfApodo);

            return item is null ? NotFound() : View(item);
        }

        // GET: /UbicacionFavorita/Create
        public async Task<IActionResult> Create()
        {
            await LoadConductoresSelectList();
            return View(new UbicacionFavorita());
        }

        // POST: /UbicacionFavorita/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UbicacionFavorita model)
        {
            if (await _context.UbicacionesFavoritas.AnyAsync(u => u.ConNU == model.ConNU && u.UbfApodo == model.UbfApodo))
                ModelState.AddModelError(nameof(model.UbfApodo), "Ya existe ese apodo para este conductor.");

            if (!ModelState.IsValid)
            {
                await LoadConductoresSelectList(model.ConNU);
                return View(model);
            }

            _context.UbicacionesFavoritas.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /UbicacionFavorita/Edit?conNU=1&ubfApodo=Casa
        public async Task<IActionResult> Edit(int conNU, string ubfApodo)
        {
            var item = await _context.UbicacionesFavoritas.FindAsync(conNU, ubfApodo);
            if (item is null) return NotFound();

            await LoadConductoresSelectList(item.ConNU);
            return View(item);
        }

        // POST: /UbicacionFavorita/Edit
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int conNU, string ubfApodo, UbicacionFavorita model)
        {
            // Evitamos que cambien la PK desde el form (si querés permitir cambiar apodo, es un delete+create)
            if (conNU != model.ConNU || ubfApodo != model.UbfApodo) return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadConductoresSelectList(model.ConNU);
                return View(model);
            }

            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /UbicacionFavorita/Delete?conNU=1&ubfApodo=Casa
        public async Task<IActionResult> Delete(int conNU, string ubfApodo)
        {
            var item = await _context.UbicacionesFavoritas
                .Include(u => u.Conductor)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.ConNU == conNU && u.UbfApodo == ubfApodo);

            return item is null ? NotFound() : View(item);
        }

        // POST: /UbicacionFavorita/Delete
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int conNU, string ubfApodo)
        {
            var item = await _context.UbicacionesFavoritas.FindAsync(conNU, ubfApodo);
            if (item is null) return NotFound();

            _context.UbicacionesFavoritas.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
