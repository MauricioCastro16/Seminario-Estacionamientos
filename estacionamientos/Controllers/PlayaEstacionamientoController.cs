using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using System.Security.Claims;

namespace estacionamientos.Controllers
{
    public class PlayaEstacionamientoController : Controller
    {
        private readonly AppDbContext _context;

        public PlayaEstacionamientoController(AppDbContext context) => _context = context;

        // Muestra todas las playas de estacionamiento
        [HttpGet]
        [Route("Playas")]
        public async Task<IActionResult> Index()
        {
            var usuNU = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var playas = await _context.AdministraPlayas
            .Where(ap => ap.DueNU == usuNU)
            .Include(ap => ap.Playa)
            .Select(ap => ap.Playa)
            .AsNoTracking()
            .ToListAsync();
            return View(playas);
        }

        public async Task<IActionResult> Details(int id)
        {
            var playa = await _context.Playas
                .Include(p => p.Valoraciones)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlyID == id);

            return playa is null ? NotFound() : View(playa);
        }

        public IActionResult Create() => View(new PlayaEstacionamiento());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PlayaEstacionamiento model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.Playas.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var playa = await _context.Playas.FindAsync(id);
            return playa is null ? NotFound() : View(playa);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PlayaEstacionamiento model)
        {
            if (id != model.PlyID) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            // PlyValProm se recalcula al guardar valoraciones; acá lo dejamos tal cual venga (o podrías ocultarlo en la vista)
            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var playa = await _context.Playas.AsNoTracking().FirstOrDefaultAsync(p => p.PlyID == id);
            return playa is null ? NotFound() : View(playa);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var playa = await _context.Playas.FindAsync(id);
            if (playa is null) return NotFound();
            _context.Playas.Remove(playa);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


    }
}
