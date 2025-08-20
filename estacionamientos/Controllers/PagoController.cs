using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class PagoController : Controller
    {
        private readonly AppDbContext _ctx;
        public PagoController(AppDbContext ctx) => _ctx = ctx;

        private async Task LoadPlayas(int? selected = null)
        {
            var playas = await _ctx.Playas.AsNoTracking()
                .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new { p.PlyID, Nombre = p.PlyCiu + " - " + p.PlyDir })
                .ToListAsync();
            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", selected);
        }

        private async Task LoadMetodosAceptados(int plyID, int? selectedMep = null)
        {
            var metodos = await _ctx.AceptaMetodosPago
                .Where(a => a.PlyID == plyID && a.AmpHab)
                .Include(a => a.MetodoPago)
                .AsNoTracking()
                .Select(a => new { a.MepID, MepNom = a.MetodoPago != null ? a.MetodoPago.MepNom : "(Sin método)" })
                .ToListAsync();

            ViewBag.MepID = new SelectList(metodos, "MepID", "MepNom", selectedMep);
        }

        private Task<bool> EsAceptado(int plyID, int mepID)
            => _ctx.AceptaMetodosPago.AnyAsync(a => a.PlyID == plyID && a.MepID == mepID && a.AmpHab);

        public async Task<IActionResult> Index()
        {
            var q = _ctx.Pagos
                .Include(p => p.Playa)
                .Include(p => p.MetodoPago)
                .AsNoTracking();
            return View(await q.ToListAsync());
        }

        public async Task<IActionResult> Details(int plyID, int pagNum)
        {
            var item = await _ctx.Pagos
                .Include(p => p.Playa)
                .Include(p => p.MetodoPago)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlyID == plyID && p.PagNum == pagNum);

            return item is null ? NotFound() : View(item);
        }

        public async Task<IActionResult> Create()
        {
            await LoadPlayas();
            ViewBag.MepID = new SelectList(Enumerable.Empty<SelectListItem>()); // hasta elegir playa
            return View(new Pago { PagFyh = DateTime.Now });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Pago model)
        {
            if (!await EsAceptado(model.PlyID, model.MepID))
                ModelState.AddModelError(nameof(model.MepID), "La playa no acepta este método de pago.");

            // Evitar repetir PagNum dentro de la playa
            if (await _ctx.Pagos.AnyAsync(p => p.PlyID == model.PlyID && p.PagNum == model.PagNum))
                ModelState.AddModelError(nameof(model.PagNum), "Ya existe ese número de pago en esta playa.");

            if (!ModelState.IsValid)
            {
                await LoadPlayas(model.PlyID);
                await LoadMetodosAceptados(model.PlyID, model.MepID);
                return View(model);
            }

            _ctx.Pagos.Add(model);
            await _ctx.SaveChangesAsync(); // FK compuesta también valida en DB
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int plyID, int pagNum)
        {
            var item = await _ctx.Pagos.FindAsync(plyID, pagNum);
            if (item is null) return NotFound();

            await LoadPlayas(item.PlyID);
            await LoadMetodosAceptados(item.PlyID, item.MepID);
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int pagNum, Pago model)
        {
            if (plyID != model.PlyID || pagNum != model.PagNum) return BadRequest();

            if (!await EsAceptado(model.PlyID, model.MepID))
                ModelState.AddModelError(nameof(model.MepID), "La playa no acepta este método de pago.");

            if (!ModelState.IsValid)
            {
                await LoadPlayas(model.PlyID);
                await LoadMetodosAceptados(model.PlyID, model.MepID);
                return View(model);
            }

            _ctx.Entry(model).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int plyID, int pagNum)
        {
            var item = await _ctx.Pagos
                .Include(p => p.Playa)
                .Include(p => p.MetodoPago)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlyID == plyID && p.PagNum == pagNum);
            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int pagNum)
        {
            var item = await _ctx.Pagos.FindAsync(plyID, pagNum);
            if (item is null) return NotFound();

            _ctx.Pagos.Remove(item);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // (Opcional) endpoint para recargar métodos por playa vía AJAX
        // GET: /Pago/MetodosPorPlaya?plyID=1
        public async Task<IActionResult> MetodosPorPlaya(int plyID)
        {
            var metodos = await _ctx.AceptaMetodosPago
                .Where(a => a.PlyID == plyID && a.AmpHab)
                .Include(a => a.MetodoPago)
                .Select(a => new { a.MepID, MepNom = a.MetodoPago != null ? a.MetodoPago.MepNom : "(Sin método)" })
                .ToListAsync();

            return Json(metodos);
        }
    }
}
