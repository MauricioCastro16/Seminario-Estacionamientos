// File: Controllers/InformesController.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using estacionamientos.Data;
using estacionamientos.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;       // Colors
using QuestPDF.Infrastructure;
using SkiaSharp;              // Canvas (gráficos)




namespace estacionamientos.Controllers
{
    public class InformesController : Controller
    {
        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        private readonly AppDbContext _ctx;
        public InformesController(AppDbContext ctx) => _ctx = ctx;

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

        // =====================
        // INDEX (dashboard)
        // =====================
        public IActionResult Index(DateTime? desde, DateTime? hasta, List<int>? playasIds, int? duenioId = null)
        {
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

            var desdeUtc = desde.HasValue ? DayStartUtc(desde.Value) : defaultDesdeUtc;
            var hastaUtc = hasta.HasValue ? DayEndUtc(hasta.Value) : defaultHastaUtc;

            var pagos = _ctx.Pagos
                .AsNoTracking()
                .Include(p => p.Playa)
                .Include(p => p.MetodoPago)
                .Where(p => p.PagFyh >= desdeUtc && p.PagFyh <= hastaUtc);

            // Dueño actual => solo sus playas
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out var currentUserId))
            {
                pagos = pagos.Where(p => p.Playa.Administradores.Any(a => a.DueNU == currentUserId));
                filtros.DuenioId = currentUserId;
            }

            if (playasIds != null && playasIds.Count > 0)
                pagos = pagos.Where(p => playasIds.Contains(p.PlyID));

            var ingresosTotales = pagos.Sum(p => (decimal?)p.PagMonto) ?? 0m;
            var cantPagos = pagos.Count();

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

            var pagosData = pagos
                .Select(p => new { p.PagFyh, p.PagMonto })
                .ToList();

            var tzData = pagosData
                .Select(x => new { Local = x.PagFyh.ToLocalTime(), x.PagMonto });

            var ingresosPorDia = tzData
                .GroupBy(x => x.Local.Date)
                .OrderBy(g => g.Key)
                .Select(g => new SeriePuntoVM
                {
                    Label = g.Key.ToString("dd/MM"),
                    Valor = g.Sum(v => v.PagMonto)
                })
                .ToList();

