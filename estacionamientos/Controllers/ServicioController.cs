using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class ServicioController : Controller
    {
        private readonly AppDbContext _ctx;
        public ServicioController(AppDbContext ctx) => _ctx = ctx;

        public async Task<IActionResult> Index()
            => View(await _ctx.Servicios.AsNoTracking().OrderBy(s => s.SerNom).ToListAsync());

        public async Task<IActionResult> Details(int id)
            => View(await _ctx.Servicios.AsNoTracking().FirstOrDefaultAsync(s => s.SerID == id) ?? (object)NotFound());

        public IActionResult Create() => View(new Servicio());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Servicio model)
        {
            if (!ModelState.IsValid) return View(model);
            _ctx.Servicios.Add(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var servicio = await _ctx.Servicios.FindAsync(id);
            return servicio is null ? NotFound() : View(servicio);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Servicio model)
        {
            if (id != model.SerID) return BadRequest();
            if (!ModelState.IsValid) return View(model);
            _ctx.Entry(model).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var item = await _ctx.Servicios.AsNoTracking().FirstOrDefaultAsync(s => s.SerID == id);
            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _ctx.Servicios.FindAsync(id);
            if (item is null) return NotFound();

            try
            {
                _ctx.Servicios.Remove(item);
                await _ctx.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "No se puede eliminar: existen registros que lo usan.");
                return View("Delete", item);
            }
        }
    }
}
