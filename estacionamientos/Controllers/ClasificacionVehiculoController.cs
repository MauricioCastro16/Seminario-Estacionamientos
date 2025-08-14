using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class ClasificacionVehiculoController : Controller
    {
        private readonly AppDbContext _context;
        public ClasificacionVehiculoController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index()
            => View(await _context.ClasificacionesVehiculo.AsNoTracking().ToListAsync());

        public async Task<IActionResult> Details(int id)
        {
            var item = await _context.ClasificacionesVehiculo.AsNoTracking()
                         .FirstOrDefaultAsync(c => c.ClasVehID == id);
            return item is null ? NotFound() : View(item);
        }

        public IActionResult Create() => View(new ClasificacionVehiculo());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClasificacionVehiculo model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.ClasificacionesVehiculo.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _context.ClasificacionesVehiculo.FindAsync(id);
            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ClasificacionVehiculo model)
        {
            if (id != model.ClasVehID) return BadRequest();
            if (!ModelState.IsValid) return View(model);
            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.ClasificacionesVehiculo.AsNoTracking()
                         .FirstOrDefaultAsync(c => c.ClasVehID == id);
            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.ClasificacionesVehiculo.FindAsync(id);
            if (item is null) return NotFound();

            try
            {
                _context.ClasificacionesVehiculo.Remove(item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                // Si está en uso por Vehiculos, fallará por el Restrict
                ModelState.AddModelError(string.Empty, "No se puede eliminar: hay vehículos usando esta clasificación.");
                return View("Delete", item);
            }
        }
    }
}