            var ingresosPorHora = Enumerable.Range(0, 24)
                .GroupJoin(
                    tzData.GroupBy(x => x.Local.Hour)
                         .Select(g => new { Hour = g.Key, Valor = g.Sum(v => v.PagMonto) }),
                    h => h,
                    g => g.Hour,
                    (h, grp) => new SeriePuntoVM
                    {
                        Label = h.ToString("00"),
                        Valor = grp.Sum(x => x.Valor)
                    })
                .ToList();

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
                IngresosPorDia = ingresosPorDia,
                IngresosPorHora = ingresosPorHora
            };

            return View(vm);
        }

        // =======================
        // PDF "premium"
        // =======================
        [HttpGet]
        public IActionResult Descargar(DateTime? desde, DateTime? hasta, List<int>? playasIds)
        {
            var result = Index(desde, hasta, playasIds, null) as ViewResult;
            if (result?.Model is not InformeDuenioVM vm)
                return NotFound();

            // Logo opcional
            byte[]? logo = null;
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "logo.png");
            if (System.IO.File.Exists(logoPath))
                logo = System.IO.File.ReadAllBytes(logoPath);

            var pdfBytes = BuildInformePdf(vm, logo);
            return File(pdfBytes, "application/pdf", $"informe_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }

        // =======================
        // Detalle de pagos por playa
        // =======================
        [HttpGet("/Informes/DetallePlaya")]
        public IActionResult DetallePlaya(int plyID, DateTime? desde, DateTime? hasta)
        {
            var todayLocal = DateTime.Now;
            var desdeUtc = DayStartUtc((desde ?? todayLocal.AddDays(-30)));
            var hastaUtc = DayEndUtc((hasta ?? todayLocal));

            // dueño actual
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var currentUserId))
                return Unauthorized();

            // validar que la playa sea del dueño
            var esDelDuenio = _ctx.Playas
                .AsNoTracking()
                .Any(pl => pl.PlyID == plyID && pl.Administradores.Any(a => a.DueNU == currentUserId));
            if (!esDelDuenio) return Forbid();

            var playaNombre = _ctx.Playas.AsNoTracking()
                .Where(pl => pl.PlyID == plyID)
                .Select(pl => string.IsNullOrWhiteSpace(pl.PlyNom) ? (pl.PlyCiu + " - " + pl.PlyDir) : pl.PlyNom)
                .FirstOrDefault() ?? $"Playa #{plyID}";

            var pagos = _ctx.Pagos
                .AsNoTracking()
                .Include(p => p.MetodoPago)
                .Where(p => p.PlyID == plyID && p.PagFyh >= desdeUtc && p.PagFyh <= hastaUtc)
                .Select(p => new { p.PlyID, p.PagNum, p.PagFyh, p.PagMonto, p.MepID, Metodo = p.MetodoPago.MepNom })
                .ToList();

            var clavesPagos = pagos.Select(p => new { p.PlyID, p.PagNum }).ToList();

            var ocupacionesPorPago = _ctx.Ocupaciones
                .AsNoTracking()
                .Where(o => o.PlyID == plyID && o.PagNum != null)
                .Select(o => new { o.PlyID, o.PagNum, o.VehPtnt })
                .ToList()
                .Where(x => clavesPagos.Any(k => k.PlyID == x.PlyID && k.PagNum == x.PagNum))
                .GroupBy(x => new { x.PlyID, x.PagNum })
                .ToDictionary(
                    g => (g.Key.PlyID, g.Key.PagNum!.Value),
                    g => new { Count = g.Count(), Vehiculos = g.Select(v => v.VehPtnt).Distinct().ToList() }
                );

            var serviciosPorPago = _ctx.ServiciosExtrasRealizados
                .AsNoTracking()
                .Where(s => s.PlyID == plyID && s.PagNum != null)
                .Include(s => s.ServicioProveido)
                .ThenInclude(sp => sp.Servicio)
                .Select(s => new { s.PlyID, s.PagNum, SerNom = s.ServicioProveido.Servicio.SerNom })
                .ToList()
                .Where(x => clavesPagos.Any(k => k.PlyID == x.PlyID && k.PagNum == x.PagNum))
                .GroupBy(x => new { x.PlyID, x.PagNum })
                .ToDictionary(
                    g => (g.Key.PlyID, g.Key.PagNum!.Value),
                    g => new { Count = g.Count(), Nombres = g.Select(v => v.SerNom).ToList() }
                );

            var mepIds = pagos.Select(p => p.MepID).Distinct().ToList();
            var nombresMetodos = _ctx.AceptaMetodosPago
                .AsNoTracking()
                .Include(a => a.MetodoPago)
                .Where(a => mepIds.Contains(a.MepID) && a.MetodoPago != null)
                .GroupBy(a => new { a.MepID, a.MetodoPago!.MepNom })
                .Select(g => new { g.Key.MepID, Nombre = g.Key.MepNom })
                .ToList()
                .ToDictionary(x => x.MepID, x => x.Nombre);

            var items = pagos
                .OrderByDescending(p => p.PagFyh)
                .Select(p =>
                {
                    var key = (p.PlyID, p.PagNum);
                    var cantOcup = ocupacionesPorPago.TryGetValue(key, out var oc) ? oc.Count : 0;
                    var cantServ = serviciosPorPago.TryGetValue(key, out var se) ? se.Count : 0;

                    return new InformeDetallePlayaItemVM
                    {
                        PlyID = p.PlyID,
                        PagNum = p.PagNum,
                        FechaUtc = p.PagFyh,
                        Monto = p.PagMonto,
                        Metodo = !string.IsNullOrWhiteSpace(p.Metodo) ? p.Metodo
                                : (nombresMetodos.TryGetValue(p.MepID, out var nom) ? nom : $"Mep #{p.MepID}"),
                        OcupacionesCount = cantOcup,
                        ServiciosExtrasCount = cantServ,
                        OcupacionesVehiculos = ocupacionesPorPago.TryGetValue(key, out var oc2) ? oc2.Vehiculos : new List<string>(),
                        ServiciosExtrasNombres = serviciosPorPago.TryGetValue(key, out var se2) ? se2.Nombres : new List<string>()
                    };
                })
                .ToList();

            var vm = new InformeDetallePlayaVM
            {
                PlyID = plyID,
                PlayaNombre = playaNombre,
                Filtros = new InformeFiltroVM
                {
                    Desde = (desde ?? todayLocal.AddDays(-30)).Date,
                    Hasta = (hasta ?? todayLocal).Date,
                    PlayasIds = new List<int> { plyID },
                    DuenioId = currentUserId
                },
                Items = items
            };

            return View(vm);
        }

        // =======================
        // PDF Builder (KPIs arriba, luego gráficos, luego mix y desglose)
        // =======================

      private static byte[] BuildInformePdf(InformeDuenioVM vm, byte[]? logoBytes)
{
    return Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Margin(PdfTheme.PageMargin);

            // ===== Header =====
            page.Header().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Informe de Ingresos")
                        .FontSize(20).SemiBold().FontColor(PdfTheme.PrimaryDark);
                    col.Item().Text($"Período: {vm.Filtros.Desde:dd/MM/yyyy} — {vm.Filtros.Hasta:dd/MM/yyyy}")
                        .FontSize(10).FontColor(PdfTheme.Muted);
                });

                row.ConstantItem(80).AlignRight().AlignMiddle().Element(e =>
                {
                    if (logoBytes is not null) e.Image(logoBytes).FitWidth();
                    else e.Text(" ");
                });
            });

            page.Content().PaddingVertical(8).Column(col =>
            {
                // ===== (1) KPI CARDS =====
                col.Item().PaddingVertical(4).Row(row =>
                {
                    void KpiCard(string title, string value)
                    {
                        row.RelativeItem().PaddingRight(6).Element(card =>
                        {
                            card.Border(1).BorderColor(PdfTheme.Border).Background(Colors.White)
                                .Padding(PdfTheme.CardPadding)
                                .Column(c =>
                                {
                                    c.Item().Text(title).FontSize(9).FontColor(PdfTheme.Muted);
                                    c.Item().Text(value).FontSize(16).SemiBold().FontColor(PdfTheme.Text);
                                });
                        });
                    }

                    KpiCard("Ingresos Totales", PdfTheme.Money(vm.Kpis.IngresosTotales));
                    KpiCard("Cantidad de Pagos", vm.Kpis.CantPagos.ToString("N0"));
                    KpiCard("Ingreso promedio por pago", PdfTheme.Money(vm.Kpis.TicketPromedio));
                });

                // ===== (2) GRÁFICOS: línea (día) + barras (hora) =====
                col.Item().PaddingTop(10).Row(r =>
                {
                    // Línea por día
                    r.RelativeItem().Element(card =>
                    {
                        card.Border(1).BorderColor(PdfTheme.Border)
                            .Padding(PdfTheme.CardPadding).Column(cc =>
                            {
                                cc.Item().Text("Ingresos por día (línea)").Bold().FontColor(PdfTheme.Accent);
                                if (vm.IngresosPorDia.Any())
                                    cc.Item().Height(260).Element(e => RenderLineChart(e, vm.IngresosPorDia));
                                else
                                    cc.Item().Text("Sin datos").FontColor(PdfTheme.Muted);
                            });
                    });

                    // Barras por hora
                    r.RelativeItem().Element(card =>
                    {
                        card.Border(1).BorderColor(PdfTheme.Border)
                            .Padding(PdfTheme.CardPadding).Column(cc =>
                            {
                                cc.Item().Text("Ingresos por hora (barras)").Bold().FontColor(PdfTheme.Accent);
                                if (vm.IngresosPorHora.Any())
                                    cc.Item().Height(260).Element(e => RenderBarChart(e, vm.IngresosPorHora));
                                else
                                    cc.Item().Text("Sin datos").FontColor(PdfTheme.Muted);
                            });
                    });
                });

                // Separador bien fino y con poco margen
                col.Item().PaddingTop(8).Element(e => e.BorderBottom(0.5f).BorderColor(PdfTheme.Border));

                // ===== (3) MIX POR MÉTODO =====
                col.Item().PaddingTop(10).Element(section =>
                {
                    section.Column(c =>
                    {
                        c.Item().Text("Mix por Método de Pago")
                            .FontSize(12).Bold().FontColor(PdfTheme.PrimaryDark);

                        if (vm.Kpis.MixMetodos?.Any() == true)
                        {
                            c.Item().PaddingTop(6).Element(t =>
                            {
                                t.Table(table =>
                                {
                                    table.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn(6);
                                        cols.RelativeColumn(2);
                                        cols.RelativeColumn(2);
                                        cols.RelativeColumn(2);
                                    });

                                    table.Header(h =>
                                    {
                                        h.Cell().Element(Th).Text("Método");
                                        h.Cell().Element(ThRight).Text("Monto");
                                        h.Cell().Element(ThRight).Text("# Pagos");
                                        h.Cell().Element(ThRight).Text("% Total");
                                    });

                                    var zebra = false;
                                    foreach (var m in vm.Kpis.MixMetodos.OrderByDescending(x => x.Monto))
                                    {
                                        zebra = !zebra;
                                        table.Cell().Element(r => Td(r, zebra)).Text(m.Metodo);
                                        table.Cell().Element(r => TdRight(r, zebra)).Text(PdfTheme.Money(m.Monto));
                                        table.Cell().Element(r => TdRight(r, zebra)).Text(m.Cantidad.ToString("N0"));
                                        table.Cell().Element(r => TdRight(r, zebra)).Text($"{m.PorcentajeMonto:0.0}%");
                                    }
                                });
                            });
                        }
                        else
                        {
                            c.Item().PaddingTop(4).Text("Sin datos en el período.").FontColor(PdfTheme.Muted);
                        }
                    });
                });

                // ===== (4) DESGLOSE POR PLAYA =====
                col.Item().PaddingTop(14).Element(section =>
                {
                    section.Column(c =>
                    {
                        c.Item().Text("Desglose por Playa")
                            .FontSize(12).Bold().FontColor(PdfTheme.PrimaryDark);

                        if (vm.PorPlaya?.Any() == true)
                        {
                            c.Item().PaddingTop(6).Element(t =>
                            {
                                t.Table(table =>
                                {
                                    table.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn(6);
                                        cols.RelativeColumn(2);
                                        cols.RelativeColumn(2);
                                        cols.RelativeColumn(2);
                                    });

                                    table.Header(h =>
                                    {
                                        h.Cell().Element(Th).Text("Playa");
                                        h.Cell().Element(ThRight).Text("# Pagos");
                                        h.Cell().Element(ThRight).Text("Ingresos");
                                        h.Cell().Element(ThRight).Text("Ingreso prom. por pago");
                                    });

                                    var zebra = false;
                                    foreach (var r in vm.PorPlaya.OrderByDescending(x => x.IngresosTotales))
                                    {
                                        zebra = !zebra;
                                        table.Cell().Element(rr => Td(rr, zebra)).Text(r.PlayaNombre);
                                        table.Cell().Element(rr => TdRight(rr, zebra)).Text(r.CantPagos.ToString("N0"));
                                        table.Cell().Element(rr => TdRight(rr, zebra)).Text(PdfTheme.Money(r.IngresosTotales));
                                        table.Cell().Element(rr => TdRight(rr, zebra)).Text(PdfTheme.Money(r.TicketPromedio));
                                    }
                                });
                            });
                        }
                        else
                        {
                            c.Item().PaddingTop(4).Text("Sin pagos en el período.").FontColor(PdfTheme.Muted);
                        }
                    });
                });
            });

            // ===== Footer =====
            page.Footer()
                .DefaultTextStyle(s => s.FontSize(9).FontColor(PdfTheme.Muted))
                .Row(r =>
                {
                    r.RelativeItem().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}");
                    r.ConstantItem(100).AlignRight().Text(t =>
                    {
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });
                });
        });
    }).GeneratePdf();
}


        // ===== Helpers: gráficos con Canvas (SkiaSharp) =====
        // ====== LÍNEA: Ingresos por día ======
        private static void RenderLineChart(IContainer container, List<SeriePuntoVM> series)
        {
            if (series == null || series.Count == 0 || series.All(s => s.Valor <= 0))
            {
                container.Text("Sin datos").FontColor(PdfTheme.Muted);
                return;
            }

            const int W = 600, H = 260;
            const int padL = 44, padR = 12, padT = 10, padB = 30;
            var plotW = W - padL - padR;
            var plotH = H - padT - padB;

            var maxRaw = series.Max(s => s.Valor);
            var max = Math.Max(1m, maxRaw * 1.10m); // 10% headroom

            int n = series.Count;
            var step = n > 1 ? (decimal)plotW / (n - 1) : plotW;

            (float x, float y) Pt(int i)
            {
                var x = padL + (float)(step * i);
                var y = padT + plotH - (float)(series[i].Valor / max) * plotH;
                return (x, y);
            }

            var pts = Enumerable.Range(0, n).Select(Pt).ToList();

            string LinePath()
            {
                if (n == 1) return $"M {F(pts[0].x)},{F(pts[0].y)}";
                var d = $"M {F(pts[0].x)},{F(pts[0].y)}";
                for (int i = 1; i < n; i++) d += $" L {F(pts[i].x)},{F(pts[i].y)}";
                return d;
            }

            string AreaPath()
            {
                var d = LinePath();
                d += $" L {F(padL + plotW)},{F(padT + plotH)} L {F(padL)},{F(padT + plotH)} Z";
                return d;
            }

            // grid y ejes
            int gridRows = 5;
            var grid = string.Join("", Enumerable.Range(0, gridRows + 1).Select(i =>
            {
                var y = padT + (float)plotH / gridRows * i;
                return $"<line x1='{F(padL)}' y1='{F(y)}' x2='{F(padL + plotW)}' y2='{F(y)}' stroke='#cbd5e1' stroke-opacity='0.5' stroke-width='1' />";
            }));

            var axes =
              $"<line x1='{F(padL)}' y1='{F(padT)}' x2='{F(padL)}' y2='{F(padT + plotH)}' stroke='#64748b' stroke-width='1'/>" +
              $"<line x1='{F(padL)}' y1='{F(padT + plotH)}' x2='{F(padL + plotW)}' y2='{F(padT + plotH)}' stroke='#64748b' stroke-width='1'/>";

            // puntos (downsample light)
            int k = Math.Max(1, n / 12);
            var dots = string.Join("", Enumerable.Range(0, n).Where(i => i % k == 0 || i == n - 1)
                .Select(i => $"<circle cx='{F(pts[i].x)}' cy='{F(pts[i].y)}' r='3' fill='#2563EB' />"));

            // leyenda
            var legend =
              $"<g transform='translate({F(padL)},{F(padT - 2)})'>" +
              $"  <rect x='0' y='0' width='10' height='10' fill='#3B82F6' opacity='0.9' />" +
              $"  <text x='14' y='9' font-size='10' fill='#1f2937'>Ingresos ($)</text>" +
              $"</g>";

            var svg = $@"
<svg viewBox='0 0 {W} {H}' xmlns='http://www.w3.org/2000/svg'>
  <rect x='0' y='0' width='{W}' height='{H}' fill='white'/>
  {legend}
  {grid}
  {axes}
  <path d='{AreaPath()}' fill='#93C5FD' fill-opacity='0.45' stroke='none'/>
  <path d='{LinePath()}' fill='none' stroke='#3B82F6' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'/>
  {dots}
</svg>";
            container.Svg(svg);
        }

