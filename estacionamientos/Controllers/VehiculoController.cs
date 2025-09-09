using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class VehiculoController : Controller
    {
        private readonly AppDbContext _context;
        public VehiculoController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index()
            => View(await _context.Vehiculos.AsNoTracking().ToListAsync());

        public async Task<IActionResult> Details(string id)
        {
            if (id is null) return BadRequest();
            var veh = await _context.Vehiculos.AsNoTracking()
                         .FirstOrDefaultAsync(v => v.VehPtnt == id);
            return veh is null ? NotFound() : View(veh);
        }

        public IActionResult Create() => View(new Vehiculo());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vehiculo model)
        {
            if (!ModelState.IsValid) return View(model);
            _context.Vehiculos.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (id is null) return BadRequest();
            var veh = await _context.Vehiculos.FindAsync(id);
            return veh is null ? NotFound() : View(veh);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Vehiculo model)
        {
            if (id != model.VehPtnt) return BadRequest();
            if (!ModelState.IsValid) return View(model);
            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (id is null) return BadRequest();
            var veh = await _context.Vehiculos.AsNoTracking()
                         .FirstOrDefaultAsync(v => v.VehPtnt == id);
            return veh is null ? NotFound() : View(veh);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var veh = await _context.Vehiculos.FindAsync(id);
            if (veh is null) return NotFound();
            _context.Vehiculos.Remove(veh);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

    }

}
