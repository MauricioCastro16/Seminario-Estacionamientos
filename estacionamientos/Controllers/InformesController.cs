using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.ViewModels;

namespace estacionamientos.Controllers
{
    public class InformesController : Controller
    {
        // =====================
        // Helpers fechas (UTC)
        // =====================
        static DateTime DayStartUtc(DateTime dtLocalOrUnspec)
        {
            var local = DateTime.SpecifyKind(dtLocalOrUnspec, DateTimeKind.Local);
            var startLocal = new DateTime(local.Year, local.Month, local.Day, 0, 0, 0, DateTimeKind.Local);
            return startLocal.ToUniversalTime();
        }

        static DateTime DayEndUtc(DateTime dtLocalOrUnspec)
        {
            var local = DateTime.SpecifyKind(dtLocalOrUnspec, DateTimeKind.Local);
            var endLocal = new DateTime(local.Year, local.Month, local.Day, 23, 59, 59, 999, DateTimeKind.Local);
            return endLocal.ToUniversalTime();
        }

        private readonly AppDbContext _ctx;
        public InformesController(AppDbContext ctx) => _ctx = ctx;

        // GET: /Informes
        public IActionResult Index(DateTime? desde, DateTime? hasta, List<int>? playasIds, int? duenioId = null)
        {
            // Defaults (últimos 30 días, interpretando parámetros como hora local del server)
            var todayLocal = DateTime.Now;
            var defaultDesdeUtc = DayStartUtc(todayLocal.AddDays(-30));
            var defaultHastaUtc = DayEndUtc(todayLocal);

            var filtros = new InformeFiltroVM
            {
                Desde = (desde ?? todayLocal.AddDays(-30)).Date,
                Hasta = (hasta ?? todayLocal).Date,
                PlayasIds = playasIds,
                DuenioId = duenioId
            };

            // Rango real en UTC para Postgres (timestamptz)
            var desdeUtc = desde.HasValue ? DayStartUtc(desde.Value) : defaultDesdeUtc;
            var hastaUtc = hasta.HasValue ? DayEndUtc(hasta.Value) : defaultHastaUtc;

            // Base de datos: pagos en rango + joins necesarios
            var pagos = _ctx.Pagos
                .AsNoTracking()
                .Include(p => p.Playa)
                .Include(p => p.MetodoPago)
                .Where(p => p.PagFyh >= desdeUtc && p.PagFyh <= hastaUtc);

            // Filtro por Playas
            if (playasIds != null && playasIds.Count > 0)
                pagos = pagos.Where(p => playasIds.Contains(p.PlyID));

            // (Opcional) Filtro por Dueño si Playa referencia a Dueño (descomentar y ajustar propiedad real)
            // if (duenioId.HasValue)
            //     pagos = pagos.Where(p => p.Playa.DuenioId == duenioId.Value);
            //     // o p.Playa.Duenio.UsuNU == duenioId.Value;

            // =====================
            // KPIs
            // =====================
            var ingresosTotales = pagos.Sum(p => (decimal?)p.PagMonto) ?? 0m;
            var cantPagos = pagos.Count();

            // =====================
            // Mix por método de pago
            // (SQL: agrupo por MepID) -> (Memoria: resuelvo nombres)
            // =====================
            var mixRaw = pagos
                .GroupBy(p => p.MepID)
                .Select(g => new
                {
                    MepID = g.Key,
                    Monto = g.Sum(x => x.PagMonto),
                    Cantidad = g.Count()
                })
                .ToList();

            var mepIds = mixRaw.Select(x => x.MepID).Distinct().ToList();

            // Resolución de nombres SIN depender de un DbSet<MetodoPago> concreto:
            // Nos apoyamos en AceptaMetodosPago -> MetodoPago (nav) para obtener MepNom.
            var nombresMetodos = _ctx.AceptaMetodosPago
                .AsNoTracking()
                .Include(a => a.MetodoPago)
                .Where(a => mepIds.Contains(a.MepID) && a.MetodoPago != null)
                .GroupBy(a => new { a.MepID, a.MetodoPago!.MepNom })
                .Select(g => new { g.Key.MepID, Nombre = g.Key.MepNom })
                .ToList()
                .ToDictionary(x => x.MepID, x => x.Nombre);

            var mixMetodos = mixRaw
                .Select(x => new MetodoPagoMixVM
                {
                    MepID = x.MepID,
                    Metodo = nombresMetodos.TryGetValue(x.MepID, out var nom) ? nom : $"Mep #{x.MepID}",
                    Monto = x.Monto,
                    Cantidad = x.Cantidad
                })
                .OrderByDescending(x => x.Monto)
                .ToList();

            foreach (var m in mixMetodos)
                m.PorcentajeMonto = ingresosTotales > 0 ? (m.Monto / ingresosTotales * 100m) : 0m;

            // =====================
            // Desglose por Playa
            // (SQL: agrupo por PlyID) -> (Memoria: resuelvo nombre de playa)
            // =====================
            var porPlayaRaw = pagos
                .GroupBy(p => p.PlyID)
                .Select(g => new
                {
                    PlyID = g.Key,
                    IngresosTotales = g.Sum(x => x.PagMonto),
                    CantPagos = g.Count()
                })
                .OrderByDescending(x => x.IngresosTotales)
                .ToList();

            var plyIds = porPlayaRaw.Select(x => x.PlyID).Distinct().ToList();

            var nombresPlayas = _ctx.Playas
                .AsNoTracking()
                .Where(pl => plyIds.Contains(pl.PlyID))
                .Select(pl => new
                {
                    pl.PlyID,
                    Nombre = string.IsNullOrWhiteSpace(pl.PlyNom) ? (pl.PlyCiu + " - " + pl.PlyDir) : pl.PlyNom
                })
                .ToList()
                .ToDictionary(pl => pl.PlyID, pl => pl.Nombre);

            var porPlaya = porPlayaRaw
                .Select(x => new InformePlayaRowVM
                {
                    PlyID = x.PlyID,
                    PlayaNombre = nombresPlayas.TryGetValue(x.PlyID, out var nom) ? nom : $"Playa #{x.PlyID}",
                    IngresosTotales = x.IngresosTotales,
                    CantPagos = x.CantPagos
                })
                .ToList();

            // =====================
            // Armo VM final
            // =====================
            var vm = new InformeDuenioVM
            {
                Filtros = filtros,
                Kpis = new InformeKpisVM
                {
                    IngresosTotales = ingresosTotales,
                    CantPagos = cantPagos,
                    MixMetodos = mixMetodos
                },
                PorPlaya = porPlaya
            };

            return View(vm);
        }
    }
}
