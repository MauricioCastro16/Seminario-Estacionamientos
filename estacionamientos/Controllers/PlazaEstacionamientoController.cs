using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class PlazaEstacionamientoController : Controller
    {
        private readonly AppDbContext _ctx;
        public PlazaEstacionamientoController(AppDbContext ctx) => _ctx = ctx;

        private async Task LoadPlayas(int? selected = null)
        {
            var playas = await _ctx.Playas.AsNoTracking()
                .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new { p.PlyID, Nombre = p.PlyCiu + " - " + p.PlyDir })
                .ToListAsync();
            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", selected);
        }

        public async Task<IActionResult> Index()
        {
            var q = _ctx.Plazas.Include(p => p.Playa).AsNoTracking();
            return View(await q.ToListAsync());
        }

        public async Task<IActionResult> Details(int plyID, int plzNum)
        {
            var item = await _ctx.Plazas.Include(p => p.Playa).AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlyID == plyID && p.PlzNum == plzNum);
            return item is null ? NotFound() : View(item);
        }

        public async Task<IActionResult> Create()
        {
            await LoadPlayas();
            return View(new PlazaEstacionamiento());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PlazaEstacionamiento model)
        {
            if (await _ctx.Plazas.AnyAsync(p => p.PlyID == model.PlyID && p.PlzNum == model.PlzNum))
                ModelState.AddModelError(nameof(model.PlzNum), "Ya existe esa plaza en la playa seleccionada.");

            if (!ModelState.IsValid)
            {
                await LoadPlayas(model.PlyID);
                return View(model);
            }

            _ctx.Plazas.Add(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int plyID, int plzNum)
        {
            var item = await _ctx.Plazas.FindAsync(plyID, plzNum);
            if (item is null) return NotFound();

            await LoadPlayas(item.PlyID);
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int plzNum, PlazaEstacionamiento model)
        {
            if (plyID != model.PlyID || plzNum != model.PlzNum) return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadPlayas(model.PlyID);
                return View(model);
            }

            _ctx.Entry(model).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int plyID, int plzNum)
        {
            var item = await _ctx.Plazas.Include(p => p.Playa).AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlyID == plyID && p.PlzNum == plzNum);
            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int plzNum)
        {
            var item = await _ctx.Plazas.FindAsync(plyID, plzNum);
            if (item is null) return NotFound();

            _ctx.Plazas.Remove(item);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