// ====== BARRAS: Ingresos por hora ======
private static void RenderBarChart(IContainer container, List<SeriePuntoVM> series)
{
    if (series == null || series.Count == 0 || series.All(s => s.Valor <= 0))
    {
        container.Text("Sin datos").FontColor(PdfTheme.Muted);
        return;
    }

    const int W = 600, H = 260;
    const int padL = 40, padR = 12, padT = 10, padB = 30;
    var plotW = W - padL - padR;
    var plotH = H - padT - padB;

    var maxRaw = series.Max(s => s.Valor);
    var max = Math.Max(1m, maxRaw * 1.10m); // 10% headroom

    int n = series.Count;
    float gap = 2f;
    float barW = Math.Max(2f, (plotW - gap * (n - 1)) / n);

    int gridRows = 5;
    var grid = string.Join("", Enumerable.Range(0, gridRows + 1).Select(i =>
    {
        var y = padT + (float)plotH / gridRows * i;
        return $"<line x1='{F(padL)}' y1='{F(y)}' x2='{F(padL + plotW)}' y2='{F(y)}' stroke='#cbd5e1' stroke-opacity='0.5' stroke-width='1' />";
    }));

    var axes =
      $"<line x1='{F(padL)}' y1='{F(padT)}' x2='{F(padL)}' y2='{F(padT + plotH)}' stroke='#64748b' stroke-width='1'/>" +
      $"<line x1='{F(padL)}' y1='{F(padT + plotH)}' x2='{F(padL + plotW)}' y2='{F(padT + plotH)}' stroke='#64748b' stroke-width='1'/>";

    var bars = string.Join("", series.Select((s, i) =>
    {
        var x = padL + i * (barW + gap);
        var h = (float)(s.Valor / max) * plotH;
        var y = padT + plotH - h;
        return $"<rect x='{F(x)}' y='{F(y)}' width='{F(barW)}' height='{F(h)}' fill='#93C5FD' />";
    }));

    // leyenda
    var legend =
      $"<g transform='translate({F(padL)},{F(padT - 2)})'>" +
      $"  <rect x='0' y='0' width='10' height='10' fill='#93C5FD' opacity='0.9' />" +
      $"  <text x='14' y='9' font-size='10' fill='#1f2937'>Ingresos ($)</text>" +
      $"</g>";

    var svg = $@"
<svg viewBox='0 0 {W} {H}' xmlns='http://www.w3.org/2000/svg'>
  <rect x='0' y='0' width='{W}' height='{H}' fill='white'/>
  {legend}
  {grid}
  {axes}
  {bars}
</svg>";
    container.Svg(svg);
}
        // =======================
        // Estilos PDF (theme + helpers)
        // =======================
        private static class PdfTheme
        {
            public static string Primary = Colors.Blue.Medium;
            public static string PrimaryDark = Colors.Blue.Darken3;
            public static string Accent = Colors.Indigo.Medium;
            public static string Text = Colors.Grey.Darken3;
            public static string Muted = Colors.Grey.Darken1;
            public static string Border = Colors.Grey.Lighten3;
            public static string Zebra = Colors.Grey.Lighten4;
            public static string Badge = Colors.Blue.Lighten4;

            public const float PageMargin = 30;
            public const float CardPadding = 12;

            public static string Money(decimal v) =>
                v.ToString("C2", new CultureInfo("es-AR"));
        }

        private static IContainer Th(IContainer c) =>
            c.PaddingVertical(6).PaddingHorizontal(8)
             .Background(PdfTheme.Badge).BorderBottom(1).BorderColor(PdfTheme.Border)
             .DefaultTextStyle(x => x.SemiBold().FontColor(PdfTheme.PrimaryDark).FontSize(10));

        private static IContainer ThRight(IContainer c) => Th(c).AlignRight();

        private static IContainer Td(IContainer c, bool zebra) =>
            c.Background(zebra ? PdfTheme.Zebra : Colors.White)
             .PaddingVertical(5).PaddingHorizontal(8)
             .DefaultTextStyle(x => x.FontColor(PdfTheme.Text).FontSize(10));

        private static IContainer TdRight(IContainer c, bool zebra) => Td(c, zebra).AlignRight();
    }
}
