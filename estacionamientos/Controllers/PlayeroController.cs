using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class PlayeroController : Controller
    {
        private readonly AppDbContext _context;
        public PlayeroController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index()
            => View(await _context.Playeros.AsNoTracking().ToListAsync());

        public async Task<IActionResult> Details(int id)
        {
            var entity = await _context.Playeros.AsNoTracking().FirstOrDefaultAsync(e => e.UsuNU == id);
            return entity is null ? NotFound() : View(entity);
        }

        public IActionResult Create() => View(new Playero());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Playero model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.Playeros.Add(model); // Inserta en Usuario y luego en Playero (TPT)
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _context.Playeros.FindAsync(id);
            return entity is null ? NotFound() : View(entity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Playero model)
        {
            if (id != model.UsuNU) return BadRequest();
            if (!ModelState.IsValid) return View(model);
            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Playeros.AsNoTracking().FirstOrDefaultAsync(e => e.UsuNU == id);
            return entity is null ? NotFound() : View(entity);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entity = await _context.Playeros.FindAsync(id);
            if (entity is null) return NotFound();
            _context.Playeros.Remove(entity);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
