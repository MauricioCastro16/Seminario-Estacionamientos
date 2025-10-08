// File: Controllers/InformesController.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
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
        public InformesController(AppDbContext ctx) 
        {
            _ctx = ctx;
            // Configurar licencia de QuestPDF
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        }
        private static readonly HttpClient Http = new HttpClient();

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
        // INDEX UNIFICADO (dashboard completo)
        // =====================
        public IActionResult Index(DateTime? desde, DateTime? hasta, List<int>? playasIds, int? duenioId = null, int? metodoPagoId = null, List<int>? metodosIds = null)
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

            // Dueño actual
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var currentUserId))
                return Unauthorized();

            // Obtener playas disponibles del dueño
            var playasDisponibles = _ctx.Playas
                .AsNoTracking()
                .Where(pl => pl.Administradores.Any(a => a.DueNU == currentUserId))
                .Select(pl => new InformePlayaRowVM
                {
                    PlyID = pl.PlyID,
                    PlayaNombre = string.IsNullOrWhiteSpace(pl.PlyNom) ? (pl.PlyCiu + " - " + pl.PlyDir) : pl.PlyNom,
                    IngresosTotales = 0, // Se calculará después
                    CantPagos = 0
                })
                .ToList();

            // Si no se especificaron playas, usar todas las del dueño
            var playasSeleccionadas = playasIds?.Any() == true ? playasIds : playasDisponibles.Select(p => p.PlyID).ToList();

            var pagos = _ctx.Pagos
                .AsNoTracking()
                .Include(p => p.Playa)
                .Include(p => p.MetodoPago)
                .Where(p => p.PagFyh >= desdeUtc && p.PagFyh <= hastaUtc)
                .Where(p => playasSeleccionadas.Contains(p.PlyID));

            // Filtrar por métodos de pago si se especifica
            if (metodosIds?.Any() == true)
            {
                pagos = pagos.Where(p => metodosIds.Contains(p.MepID));
            }
            else if (metodoPagoId.HasValue)
            {
                // Compatibilidad con filtro único
                pagos = pagos.Where(p => p.MepID == metodoPagoId.Value);
            }

            var ingresosTotales = pagos.Sum(p => (decimal?)p.PagMonto) ?? 0m;
            var cantPagos = pagos.Count();

            // Obtener TODOS los métodos de pago disponibles (no solo los que tienen pagos)
            var todosLosMetodos = _ctx.AceptaMetodosPago
                .AsNoTracking()
                .Include(a => a.MetodoPago)
                .Where(a => a.MetodoPago != null)
                .GroupBy(a => new { a.MepID, a.MetodoPago!.MepNom })
                .Select(g => new { g.Key.MepID, Nombre = g.Key.MepNom })
                .ToList()
                .ToDictionary(x => x.MepID, x => x.Nombre);

            // Obtener estadísticas de pagos por método (solo para los que tienen pagos)
            var mixRaw = pagos
                .GroupBy(p => p.MepID)
                .Select(g => new
                {
                    MepID = g.Key,
                    Monto = g.Sum(x => x.PagMonto),
                    Cantidad = g.Count()
                })
                .ToList();

            // Crear lista completa de métodos (todos los disponibles, con estadísticas si tienen pagos)
            var mixMetodos = todosLosMetodos.Keys
                .Select(mepId => new MetodoPagoMixVM
                {
                    MepID = mepId,
                    Metodo = todosLosMetodos[mepId],
                    Monto = mixRaw.FirstOrDefault(x => x.MepID == mepId)?.Monto ?? 0m,
                    Cantidad = mixRaw.FirstOrDefault(x => x.MepID == mepId)?.Cantidad ?? 0
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

            // Calcular estadísticas por playa para las playas disponibles
            var estadisticasPorPlaya = pagos
                .GroupBy(p => p.PlyID)
                .Select(g => new
                {
                    PlyID = g.Key,
                    IngresosTotales = g.Sum(x => x.PagMonto),
                    CantPagos = g.Count()
                })
                .ToDictionary(x => x.PlyID, x => new { x.IngresosTotales, x.CantPagos });

            // Actualizar estadísticas en playas disponibles
            foreach (var playa in playasDisponibles)
            {
                if (estadisticasPorPlaya.TryGetValue(playa.PlyID, out var stats))
                {
                    playa.IngresosTotales = stats.IngresosTotales;
                    playa.CantPagos = stats.CantPagos;
                }
            }

            // Obtener detalles de pagos para la tabla (siempre)
            List<InformeDetalleMetodoPagoGeneralItemVM>? detallePagos = null;
            if (pagos.Any())
            {
                var clavesPagos = pagos.Select(p => new { p.PlyID, p.PagNum }).ToList();

                var ocupacionesPorPago = _ctx.Ocupaciones
                    .AsNoTracking()
                    .Where(o => o.PagNum != null)
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
                    .Where(s => s.PagNum != null)
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

                var metodoNombre = "";
                if (metodosIds?.Any() == true)
                {
                    var nombresMetodosSeleccionados = mixMetodos.Where(m => metodosIds.Contains(m.MepID)).Select(m => m.Metodo).ToList();
                    metodoNombre = nombresMetodosSeleccionados.Count == 1 ? nombresMetodosSeleccionados.First() : $"{nombresMetodosSeleccionados.Count} métodos seleccionados";
                }
                else if (metodoPagoId.HasValue)
                {
                    metodoNombre = mixMetodos.FirstOrDefault(m => m.MepID == metodoPagoId.Value)?.Metodo ?? $"Método #{metodoPagoId.Value}";
                }

                detallePagos = pagos
                    .OrderByDescending(p => p.PagFyh)
                    .ToList()
                    .Select(p =>
                    {
                        var key = (p.PlyID, p.PagNum);
                        var cantOcup = ocupacionesPorPago.TryGetValue(key, out var oc) ? oc.Count : 0;
                        var cantServ = serviciosPorPago.TryGetValue(key, out var se) ? se.Count : 0;

                        // Obtener el nombre específico del método de pago para este pago
                        var metodoEspecifico = todosLosMetodos.TryGetValue(p.MepID, out var nomMetodo) ? nomMetodo : $"Método #{p.MepID}";

                        return new InformeDetalleMetodoPagoGeneralItemVM
                        {
                            PlyID = p.PlyID,
                            PlayaNombre = !string.IsNullOrWhiteSpace(p.Playa.PlyNom) ? p.Playa.PlyNom : $"Playa #{p.PlyID}",
                            PagNum = p.PagNum,
                            FechaUtc = p.PagFyh,
                            Monto = p.PagMonto,
                            Metodo = metodoEspecifico,
                            OcupacionesCount = cantOcup,
                            ServiciosExtrasCount = cantServ,
                            OcupacionesVehiculos = ocupacionesPorPago.TryGetValue(key, out var oc2) ? oc2.Vehiculos : new List<string>(),
                            ServiciosExtrasNombres = serviciosPorPago.TryGetValue(key, out var se2) ? se2.Nombres : new List<string>()
                        };
                    })
                    .ToList();
            }

            // Por defecto, si no se especificaron métodos de pago, seleccionar todos los disponibles
            var metodosPagoSeleccionados = metodosIds ?? mixMetodos.Select(m => m.MepID).ToList();

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
                IngresosPorHora = ingresosPorHora,
                PlayasDisponibles = playasDisponibles,
                PlayasSeleccionadas = playasSeleccionadas,
                DetallePagos = detallePagos,
                MetodoPagoSeleccionado = metodoPagoId,
                MetodosPagoSeleccionados = metodosPagoSeleccionados
            };

            // Agregar información adicional al ViewData
            if (metodosIds?.Any() == true)
            {
                var nombresMetodosViewData = mixMetodos.Where(m => metodosIds.Contains(m.MepID)).Select(m => m.Metodo).ToList();
                var metodoNombreViewData = nombresMetodosViewData.Count == 1 ? nombresMetodosViewData.First() : $"{nombresMetodosViewData.Count} métodos seleccionados";
                ViewData["MetodoPagoNombre"] = metodoNombreViewData;
            }
            else if (metodoPagoId.HasValue)
            {
                var metodoNombreViewData = mixMetodos.FirstOrDefault(m => m.MepID == metodoPagoId.Value)?.Metodo ?? $"Método #{metodoPagoId.Value}";
                ViewData["MetodoPagoNombre"] = metodoNombreViewData;
                ViewData["MetodoPagoID"] = metodoPagoId.Value;
            }
            else
            {
                // Si no se especificaron métodos, mostrar "Todos" (porque se seleccionan todos por defecto)
                ViewData["MetodoPagoNombre"] = "Todos";
            }

            return View(vm);
        }

        // =======================
        // PDF "premium"
        // =======================
        [HttpGet]
        public IActionResult Descargar(DateTime? desde, DateTime? hasta, List<int>? playasIds, int? duenioId = null, int? metodoPagoId = null, List<int>? metodosIds = null)
        {
            // Normalizar listas desde querystring cuando vienen separadas por coma
            if (metodosIds == null || metodosIds.Count == 0)
            {
                var qpMetodos = Request.Query["metodosIds"].ToString();
                if (!string.IsNullOrWhiteSpace(qpMetodos))
                {
                    metodosIds = qpMetodos
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim(), out var v) ? v : (int?)null)
                        .Where(v => v.HasValue)
                        .Select(v => v!.Value)
                        .Distinct()
                        .ToList();
                }
            }

            if (playasIds == null || playasIds.Count == 0)
            {
                var qpPlayas = Request.Query["playasIds"].ToString();
                if (!string.IsNullOrWhiteSpace(qpPlayas))
                {
                    playasIds = qpPlayas
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.TryParse(s.Trim(), out var v) ? v : (int?)null)
                        .Where(v => v.HasValue)
                        .Select(v => v!.Value)
                        .Distinct()
                        .ToList();
                }
            }

            var result = Index(desde, hasta, playasIds, duenioId, metodoPagoId, metodosIds) as ViewResult;
            if (result?.Model is not InformeDuenioVM vm)
                return NotFound();

            // Logo opcional
            byte[]? logo = null;
            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "logo.png");
            if (System.IO.File.Exists(logoPath))
                logo = System.IO.File.ReadAllBytes(logoPath);

            // Marca de agua (favicon) -> convertir a PNG por compatibilidad
            byte[]? watermark = null;
            var watermarkPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "favicon-2.ico");
            if (System.IO.File.Exists(watermarkPath))
            {
                try
                {
                    using var fs = System.IO.File.OpenRead(watermarkPath);
                    using var codec = SkiaSharp.SKCodec.Create(fs);
                    using var bmp = SkiaSharp.SKBitmap.Decode(codec);
                    if (bmp != null)
                    {
                        using var img = SkiaSharp.SKImage.FromBitmap(bmp);
                        using var data = img.Encode(SkiaSharp.SKEncodedImageFormat.Png, 90);
                        watermark = data?.ToArray();
                    }
                }
                catch { /* si falla, watermark queda null */ }
            }

            // Dueño
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var duenioNombre = _ctx.Duenios.AsNoTracking()
                .Where(d => d.UsuNU.ToString() == userIdStr)
                .Select(d => d.UsuNyA)
                .FirstOrDefault() ?? "";

            // Lógica de playas: decidir "Todas" vs listar una a una usando el VM
            string playaNombre;
            var todasLasPlayasDisponibles = vm.PlayasDisponibles.Select(p => p.PlyID).ToList();
            var playasSeleccionadasVm = vm.PlayasSeleccionadas ?? new List<int>();
            var setTodasPlayas = new HashSet<int>(todasLasPlayasDisponibles);
            var setSelPlayas = new HashSet<int>(playasSeleccionadasVm);
            bool todasPlayasSeleccionadas = setSelPlayas.Count == setTodasPlayas.Count && setSelPlayas.SetEquals(setTodasPlayas);

            if (todasPlayasSeleccionadas)
            {
                playaNombre = "Todas";
            }
            else if (setSelPlayas.Count == 1)
            {
                var plyId = setSelPlayas.First();
                var playa = vm.PlayasDisponibles.FirstOrDefault(p => p.PlyID == plyId);
                playaNombre = playa?.PlayaNombre ?? $"Playa #{plyId}";
            }
            else if (setSelPlayas.Count > 1)
            {
                var nombresPlayas = vm.PlayasDisponibles
                    .Where(p => setSelPlayas.Contains(p.PlyID))
                    .Select(p => p.PlayaNombre)
                    .ToList();
                playaNombre = string.Join(", ", nombresPlayas);
            }
            else
            {
                // Sin selección explícita en VM (fallback)
                playaNombre = "Todas";
            }

            var pdfBytes = BuildInformePdf(vm, logo, watermark, duenioNombre, playaNombre, vm.DetallePagos, playasIds, metodosIds);

            // Nombre de archivo: incluir playa sin espacios
            var playaSlug = new string((playaNombre ?? "").Where(ch => !char.IsWhiteSpace(ch)).ToArray());
            var fileName = $"informe_{playaSlug}_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }





        // =======================
        // PDF Builder (KPIs arriba, luego gráficos, luego mix y desglose)
        // =======================

        private static byte[] BuildInformePdf(InformeDuenioVM vm, byte[]? logoBytes, byte[]? watermarkBytes, string duenioNombre, string playaNombre, List<InformeDetalleMetodoPagoGeneralItemVM>? detalleItems, List<int>? playasIds = null, List<int>? metodosIds = null)
{
    // Configurar licencia de QuestPDF
    QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    
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
                    if (!string.IsNullOrWhiteSpace(duenioNombre))
                        col.Item().Text($"Dueño: {duenioNombre}")
                            .FontSize(10).FontColor(PdfTheme.Text);
                    
                    // Información específica de filtros aplicados
                    if (!string.IsNullOrWhiteSpace(playaNombre))
                        col.Item().Text($"Playas: {playaNombre}")
                            .FontSize(10).FontColor(PdfTheme.Text);
                    
                    // Lógica de métodos de pago: decidir "Todos" vs lista usando el VM
                    {
                        var todosMetodos = vm.Kpis.MixMetodos?.ToList() ?? new List<MetodoPagoMixVM>();
                        var todosMetodosIds = new HashSet<int>(todosMetodos.Select(m => m.MepID));
                        var seleccionMetodosIds = new HashSet<int>((vm.MetodosPagoSeleccionados?.Any() == true)
                            ? vm.MetodosPagoSeleccionados
                            : todosMetodosIds);

                        bool todosMetodosSeleccionados = seleccionMetodosIds.Count == todosMetodosIds.Count && seleccionMetodosIds.SetEquals(todosMetodosIds);

                        if (todosMetodosSeleccionados)
                        {
                            col.Item().Text("Métodos de pago: Todos").FontSize(10).FontColor(PdfTheme.Text);
                        }
                        else if (seleccionMetodosIds.Count == 1)
                        {
                            var unicoId = seleccionMetodosIds.First();
                            var nombre = todosMetodos.FirstOrDefault(m => m.MepID == unicoId)?.Metodo ?? $"Método #{unicoId}";
                            col.Item().Text($"Método de pago: {nombre}").FontSize(10).FontColor(PdfTheme.Text);
                        }
                        else
                        {
                            var nombres = todosMetodos
                                .Where(m => seleccionMetodosIds.Contains(m.MepID))
                                .Select(m => m.Metodo)
                                .ToList();
                            col.Item().Text($"Métodos de pago: {string.Join(", ", nombres)}").FontSize(10).FontColor(PdfTheme.Text);
                        }
                    }
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

                // ===== (2) GRÁFICOS: usar la misma configuración Chart.js vía QuickChart =====
                byte[]? chartDiaPng = null;
                byte[]? chartHoraPng = null;
                try
                {
                    if (vm.IngresosPorDia?.Any() == true)
                        chartDiaPng = BuildQuickChart("line", vm.IngresosPorDia, "Ingresos por día");
                    if (vm.IngresosPorHora?.Any() == true)
                        chartHoraPng = BuildQuickChart("bar", vm.IngresosPorHora, "Ingresos por hora");
                }
                catch { }

                // Gráficos en una fila para ahorrar espacio
                if (chartDiaPng is not null || chartHoraPng is not null)
                {
                    col.Item().PaddingTop(10).Row(row =>
                    {
                        if (chartDiaPng is not null)
                        {
                            row.RelativeItem().Element(card =>
                {
                    card.Border(1).BorderColor(PdfTheme.Border)
                        .Padding(PdfTheme.CardPadding).Column(cc =>
                        {
                                        cc.Item().Text("Ingresos por día").FontSize(10).Bold().FontColor(PdfTheme.Accent);
                                cc.Item().Image(chartDiaPng).FitWidth();
                        });
                });
                        }

                        if (chartHoraPng is not null)
                        {
                            row.RelativeItem().PaddingLeft(8).Element(card =>
                {
                    card.Border(1).BorderColor(PdfTheme.Border)
                        .Padding(PdfTheme.CardPadding).Column(cc =>
                        {
                                        cc.Item().Text("Ingresos por hora").FontSize(10).Bold().FontColor(PdfTheme.Accent);
                                cc.Item().Image(chartHoraPng).FitWidth();
                        });
                });
                        }
                    });
                }

                // Separador bien fino y con poco margen
                col.Item().PaddingTop(8).Element(e => e.BorderBottom(0.5f).BorderColor(PdfTheme.Border));

                // ===== (3) MÉTODOS DE PAGO =====
                col.Item().PaddingTop(10).Element(section =>
                {
                    section.Column(c =>
                    {
                        c.Item().Text("Métodos de pago")
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
                // ===== (5) DETALLE DE PAGOS =====
                if (detalleItems != null && detalleItems.Any())
                {
                    col.Item().PaddingTop(14).Element(section =>
                    {
                        section.Column(c =>
                        {
                            c.Item().Text("Detalle de pagos")
                                .FontSize(12).Bold().FontColor(PdfTheme.PrimaryDark);

                            c.Item().PaddingTop(6).Element(t =>
                            {
                                t.Table(table =>
                                {
                                    table.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn(2);   // # Pago
                                        cols.RelativeColumn(3);   // Fecha
                                        cols.RelativeColumn(3);   // Monto
                                        cols.RelativeColumn(4);   // Método
                                    });

                                    table.Header(h =>
                                    {
                                        h.Cell().Element(Th).Text("# Pago");
                                        h.Cell().Element(Th).Text("Fecha");
                                        h.Cell().Element(ThRight).Text("Monto");
                                        h.Cell().Element(Th).Text("Método");
                                    });

                                    var zebra = false;
                                    foreach (var x in detalleItems)
                                    {
                                        zebra = !zebra;
                                        table.Cell().Element(r => Td(r, zebra)).Text(x.PagNum.ToString());
                                        table.Cell().Element(r => Td(r, zebra)).Text(x.FechaUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                                        table.Cell().Element(r => TdRight(r, zebra)).Text(PdfTheme.Money(x.Monto));
                                        table.Cell().Element(r => Td(r, zebra)).Text(x.Metodo);

                                        // Subfila descriptiva
                                        var desc = new List<string>();
                                        if (x.OcupacionesCount > 0)
                                            desc.Add($"Incluye {x.OcupacionesCount} {(x.OcupacionesCount == 1 ? "ocupación" : "ocupaciones")}");
                                        if (x.ServiciosExtrasNombres?.Any() == true)
                                            desc.Add("Servicios extra: " + string.Join(", ", x.ServiciosExtrasNombres));

                                        if (desc.Count > 0)
                                        {
                                            table.Cell().ColumnSpan(4).Element(r => Td(r, zebra)).Text(string.Join(". ", desc) + ".");
                                        }
                                    }
                                });
                            });
                        });
                    });
                }
                
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

            // ===== Marca de agua (inferior izquierda) =====
            if (watermarkBytes is not null)
            {
                page.Background().Element(bg =>
                {
                    bg.AlignLeft().AlignBottom().Padding(24).Row(r =>
                    {
                        r.ConstantItem(40).Image(watermarkBytes).FitHeight();
                        r.RelativeItem().PaddingLeft(8).AlignLeft().AlignBottom().Text(t =>
                        {
                            t.Span("NetParking").FontSize(20).SemiBold().FontColor(Colors.Grey.Lighten1);
                        });
                    });
                });
            }

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

            const int W = 900, H = 500; // relación ~1.8 para reducir espacio vertical
            const int padL = 60, padR = 18, padT = 18, padB = 44;
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

            // Ticks + labels en Y (0..max)
            var yLabels = string.Join("", Enumerable.Range(0, 6).Select(i =>
            {
                var val = max / 5m * i;
                var y = padT + plotH - (float)(val / max) * plotH;
                return $"<text x='{F(padL - 6)}' y='{F(y + 4)}' font-size='10' text-anchor='end' fill='#475569'>{val:0}</text>";
            }));

            // Labels en X (hasta 8 marcas uniformes)
            int xTicks = Math.Min(10, n);
            var xLabels = string.Join("", Enumerable.Range(0, xTicks).Select(i =>
            {
                int idx = (int)Math.Round(i * (n - 1) / (double)Math.Max(1, xTicks - 1));
                var (xx, _) = Pt(idx);
                return $"<text x='{F(xx)}' y='{F(padT + plotH + 14)}' font-size='10' text-anchor='middle' fill='#475569'>{series[idx].Label}</text>";
            }));

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
<svg width='{W}' height='{H}' viewBox='0 0 {W} {H}' preserveAspectRatio='none' xmlns='http://www.w3.org/2000/svg'>
  <rect x='0' y='0' width='{W}' height='{H}' fill='white'/>
  {legend}
  {grid}
  {axes}
  {yLabels}
  {xLabels}
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

    const int W = 900, H = 500;
    const int padL = 60, padR = 18, padT = 18, padB = 44;
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

    // Ticks + labels en Y
    var yLabels = string.Join("", Enumerable.Range(0, 6).Select(i =>
    {
        var val = max / 5m * i;
        var y = padT + plotH - (float)(val / max) * plotH;
        return $"<text x='{F(padL - 6)}' y='{F(y + 4)}' font-size='10' text-anchor='end' fill='#475569'>{val:0}</text>";
    }));

    // Labels en X para 24 horas
    var xLabels = string.Join("", Enumerable.Range(0, n).Select(i =>
    {
        var x = padL + i * (barW + gap) + barW / 2f;
        return $"<text x='{F(x)}' y='{F(padT + plotH + 14)}' font-size='9' text-anchor='middle' fill='#475569'>{series[i].Label}</text>";
    }));

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
<svg width='{W}' height='{H}' viewBox='0 0 {W} {H}' preserveAspectRatio='none' xmlns='http://www.w3.org/2000/svg'>
  <rect x='0' y='0' width='{W}' height='{H}' fill='white'/>
  {legend}
  {grid}
  {axes}
  {yLabels}
  {xLabels}
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

        // ===== QuickChart helper =====
        private static byte[]? BuildQuickChart(string type, List<SeriePuntoVM> series, string title)
        {
            try
            {
                var labels = series.Select(s => s.Label).ToList();
                var values = series.Select(s => s.Valor).ToList();

                var cfg = new
                {
                    type,
                    data = new
                    {
                        labels,
                        datasets = new[]
                        {
                            new { label = title, data = values }
                        }
                    },
                    options = new
                    {
                        responsive = true,
                        plugins = new { legend = new { position = "top" } },
                        scales = new
                        {
                            y = new { beginAtZero = true }
                        }
                    }
                };

                var url = "https://quickchart.io/chart";
                var payload = new { width = 450, height = 200, format = "png", backgroundColor = "white", chart = cfg };
                var json = JsonSerializer.Serialize(payload);
                var resp = Http.PostAsync(url, new StringContent(json, System.Text.Encoding.UTF8, "application/json")).Result;
                if (!resp.IsSuccessStatusCode) return null;
                return resp.Content.ReadAsByteArrayAsync().Result;
            }
            catch { return null; }
        }
    }
}
