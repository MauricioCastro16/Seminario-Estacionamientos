using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;


// Para PDF con QuestPDF (agregá el paquete QuestPDF desde NuGet)
// using QuestPDF.Fluent;
// using QuestPDF.Helpers;
// using QuestPDF.Infrastructure;

namespace estacionamientos.Controllers
{
    public class InformesController : Controller
    {
        // ===== Helpers fechas (UTC) =====
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
            // Defaults (últimos 30 días, params interpretados como hora local)
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

            // Rango en UTC para timestamptz
            var desdeUtc = desde.HasValue ? DayStartUtc(desde.Value) : defaultDesdeUtc;
            var hastaUtc = hasta.HasValue ? DayEndUtc(hasta.Value) : defaultHastaUtc;

            // Base: pagos en rango
            var pagos = _ctx.Pagos
                .AsNoTracking()
                .Include(p => p.Playa)
                .Include(p => p.MetodoPago)
                .Where(p => p.PagFyh >= desdeUtc && p.PagFyh <= hastaUtc);

            // ===== FILTRO: Dueño logueado (solo sus playas) =====
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out var currentUserId))
            {
                // Cambiaste a DueNU, perfecto:
                pagos = pagos.Where(p => p.Playa.Administradores.Any(a => a.DueNU == currentUserId));
                filtros.DuenioId = currentUserId;
            }

            // Filtro adicional por lista de playas (dentro del set del dueño)
            if (playasIds != null && playasIds.Count > 0)
                pagos = pagos.Where(p => playasIds.Contains(p.PlyID));

            // ===== KPIs =====
            var ingresosTotales = pagos.Sum(p => (decimal?)p.PagMonto) ?? 0m;
            var cantPagos = pagos.Count();

            // ===== Mix por método (agrupo por ID en SQL, nombres en memoria) =====
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

            // ===== Desglose por Playa (agrupo por ID y resuelvo nombre en memoria) =====
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

            // ===== NUEVO: Series de ingresos por día / hora =====
            var pagosData = pagos
                .Select(p => new { p.PagFyh, p.PagMonto })
                .ToList(); // Cargamos a memoria para usar ToLocalTime sin problemas de traducción EF

            var tzData = pagosData
                .Select(x => new { Local = x.PagFyh.ToLocalTime(), x.PagMonto });

            // Serie por día (dd/MM)
            var ingresosPorDia = tzData
                .GroupBy(x => x.Local.Date)
                .OrderBy(g => g.Key)
                .Select(g => new SeriePuntoVM
                {
                    Label = g.Key.ToString("dd/MM"),
                    Valor = g.Sum(v => v.PagMonto)
                })
                .ToList();

            // Serie por hora (0..23) sumando todo el período
            var ingresosPorHora = Enumerable.Range(0, 24)
                .GroupJoin(
                    tzData.GroupBy(x => x.Local.Hour)
                         .Select(g => new { Hour = g.Key, Valor = g.Sum(v => v.PagMonto) }),
                    h => h,
                    g => g.Hour,
                    (h, grp) => new SeriePuntoVM
                    {
                        Label = h.ToString("00"),
                        Valor = grp.Sum(x => x.Valor) // 0 si no hay
                    })
                .ToList();

            // ===== VM final =====
            var vm = new InformeDuenioVM
            {
                Filtros = filtros,
                Kpis = new InformeKpisVM
                {
                    IngresosTotales = ingresosTotales,
                    CantPagos = cantPagos,
                    MixMetodos = mixMetodos
                },
                PorPlaya = porPlaya,
                IngresosPorDia = ingresosPorDia,    // NEW
                IngresosPorHora = ingresosPorHora   // NEW
            };

            return View(vm);
        }

        // =======================
        // DESCARGAR PDF (preparado)
        // GET: /Informes/Descargar?desde=...&hasta=... (mismos filtros que Index)
        // =======================
       public IActionResult Descargar(DateTime? desde, DateTime? hasta, List<int>? playasIds)
{
    // Reutilizamos la lógica de Index para obtener el mismo VM:
    var result = Index(desde, hasta, playasIds, null) as ViewResult;
    if (result?.Model is not InformeDuenioVM vm)
        return NotFound();

    var bytes = Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Margin(30);

            page.Header().Text($"Informe del {vm.Filtros.Desde:dd/MM/yyyy} al {vm.Filtros.Hasta:dd/MM/yyyy}")
                .SemiBold().FontSize(16);

            page.Content().PaddingVertical(10).Column(col =>
            {
                // KPIs
                col.Item().Text($"Ingresos Totales: $ {vm.Kpis.IngresosTotales:N2}");
                col.Item().Text($"Cantidad de Pagos: {vm.Kpis.CantPagos}");
                col.Item().Text($"Ticket Promedio: $ {vm.Kpis.TicketPromedio:N2}");

                // Mix por método
                col.Item().PaddingTop(10).Text("Mix por Método de Pago").SemiBold();
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(6);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                    });
                    t.Header(h =>
                    {
                        h.Cell().Text("Método");
                        h.Cell().AlignRight().Text("Monto");
                        h.Cell().AlignRight().Text("#");
                        h.Cell().AlignRight().Text("%");
                    });
                    foreach (var m in vm.Kpis.MixMetodos)
                    {
                        t.Cell().Text(m.Metodo);
                        t.Cell().AlignRight().Text($"$ {m.Monto:N2}");
                        t.Cell().AlignRight().Text($"{m.Cantidad}");
                        t.Cell().AlignRight().Text($"{m.PorcentajeMonto:0.0}%");
                    }
                });

                // Desglose por playa
                col.Item().PaddingTop(10).Text("Desglose por Playa").SemiBold();
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(6);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                    });
                    t.Header(h =>
                    {
                        h.Cell().Text("Playa");
                        h.Cell().AlignRight().Text("# Pagos");
                        h.Cell().AlignRight().Text("Ingresos");
                    });
                    foreach (var r in vm.PorPlaya)
                    {
                        t.Cell().Text(r.PlayaNombre);
                        t.Cell().AlignRight().Text($"{r.CantPagos}");
                        t.Cell().AlignRight().Text($"$ {r.IngresosTotales:N2}");
                    }
                });

                // Series (en texto por ahora)
                col.Item().PaddingTop(10).Text("Series").SemiBold();
                col.Item().Text($"Ingresos por Día: {string.Join(", ", vm.IngresosPorDia.Select(x => x.Label + "=$" + x.Valor.ToString("N0")))}");
                col.Item().Text($"Ingresos por Hora: {string.Join(", ", vm.IngresosPorHora.Select(x => x.Label + "=$" + x.Valor.ToString("N0")))}");
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Generado por el sistema de informes");
                x.Span(" • ");
                x.Span($"{DateTime.Now:dd/MM/yyyy HH:mm}");
            });
        });
    }).GeneratePdf();

    return File(bytes, "application/pdf", $"informe_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
}

    }
}
