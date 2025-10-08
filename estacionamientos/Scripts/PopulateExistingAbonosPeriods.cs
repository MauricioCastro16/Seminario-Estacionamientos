using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.Models;

namespace estacionamientos.Scripts
{
    public class PopulateExistingAbonosPeriods
    {
        private readonly AppDbContext _context;

        public PopulateExistingAbonosPeriods(AppDbContext context)
        {
            _context = context;
        }

        public async Task PopulatePeriodsForExistingAbonos()
        {
            // Obtener todos los abonos que no tienen períodos
            var abonosSinPeriodos = await _context.Abonos
                .Include(a => a.Periodos)
                .Where(a => !a.Periodos.Any())
                .ToListAsync();

            Console.WriteLine($"Encontrados {abonosSinPeriodos.Count} abonos sin períodos");

            foreach (var abono in abonosSinPeriodos)
            {
                try
                {
                    // Obtener información del servicio (asumir abono por 1 día si no se puede determinar)
                    var servicio = await _context.Servicios
                        .FirstOrDefaultAsync(s => s.SerNom.Contains("Abono"));

                    int diasPorPeriodo = 1; // Default
                    if (servicio?.SerDuracionMinutos != null)
                    {
                        diasPorPeriodo = (int)Math.Ceiling(servicio.SerDuracionMinutos.Value / 1440m);
                    }

                    // Obtener tarifa del abono
                    var tarifa = await _context.TarifasServicio
                        .Where(t => t.PlyID == abono.PlyID)
                        .OrderByDescending(t => t.TasFecIni)
                        .FirstOrDefaultAsync();

                    var montoPorPeriodo = tarifa?.TasMonto ?? (abono.AboMonto / 30); // Estimación si no hay tarifa

                    // Calcular cuántos períodos tiene el abono basándose en la duración
                    var duracionTotal = (abono.AboFyhFin - abono.AboFyhIni)?.TotalDays ?? 1;
                    var periodosTotales = Math.Max(1, (int)Math.Ceiling(duracionTotal / diasPorPeriodo));

                    // Crear períodos para el abono
                    for (int i = 1; i <= periodosTotales; i++)
                    {
                        var fechaInicioPeriodo = abono.AboFyhIni.AddDays((i - 1) * diasPorPeriodo);
                        var fechaFinPeriodo = fechaInicioPeriodo.AddDays(diasPorPeriodo);

                        // Marcar solo el primer período como pagado si el abono está activo
                        bool periodoPagado = abono.EstadoPago == EstadoPago.Activo && i == 1;

                        var periodo = new PeriodoAbono
                        {
                            PlyID = abono.PlyID,
                            PlzNum = abono.PlzNum,
                            AboFyhIni = abono.AboFyhIni,
                            PeriodoNumero = i,
                            PeriodoFechaInicio = fechaInicioPeriodo,
                            PeriodoFechaFin = fechaFinPeriodo,
                            PeriodoMonto = montoPorPeriodo,
                            PeriodoPagado = periodoPagado,
                            PeriodoFechaPago = periodoPagado ? abono.AboFyhIni : null
                        };

                        _context.PeriodosAbono.Add(periodo);
                    }

                    Console.WriteLine($"Creados {periodosTotales} períodos para abono {abono.PlyID}-{abono.PlzNum}-{abono.AboFyhIni:yyyy-MM-dd}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error procesando abono {abono.PlyID}-{abono.PlzNum}-{abono.AboFyhIni}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
            Console.WriteLine("Períodos creados exitosamente para abonos existentes");
        }
    }
}


