using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class AbonadoController : Controller
    {
        private readonly AppDbContext _ctx;
        public AbonadoController(AppDbContext ctx) => _ctx = ctx;

        private async Task LoadConductores(int? selected = null)
        {
            var conductores = await _ctx.Conductores.AsNoTracking()
                .OrderBy(c => c.UsuNyA)
                .Select(c => new { c.UsuNU, c.UsuNyA })
                .ToListAsync();
            ViewBag.ConNU = new SelectList(conductores, "UsuNU", "UsuNyA", selected);
        }

        public async Task<IActionResult> Index()
            => View(await _ctx.Abonados.AsNoTracking().OrderBy(a => a.AboNom).ToListAsync());

        public async Task<IActionResult> Details(string id)
        {
            var item = await _ctx.Abonados.Include(a => a.Conductor).AsNoTracking()
                .FirstOrDefaultAsync(a => a.AboDNI == id);
            return item is null ? NotFound() : View(item);
        }

        public async Task<IActionResult> Create() { await LoadConductores(); return View(new Abonado()); }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Abonado model)
        {
            if (!ModelState.IsValid) { await LoadConductores(model.ConNU); return View(model); }
            _ctx.Abonados.Add(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            var item = await _ctx.Abonados.FindAsync(id);
            if (item is null) return NotFound();
            await LoadConductores(item.ConNU);
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Abonado model)
        {
            if (id != model.AboDNI) return BadRequest();
            if (!ModelState.IsValid) { await LoadConductores(model.ConNU); return View(model); }
            _ctx.Entry(model).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            var item = await _ctx.Abonados.AsNoTracking().FirstOrDefaultAsync(a => a.AboDNI == id);
            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var item = await _ctx.Abonados.FindAsync(id);
            if (item is null) return NotFound();
            _ctx.Abonados.Remove(item);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
