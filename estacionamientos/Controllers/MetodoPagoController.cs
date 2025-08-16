using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class MetodoPagoController : Controller
    {
        private readonly AppDbContext _ctx;
        public MetodoPagoController(AppDbContext ctx) => _ctx = ctx;

        public async Task<IActionResult> Index()
            => View(await _ctx.MetodosPago.AsNoTracking().OrderBy(m => m.MepNom).ToListAsync());

        public async Task<IActionResult> Details(int id)
        {
            var item = await _ctx.MetodosPago.AsNoTracking().FirstOrDefaultAsync(m => m.MepID == id);
            return item is null ? NotFound() : View(item);
        }

        public IActionResult Create() => View(new MetodoPago());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MetodoPago model)
        {
            if (await _ctx.MetodosPago.AnyAsync(m => m.MepNom == model.MepNom))
                ModelState.AddModelError(nameof(model.MepNom), "Ya existe un método con ese nombre.");

            if (!ModelState.IsValid) return View(model);

            _ctx.MetodosPago.Add(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var item = await _ctx.MetodosPago.FindAsync(id);
            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MetodoPago model)
        {
            if (id != model.MepID) return BadRequest();

            if (await _ctx.MetodosPago.AnyAsync(m => m.MepID != model.MepID && m.MepNom == model.MepNom))
                ModelState.AddModelError(nameof(model.MepNom), "Ya existe un método con ese nombre.");

            if (!ModelState.IsValid) return View(model);

            _ctx.Entry(model).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var item = await _ctx.MetodosPago.AsNoTracking().FirstOrDefaultAsync(m => m.MepID == id);
            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _ctx.MetodosPago.FindAsync(id);
            if (item is null) return NotFound();

            try
            {
                _ctx.MetodosPago.Remove(item);
                await _ctx.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                // Si hay AceptaMetodoPago o Pago referenciándolo, puede fallar
                ModelState.AddModelError(string.Empty, "No se puede eliminar: hay registros que lo usan.");
                return View("Delete", item);
            }
        }
    }
}
