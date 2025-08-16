using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.ViewModels.SelectOptions;

namespace estacionamientos.Controllers
{
    public class VehiculoAbonadoController : Controller
    {
        private readonly AppDbContext _ctx;
        public VehiculoAbonadoController(AppDbContext ctx) => _ctx = ctx;

        private async Task LoadSelects(int? plySel = null, int? plzSel = null, DateTime? aboIniSel = null, string? vehSel = null)
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

            var abonos = (plySel is null || plzSel is null)
                ? new List<OpcionAbono>()
                : await _ctx.Abonos.AsNoTracking()
                    .Where(a => a.PlyID == plySel && a.PlzNum == plzSel)
                    .OrderByDescending(a => a.AboFyhIni)
                    .Select(a => new OpcionAbono { AboFyhIni = a.AboFyhIni, Texto = a.AboFyhIni.ToString("g") })
                    .ToListAsync();
            ViewBag.AboFyhIni = new SelectList(abonos, "AboFyhIni", "Texto", aboIniSel);

            var vehiculos = await _ctx.Vehiculos.AsNoTracking()
                .OrderBy(v => v.VehPtnt).Select(v => v.VehPtnt).ToListAsync();
            ViewBag.VehPtnt = new SelectList(vehiculos, vehSel);
        }

        private Task<bool> AbonoExiste(int plyID, int plzNum, DateTime aboFyhIni)
            => _ctx.Abonos.AnyAsync(a => a.PlyID == plyID && a.PlzNum == plzNum && a.AboFyhIni == aboFyhIni);

        public async Task<IActionResult> Index()
        {
            var q = _ctx.VehiculosAbonados
                .Include(v => v.Abono).ThenInclude(a => a.Plaza)
                .Include(v => v.Vehiculo)
                .AsNoTracking();
            return View(await q.ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            await LoadSelects();
            return View(new VehiculoAbonado());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VehiculoAbonado model)
        {
            if (!await AbonoExiste(model.PlyID, model.PlzNum, model.AboFyhIni))
                ModelState.AddModelError("", "El abono seleccionado no existe.");

            if (!ModelState.IsValid)
            {
                await LoadSelects(model.PlyID, model.PlzNum, model.AboFyhIni, model.VehPtnt);
                return View(model);
            }

            _ctx.VehiculosAbonados.Add(model);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int plyID, int plzNum, DateTime aboFyhIni, string vehPtnt)
        {
            var item = await _ctx.VehiculosAbonados
                .Include(v => v.Abono).ThenInclude(a => a.Plaza)
                .Include(v => v.Vehiculo)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.PlyID == plyID && v.PlzNum == plzNum && v.AboFyhIni == aboFyhIni && v.VehPtnt == vehPtnt);

            return item is null ? NotFound() : View(item);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int plyID, int plzNum, DateTime aboFyhIni, string vehPtnt)
        {
            var item = await _ctx.VehiculosAbonados.FindAsync(plyID, plzNum, aboFyhIni, vehPtnt);
            if (item is null) return NotFound();

            _ctx.VehiculosAbonados.Remove(item);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
