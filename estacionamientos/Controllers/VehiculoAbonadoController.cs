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
        public async Task<IActionResult> VerificarPatente(string patente, int? plyID = null)
        {
            if (string.IsNullOrWhiteSpace(patente))
                return Json(new { success = false });

            var normalized = patente.Trim().ToUpper();

            // Construir la consulta base
            var query = _ctx.VehiculosAbonados
                .Include(v => v.Abono)
                    .ThenInclude(a => a.Abonado)
                .Include(v => v.Vehiculo)
                .AsNoTracking()
                .Where(v => v.VehPtnt.ToUpper() == normalized);

            // Si se proporciona plyID, filtrar por playa para solo buscar abonos de esa playa
            if (plyID.HasValue)
            {
                query = query.Where(v => v.PlyID == plyID.Value);
            }

            var vehiculoAbonado = await query.FirstOrDefaultAsync();

            if (vehiculoAbonado == null || vehiculoAbonado.Abono == null)
                return Json(new { success = false });

            // Verificar estado del abono
            var fechaActual = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            var fechaActualDate = fechaActual.Date;
            
            if (vehiculoAbonado.Abono.EstadoPago != EstadoPago.Activo &&
                vehiculoAbonado.Abono.EstadoPago != EstadoPago.Pendiente)
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

            // Verificar si el abono ya comenzó
            bool abonoYaComenzo = fechaActualDate >= vehiculoAbonado.Abono.AboFyhIni.Date;
            bool abonoFinalizado = vehiculoAbonado.Abono.AboFyhFin.HasValue && 
                                   fechaActualDate > vehiculoAbonado.Abono.AboFyhFin.Value.Date;

            // Si el abono finalizó, no mostrar información
            if (abonoFinalizado)
                return Json(new { success = false });

            // Si el abono es programado (aún no comenzó), devolver información para autocompletar
            if (!abonoYaComenzo)
            {
                // Para abonos programados, usar la fecha UTC directamente (sin convertir a local)
                // para evitar que cambie el día por diferencias de zona horaria
                // La fecha es conceptual (el día 7), no un momento específico
                var fechaInicioUtc = vehiculoAbonado.Abono.AboFyhIni.Kind == DateTimeKind.Utc 
                    ? vehiculoAbonado.Abono.AboFyhIni 
                    : DateTime.SpecifyKind(vehiculoAbonado.Abono.AboFyhIni, DateTimeKind.Utc);
                var fechaInicioFormateada = fechaInicioUtc.ToString("dd/MM/yyyy");
                
                return Json(new
                {
                    success = true,
                    esAbonadoProgramado = true,
                    message = $"La patente {vehiculoAbonado.VehPtnt} pertenece al abonado {abonado}. Su abono comienza el {fechaInicioFormateada}. En esta estancia se le cobrará por hora.",
                    clasVehID = clasificacionId,
                    clasificacionNombre = vehiculoAbonado.Vehiculo?.Clasificacion?.ClasVehTipo ?? "(sin tipo)",
                    techada,
                    piso,
                    plaza = plazaNum,
                    esAbonado = false, // No es abonado aún porque no comenzó
                    fechaInicioAbono = fechaInicioFormateada,
                    plazaAbonoID = vehiculoAbonado.Abono.PlyID,
                    plazaAbonoNum = vehiculoAbonado.Abono.PlzNum
                });
            }

            var existeOtroVehiculo = await _ctx.VehiculosAbonados
                .AsNoTracking()
                .AnyAsync(v => v.Abono.PlyID == vehiculoAbonado.Abono.PlyID && v.Abono.PlzNum == vehiculoAbonado.Abono.PlzNum && v.VehPtnt != vehiculoAbonado.VehPtnt);

            // Verificar si la plaza del abono está ocupada actualmente
            var plazaOcupada = await _ctx.Ocupaciones
                .AsNoTracking()
                .AnyAsync(o => o.PlyID == vehiculoAbonado.Abono.PlyID &&
                              o.PlzNum == vehiculoAbonado.Abono.PlzNum &&
                              o.OcufFyhFin == null);

            return Json(new
            {
                success = true,
                esAbonadoProgramado = false,
                message = $"La patente {vehiculoAbonado.VehPtnt} pertenece al abonado {abonado}, plaza {plazaNombre}.",
                clasVehID = clasificacionId,
                clasificacionNombre = vehiculoAbonado.Vehiculo?.Clasificacion?.ClasVehTipo ?? "(sin tipo)",
                techada,
                piso,
                plaza = plazaNum,
                esAbonado = true,
                existeOtroVehiculo,
                plazaOcupada = plazaOcupada,
                plazaAbonoID = vehiculoAbonado.Abono.PlyID,
                plazaAbonoNum = vehiculoAbonado.Abono.PlzNum
            });
        }
    }
}
