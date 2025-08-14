using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class ConduceController : Controller
    {
        private readonly AppDbContext _context;
        public ConduceController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var q = _context.Conducciones
                .Include(c => c.Conductor)
                .Include(c => c.Vehiculo)
                .AsNoTracking();

            return View(await q.ToListAsync());
        }

        public async Task<IActionResult> Details(int conNU, string vehPtnt)
        {
            var item = await _context.Conducciones
                .Include(c => c.Conductor)
                .Include(c => c.Vehiculo)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ConNU == conNU && c.VehPtnt == vehPtnt);

            return item is null ? NotFound() : View(item);
        }

        public async Task<IActionResult> Create()
        {
            await LoadSelectLists();
            return View(new Conduce());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Conduce model)
        {
            if (!await ExistsConductor(model.ConNU))
                ModelState.AddModelError(nameof(model.ConNU), "Conductor inexistente.");
            if (!await ExistsVehiculo(model.VehPtnt))
                ModelState.AddModelError(nameof(model.VehPtnt), "Vehículo inexistente.");

            if (await _context.Conducciones.AnyAsync(c => c.ConNU == model.ConNU && c.VehPtnt == model.VehPtnt))
                ModelState.AddModelError(string.Empty, "Ya existe esa asignación Conductor-Vehículo.");

            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                return View(model);
            }

            _context.Conducciones.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int conNU, string vehPtnt)
        {
            var item = await _context.Conducciones
                .Include(c => c.Conductor)
                .Include(c => c.Vehiculo)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ConNU == conNU && c.VehPtnt == vehPtnt);

            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int conNU, string vehPtnt)
        {
            var item = await _context.Conducciones.FindAsync(conNU, vehPtnt);
            if (item is null) return NotFound();

            _context.Conducciones.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Helpers
        private async Task LoadSelectLists()
        {
            var conductores = await _context.Conductores
                .AsNoTracking().Select(c => new { c.UsuNU, c.UsuNyA }).ToListAsync();
            var vehiculos = await _context.Vehiculos
                .AsNoTracking().Select(v => new { v.VehPtnt, v.VehMarc }).ToListAsync();

            ViewBag.Conductores = new SelectList(conductores, "UsuNU", "UsuNyA");
            ViewBag.Vehiculos = new SelectList(vehiculos, "VehPtnt", "VehMarc");
        }

        private Task<bool> ExistsConductor(int conNU)
            => _context.Conductores.AnyAsync(c => c.UsuNU == conNU);

        private Task<bool> ExistsVehiculo(string vehPtnt)
            => _context.Vehiculos.AnyAsync(v => v.VehPtnt == vehPtnt);
    }
}
