using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.ViewModels.SelectOptions;

namespace estacionamientos.Controllers
{
    public class AbonoController : Controller
    {
        private readonly AppDbContext _ctx;
        public AbonoController(AppDbContext ctx) => _ctx = ctx;

        private async Task LoadSelects(int? plySel = null, int? plzSel = null, string? dniSel = null, int? pagSel = null)
        {
            var playas = await _ctx.Playas.AsNoTracking()
                .OrderBy(p => p.PlyCiu).ThenBy(p => p.PlyDir)
                .Select(p => new { p.PlyID, Nombre = p.PlyCiu + " - " + p.PlyDir }).ToListAsync();
            ViewBag.PlyID = new SelectList(playas, "PlyID", "Nombre", plySel);

            var plazas = plySel is null
                ? new List<OpcionPlaza>()
                : await _ctx.Plazas.AsNoTracking()
                    .Where(p => p.PlyID == plySel)
                    .OrderBy(p => p.PlzNum)
                    .Select(p => new OpcionPlaza { PlzNum = p.PlzNum })
                    .ToListAsync();
                        ViewBag.PlzNum = new SelectList(plazas, "PlzNum", "PlzNum", plzSel);

            var abonados = await _ctx.Abonados.AsNoTracking()
                .OrderBy(a => a.AboNom).Select(a => new { a.AboDNI, a.AboNom }).ToListAsync();
            ViewBag.AboDNI = new SelectList(abonados, "AboDNI", "AboNom", dniSel);

            var pagos = plySel is null
                ? new List<OpcionPago>()
                : await _ctx.Pagos.AsNoTracking()
                    .Where(p => p.PlyID == plySel)
                    .OrderByDescending(p => p.PagFyh)
                    .Select(p => new OpcionPago { PagNum = p.PagNum, Texto = p.PagNum + " - " + p.PagFyh.ToString("g") })
                    .ToListAsync();
            ViewBag.PagNum = new SelectList(pagos, "PagNum", "Texto", pagSel);
        }

        private Task<bool> PagoExiste(int plyID, int pagNum)
            => _ctx.Pagos.AnyAsync(p => p.PlyID == plyID && p.PagNum == pagNum);

        public async Task<IActionResult> Index()
        {
            var q = _ctx.Abonos
                .Include(a => a.Plaza).ThenInclude(p => p.Playa)
                .Include(a => a.Abonado)
                .Include(a => a.Pago)
                .AsNoTracking();
            return View(await q.ToListAsync());
        }

        public async Task<IActionResult> Details(int plyID, int plzNum, DateTime aboFyhIni)
        {
            var item = await _ctx.Abonos
                .Include(a => a.Plaza).ThenInclude(p => p.Playa)
                .Include(a => a.Abonado)
                .Include(a => a.Pago)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.PlyID == plyID && a.PlzNum == plzNum && a.AboFyhIni == aboFyhIni);
            return item is null ? NotFound() : View(item);
        }

        public async Task<IActionResult> Create()
        {
            await LoadSelects();
            return View(new Abono { AboFyhIni = DateTime.Now });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Abono model)
        {
            if (!await PagoExiste(model.PlyID, model.PagNum))
                ModelState.AddModelError(nameof(model.PagNum), "El pago no existe para esa playa.");

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.PlzNum, model.AboDNI, model.PagNum);
                return View(model);
            }

            _ctx.Abonos.Add(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int plyID, int plzNum, DateTime aboFyhIni)
        {
            var item = await _ctx.Abonos.FindAsync(plyID, plzNum, aboFyhIni);
            if (item is null) return NotFound();
            await LoadSelects(item.PlyID, item.PlzNum, item.AboDNI, item.PagNum);
            return View(item);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int plyID, int plzNum, DateTime aboFyhIni, Abono model)
        {
            if (plyID != model.PlyID || plzNum != model.PlzNum || aboFyhIni != model.AboFyhIni) return BadRequest();

            if (!await PagoExiste(model.PlyID, model.PagNum))
                ModelState.AddModelError(nameof(model.PagNum), "El pago no existe para esa playa.");

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.PlzNum, model.AboDNI, model.PagNum);
                return View(model);
            }

            _ctx.Entry(model).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int plyID, int plzNum, DateTime aboFyhIni)
        {
            var item = await _ctx.Abonos
                .Include(a => a.Abonado).Include(a => a.Plaza).Include(a => a.Pago)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.PlyID == plyID && a.PlzNum == plzNum && a.AboFyhIni == aboFyhIni);
            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int plzNum, DateTime aboFyhIni)
        {
            var item = await _ctx.Abonos.FindAsync(plyID, plzNum, aboFyhIni);
            if (item is null) return NotFound();
            _ctx.Abonos.Remove(item);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
