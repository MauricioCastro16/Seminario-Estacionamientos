using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Controllers
{
    public class VehiculoAbonadoController : Controller
    {
        private readonly AppDbContext _ctx;
        public VehiculoAbonadoController(AppDbContext ctx) => _ctx = ctx;

        public async Task<IActionResult> Index()
        {
            var q = _ctx.VehiculosAbonados
                .Include(v => v.Abono).ThenInclude(a => a.Plaza)
                .Include(v => v.Vehiculo)
                .AsNoTracking();
            return View(await q.ToListAsync());
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
