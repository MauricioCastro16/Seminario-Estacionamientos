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

        [HttpGet]
        public async Task<IActionResult> VerificarPatente(string patente)
        {
            if (string.IsNullOrWhiteSpace(patente))
                return Json(new { success = false });

            var normalized = patente.Trim().ToUpper();

            var vehiculoAbonado = await _ctx.VehiculosAbonados
                .Include(v => v.Abono)
                    .ThenInclude(a => a.Abonado)
                .Include(v => v.Vehiculo)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.VehPtnt.ToUpper() == normalized);

            if (vehiculoAbonado?.Abono == null || vehiculoAbonado.Abono.EstadoPago != EstadoPago.Activo)
                return Json(new { success = false });

            if (vehiculoAbonado == null)
                return Json(new { success = false });

            var abonado = vehiculoAbonado.Abono?.Abonado?.AboNom ?? "(sin nombre)";
            var clasificacionId = vehiculoAbonado.Vehiculo?.ClasVehID ?? 0;

            // Buscar la plaza asociada en PlazaEstacionamiento
            var plaza = await _ctx.Plazas
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.PlyID == vehiculoAbonado.Abono.PlyID &&
                    p.PlzNum == vehiculoAbonado.Abono.PlzNum);

            bool techada = plaza?.PlzTecho ?? false;
            var piso = plaza?.Piso ?? 0;
            var plazaNum = plaza?.PlzNum ?? 0;
            var plazaNombre = plaza?.PlzNombre ?? "(sin nombre)";

            var existeOtroVehiculo = await _ctx.VehiculosAbonados
                .AsNoTracking()
                .AnyAsync(v => v.Abono.PlyID == vehiculoAbonado.Abono.PlyID && v.Abono.PlzNum == vehiculoAbonado.Abono.PlzNum && v.VehPtnt != vehiculoAbonado.VehPtnt);


            return Json(new
            {
                success = true,
                message = $"La patente {vehiculoAbonado.VehPtnt} pertenece al abonado {abonado}, plaza {plazaNombre}.",
                clasVehID = clasificacionId,
                clasificacionNombre = vehiculoAbonado.Vehiculo?.Clasificacion?.ClasVehTipo ?? "(sin tipo)",
                techada,
                piso,
                plaza = plazaNum,
                esAbonado = true,
                existeOtroVehiculo
            });
        }
    }
}
