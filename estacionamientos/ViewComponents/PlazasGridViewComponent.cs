using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.ViewComponents
{
    public class PlazasGridViewComponent(AppDbContext context) : ViewComponent
    {
        private readonly AppDbContext _context = context;

        public async Task<IViewComponentResult> InvokeAsync(int plyID)
        {
            // Buscar todas las plazas de la playa
            var plazas = await _context.Plazas
                .Include(p => p.Ocupaciones)
                .Include(p => p.Clasificaciones)
                    .ThenInclude(c => c.Clasificacion)
                .Where(p => p.PlyID == plyID)
                .OrderBy(p => p.Piso)
                .ThenBy(p => p.PlzNum)
                .ToListAsync();

            // Obtener fecha actual (UTC, truncada a día)
            var fechaActual = DateTime.UtcNow.Date;

            // Buscar todas las plazas con abono activo en esta playa
            var abonosActivos = await _context.Abonos
                .Where(a => a.PlyID == plyID
                    && a.EstadoPago != EstadoPago.Cancelado
                    && a.EstadoPago != EstadoPago.Finalizado
                    && a.AboFyhIni.Date <= fechaActual
                    && (a.AboFyhFin == null || a.AboFyhFin.Value.Date >= fechaActual))
                .Select(a => a.PlzNum)
                .ToListAsync();

            var abonosSet = new HashSet<int>(abonosActivos);

            // Mapear las plazas al viewmodel
            var modelo = plazas.Select(p => new PlazaViewModel
            {
                PlyID = p.PlyID,
                PlzNum = p.PlzNum,
                PlzNombre = p.PlzNombre,
                PlzHab = p.PlzHab,
                PlzOcupada = p.Ocupaciones.Any(o => o.OcufFyhFin == null),
                VehPtnt = p.Ocupaciones.FirstOrDefault(o => o.OcufFyhFin == null)?.VehPtnt,
                Clasificaciones = p.Clasificaciones.ToList(),
                Piso = p.Piso,
                TieneAbonoActivo = abonosSet.Contains(p.PlzNum),
                Techada = p.PlzTecho
            }).ToList();

            return View(modelo);
        }
    }
}
