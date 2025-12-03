using System.Globalization;
using System.Linq;
using System.Security.Claims;
using estacionamientos.Data;
using estacionamientos.Models;
using estacionamientos.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace estacionamientos.Controllers;

public class GestionController(AppDbContext ctx) : BaseController
{
    private readonly AppDbContext _ctx = ctx;

    [HttpGet]
    public async Task<IActionResult> Index(string? modo = null, int? playaId = null, DateTime? desde = null, DateTime? hasta = null, List<int>? playasIds = null)
    {
        SetBreadcrumb(
            new BreadcrumbItem { Title = "Gestión", Url = Url.Action("Index", "Gestion")! }
        );
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var duenioId))
        {
            return Unauthorized();
        }

        var playasRaw = await _ctx.Playas
            .AsNoTracking()
            .Where(pl => pl.Administradores.Any(a => a.DueNU == duenioId))
            .Select(pl => new
            {
                pl.PlyID,
                pl.PlyNom,
                pl.PlyCiu,
                pl.PlyDir
            })
            .ToListAsync();

        var playasDisponibles = playasRaw
            .Select(pl => new GestionPlayaOptionVM
            {
                PlyID = pl.PlyID,
                PlayaNombre = !string.IsNullOrWhiteSpace(pl.PlyNom)
                    ? pl.PlyNom
                    : $"{pl.PlyCiu} - {pl.PlyDir}"
            })
            .OrderBy(pl => pl.PlayaNombre)
            .ToList();

        var modoSeleccionado = ParseModo(modo);

        var playasSeleccionadas = playasIds?.Any() == true
            ? playasIds.Where(id => playasDisponibles.Any(p => p.PlyID == id)).Distinct().ToList()
            : playasDisponibles.Select(p => p.PlyID).ToList();

        var hoy = DateTime.Now;
        var filtros = new GestionFiltroVM
        {
            Desde = (desde ?? hoy.AddDays(-30)).Date,
            Hasta = (hasta ?? hoy.Date)
        };

        var vm = new GestionDashboardVM
        {
            Modo = modoSeleccionado,
            Filtros = filtros,
            PlayasDisponibles = playasDisponibles,
            PlayasSeleccionadas = playasSeleccionadas,
            PlayaSeleccionadaId = playaId,
            PlayaSeleccionadaNombre = playaId.HasValue
                ? playasDisponibles.FirstOrDefault(p => p.PlyID == playaId.Value)?.PlayaNombre ?? "Playas seleccionadas"
                : (playasSeleccionadas.Count == 1
                    ? playasDisponibles.FirstOrDefault(p => p.PlyID == playasSeleccionadas[0])?.PlayaNombre ?? "Playas seleccionadas"
                    : "Playas seleccionadas")
        };

        if (playasSeleccionadas.Count > 0)
        {
            if (modoSeleccionado == GestionModo.EnVivo)
            {
                vm.EnVivo = await BuildEnVivoAsync(playasSeleccionadas, vm.PlayaSeleccionadaNombre);
            }
            else
            {
                var (desdeUtc, hastaUtc) = NormalizarFechas(filtros.Desde, filtros.Hasta);
                vm.Historico = await BuildHistoricoAsync(playasSeleccionadas, desdeUtc, hastaUtc);
            }
        }

        return View(vm);
    }

    private GestionModo ParseModo(string? modo) => modo?.Trim().ToLowerInvariant() switch
    {
        "rango" or "rango-fechas" or "historico" or "histórico" => GestionModo.RangoFechas,
        _ => GestionModo.EnVivo
    };

    private static (DateTime desdeUtc, DateTime hastaUtc) NormalizarFechas(DateTime desdeLocal, DateTime hastaLocal)
    {
        var inicioDia = new DateTime(desdeLocal.Year, desdeLocal.Month, desdeLocal.Day, 0, 0, 0, DateTimeKind.Local);
        var finDia = new DateTime(hastaLocal.Year, hastaLocal.Month, hastaLocal.Day, 23, 59, 59, 999, DateTimeKind.Local);
        return (inicioDia.ToUniversalTime(), finDia.ToUniversalTime());
    }

    private async Task<GestionEnVivoVM> BuildEnVivoAsync(List<int> playasIds, string playaNombre)
    {
        var ahoraUtc = DateTime.UtcNow;
        var hoyLocal = DateTime.Now;
        var hoyNormalizado = NormalizarFechas(hoyLocal.Date, hoyLocal.Date);
        var hoyInicioUtc = hoyNormalizado.desdeUtc;
        var hoyFinUtc = hoyNormalizado.hastaUtc;
        var haceUnaHoraUtc = ahoraUtc.AddHours(-1);

        var plazasTotales = await _ctx.Plazas
            .AsNoTracking()
            .Where(p => playasIds.Contains(p.PlyID))
            .Select(p => new { p.PlyID, p.PlzNum, p.PlzHab, p.Piso, p.PlzNombre })
            .ToListAsync();

        var plazasHabilitadas = plazasTotales.Count(p => p.PlzHab);

        var plazasNoHab = plazasTotales
            .Where(p => !p.PlzHab)
            .Select(p => new GestionPlazaEstadoVM
            {
                PlzNum = p.PlzNum,
                Piso = p.Piso,
                Nombre = p.PlzNombre
            })
            .OrderBy(p => p.Piso)
            .ThenBy(p => p.PlzNum)
            .Take(10)
            .ToList();

        var ocupacionesActivas = await _ctx.Ocupaciones
            .AsNoTracking()
            .Where(o => playasIds.Contains(o.PlyID) && o.OcufFyhFin == null)
            .Select(o => new { o.PlyID, o.PlzNum, o.VehPtnt, o.OcufFyhIni, o.OcuLlavDej })
            .ToListAsync();

        var plazasOcupadas = ocupacionesActivas.Count;
        var ocupacionesHoyCompletas = await _ctx.Ocupaciones
            .AsNoTracking()
            .Where(o => playasIds.Contains(o.PlyID)
                        && o.OcufFyhIni >= hoyInicioUtc
                        && o.OcufFyhIni <= hoyFinUtc
                        && o.OcufFyhFin != null
                        && o.OcufFyhFin >= hoyInicioUtc
                        && o.OcufFyhFin <= hoyFinUtc)
            .Select(o => new { o.OcufFyhIni, o.OcufFyhFin })
            .ToListAsync();

        var promedioActiva = ocupacionesHoyCompletas.Any()
            ? ocupacionesHoyCompletas.Average(o => (o.OcufFyhFin!.Value - o.OcufFyhIni).TotalMinutes)
            : 0d;

        var playerosActivos = await _ctx.Turnos
            .AsNoTracking()
            .Where(t => playasIds.Contains(t.PlyID) && t.TurFyhFin == null)
            .Select(t => t.PlaNU)
            .Distinct()
            .CountAsync();

        var serviciosDisponibles = await _ctx.ServiciosProveidos
            .AsNoTracking()
            .Include(sp => sp.Servicio)
            .Where(sp => playasIds.Contains(sp.PlyID))
            .Select(sp => new GestionServicioCatalogoVM
            {
                Servicio = sp.Servicio.SerNom,
                Habilitado = sp.SerProvHab
            })
            .OrderBy(sp => sp.Servicio)
            .ToListAsync();

        var serviciosActivos = serviciosDisponibles.Count(sp => sp.Habilitado);

        var entradasHoy = await _ctx.Ocupaciones
            .AsNoTracking()
            .Where(o => playasIds.Contains(o.PlyID) && o.OcufFyhIni >= hoyInicioUtc && o.OcufFyhIni <= hoyFinUtc)
            .CountAsync();

        var egresosHoy = await _ctx.Ocupaciones
            .AsNoTracking()
            .Where(o => playasIds.Contains(o.PlyID) && o.OcufFyhFin != null && o.OcufFyhFin >= hoyInicioUtc && o.OcufFyhFin <= hoyFinUtc)
            .CountAsync();

        var rotacionesHora = await _ctx.Ocupaciones
            .AsNoTracking()
            .Where(o => playasIds.Contains(o.PlyID) && o.OcufFyhFin != null && o.OcufFyhFin >= haceUnaHoraUtc)
            .CountAsync();

        var movimientosHora = await _ctx.MovimientosPlayeros
            .AsNoTracking()
            .Where(m => playasIds.Contains(m.PlyID) && m.FechaMov >= haceUnaHoraUtc)
            .GroupBy(m => m.TipoMov)
            .Select(g => new GestionMovimientoResumenVM
            {
                Tipo = g.Key.ToString(),
                Conteo = g.Count()
            })
            .OrderByDescending(g => g.Conteo)
            .ToListAsync();

        var vehiculosActuales = ocupacionesActivas.Select(o => o.VehPtnt).Distinct().ToList();

        var vehiculosAbonadosActuales = 0;
        if (vehiculosActuales.Any())
        {
            var vehiculosAbonados = await _ctx.VehiculosAbonados
                .AsNoTracking()
                .Where(va => playasIds.Contains(va.PlyID) && vehiculosActuales.Contains(va.VehPtnt))
                .Join(_ctx.Abonos.AsNoTracking(),
                      va => new { va.PlyID, va.PlzNum, va.AboFyhIni },
                      ab => new { ab.PlyID, ab.PlzNum, ab.AboFyhIni },
                      (va, ab) => new { va.VehPtnt, ab.AboFyhIni, ab.AboFyhFin, ab.EstadoPago })
                .Where(x => x.EstadoPago == EstadoPago.Activo &&
                            x.AboFyhIni <= ahoraUtc &&
                            (x.AboFyhFin == null || x.AboFyhFin >= ahoraUtc))
                .Select(x => x.VehPtnt)
                .Distinct()
                .ToListAsync();

            vehiculosAbonadosActuales = vehiculosAbonados.Count;
        }

        var clasVehPorPatente = await _ctx.Vehiculos
            .AsNoTracking()
            .Where(v => vehiculosActuales.Contains(v.VehPtnt))
            .Select(v => new { v.VehPtnt, v.ClasVehID })
            .ToListAsync();

        var clasVehIds = clasVehPorPatente.Select(v => v.ClasVehID).Distinct().ToList();
        var clasificaciones = await _ctx.ClasificacionesVehiculo
            .AsNoTracking()
            .Where(c => clasVehIds.Contains(c.ClasVehID))
            .Select(c => new { c.ClasVehID, c.ClasVehTipo })
            .ToListAsync();

        var clasificacionVehiculos = clasVehPorPatente
            .GroupBy(v => v.ClasVehID)
            .Select(g =>
            {
                var nombre = clasificaciones.FirstOrDefault(c => c.ClasVehID == g.Key)?.ClasVehTipo ?? "Sin clasificación";
                return new GestionSerieValorVM
                {
                    Label = nombre,
                    Value = g.Count()
                };
            })
            .OrderByDescending(c => c.Value)
            .ToList();

        var valoracionesRecientes = new List<GestionValoracionRecienteVM>();
        if (playasIds.Count == 1)
        {
            var plyId = playasIds[0];
            var valoracionesRaw = await _ctx.Valoraciones
                .AsNoTracking()
                .Include(v => v.Conductor)
                .Where(v => v.PlyID == plyId)
                .OrderByDescending(v => v.ConNU)
                .Take(5)
                .ToListAsync();
            
            valoracionesRecientes = valoracionesRaw
                .Select(v => new GestionValoracionRecienteVM
                {
                    Estrellas = v.ValNumEst,
                    Favorito = v.ValFav,
                    FechaUtc = null,
                    Comentario = v.ValComentario,
                    ConductorNombre = v.Conductor?.UsuNyA ?? "Conductor desconocido"
                })
                .ToList();
        }

        var valoracionesLista = await _ctx.Playas
            .AsNoTracking()
            .Where(pl => playasIds.Contains(pl.PlyID))
            .Select(pl => pl.PlyValProm)
            .ToListAsync();

        var valoracionPromedio = valoracionesLista.Any() ? valoracionesLista.Average() : 0m;

        var abonosActivos = await _ctx.Abonos
            .AsNoTracking()
            .Where(a => playasIds.Contains(a.PlyID) &&
                        a.EstadoPago == EstadoPago.Activo &&
                        a.AboFyhIni <= ahoraUtc &&
                        (a.AboFyhFin == null || a.AboFyhFin >= ahoraUtc))
            .CountAsync();

        var vehiculosOcasionales = Math.Max(0, vehiculosActuales.Count - vehiculosAbonadosActuales);

        return new GestionEnVivoVM
        {
            GeneradoUtc = DateTime.UtcNow,
            PlayasMonitoreadas = playasIds.Count,
            PlayaNombre = playaNombre,
            PlazasHabilitadas = plazasHabilitadas,
            PlazasOcupadas = plazasOcupadas,
            PlazasFueraServicio = plazasNoHab.Count,
            PlayerosActivos = playerosActivos,
            ServiciosExtrasActivos = serviciosActivos,
            EstadoOperativo = playerosActivos > 0 ? "Abierta" : "Sin playeros",
            ValoracionPromedio = Math.Round(valoracionPromedio, 2),
            AbonosActivos = abonosActivos,
            EntradasHoy = entradasHoy,
            EgresosHoy = egresosHoy,
            RotacionesUltimaHora = rotacionesHora,
            DuracionPromedioActivaMin = Math.Round(promedioActiva, 1),
            VehiculosAbonadosActuales = vehiculosAbonadosActuales,
            VehiculosOcasionalesActuales = vehiculosOcasionales,
            ClasificacionVehiculos = clasificacionVehiculos,
            PlazasNoHabilitadas = plazasNoHab,
            ServiciosDisponibles = serviciosDisponibles,
            MovimientosUltimaHora = movimientosHora,
            ValoracionesRecientes = valoracionesRecientes
        };
    }

    private async Task<GestionHistoricoVM> BuildHistoricoAsync(List<int> playasIds, DateTime desdeUtc, DateTime hastaUtc)
    {
        var ocupacionesRango = await _ctx.Ocupaciones
            .AsNoTracking()
            .Where(o => playasIds.Contains(o.PlyID) && o.OcufFyhIni >= desdeUtc && o.OcufFyhIni <= hastaUtc)
            .Select(o => new { o.VehPtnt, o.OcufFyhIni, o.OcufFyhFin, o.OcuLlavDej, o.PlyID })
            .ToListAsync();

        var entradas = ocupacionesRango.Count;
        var egresos = ocupacionesRango.Count(o => o.OcufFyhFin.HasValue && o.OcufFyhFin <= hastaUtc);

        var permanenciaPromedio = ocupacionesRango.Any()
            ? ocupacionesRango
                .Where(o => o.OcufFyhFin.HasValue)
                .DefaultIfEmpty()
                .Average(o => o == null
                    ? 0
                    : (o.OcufFyhFin!.Value - o.OcufFyhIni).TotalMinutes)
            : 0d;

        var serviciosExtrasCompletados = await _ctx.ServiciosExtrasRealizados
            .AsNoTracking()
            .Where(s => playasIds.Contains(s.PlyID) && s.ServExFyHIni >= desdeUtc && s.ServExFyHIni <= hastaUtc && s.ServExFyHFin != null)
            .CountAsync();

        var vehiculosUnicos = ocupacionesRango.Select(o => o.VehPtnt).Distinct().Count();
        var llavesPromedio = ocupacionesRango.Where(o => o.OcuLlavDej).Count();

        var reubicaciones = await _ctx.MovimientosPlayeros
            .AsNoTracking()
            .Where(m => playasIds.Contains(m.PlyID)
                        && m.FechaMov >= desdeUtc
                        && m.FechaMov <= hastaUtc
                        && m.TipoMov == TipoMovimiento.ReubicacionVehiculo)
            .CountAsync();

        var ocupacionDiaria = ocupacionesRango
            .GroupBy(o => o.OcufFyhIni.ToLocalTime().Date)
            .Select(g => new GestionSerieValorVM
            {
                Label = g.Key.ToString("dd/MM"),
                Value = g.Count()
            })
            .OrderBy(g => g.Label)
            .ToList();

        var rotacionDiaria = ocupacionesRango
            .Where(o => o.OcufFyhFin.HasValue)
            .GroupBy(o => o.OcufFyhFin!.Value.ToLocalTime().Date)
            .Select(g => new GestionSerieValorVM
            {
                Label = g.Key.ToString("dd/MM"),
                Value = g.Count()
            })
            .OrderBy(g => g.Label)
            .ToList();

        var clasificacionVehiculos = new List<GestionSerieValorVM>();
        var topPatentes = ocupacionesRango
            .GroupBy(o => o.VehPtnt)
            .Select(g => new GestionSerieValorVM { Label = g.Key, Value = g.Count() })
            .OrderByDescending(g => g.Value)
            .Take(5)
            .ToList();

        if (ocupacionesRango.Any())
        {
            var patentes = ocupacionesRango.Select(o => o.VehPtnt).Distinct().ToList();
            var vehClas = await _ctx.Vehiculos
                .AsNoTracking()
                .Where(v => patentes.Contains(v.VehPtnt))
                .Select(v => new { v.VehPtnt, v.ClasVehID })
                .ToListAsync();

            var clasIds = vehClas.Select(v => v.ClasVehID).Distinct().ToList();
            var clas = await _ctx.ClasificacionesVehiculo
                .AsNoTracking()
                .Where(c => clasIds.Contains(c.ClasVehID))
                .Select(c => new { c.ClasVehID, c.ClasVehTipo })
                .ToListAsync();

            clasificacionVehiculos = vehClas
                .GroupBy(v => v.ClasVehID)
                .Select(g => new GestionSerieValorVM
                {
                    Label = clas.FirstOrDefault(c => c.ClasVehID == g.Key)?.ClasVehTipo ?? "Sin clasificación",
                    Value = g.Count()
                })
                .OrderByDescending(g => g.Value)
                .ToList();
        }

        var playerosActivos = await _ctx.MovimientosPlayeros
            .AsNoTracking()
            .Where(m => playasIds.Contains(m.PlyID) && m.FechaMov >= desdeUtc && m.FechaMov <= hastaUtc)
            .GroupBy(m => m.PlaNU)
            .Select(g => new GestionSerieValorVM
            {
                Label = $"#{g.Key}",
                Value = g.Count()
            })
            .OrderByDescending(g => g.Value)
            .Take(5)
            .ToListAsync();

        var serviciosExtraPorTipo = await _ctx.ServiciosExtrasRealizados
            .AsNoTracking()
            .Include(s => s.ServicioProveido)
            .ThenInclude(sp => sp!.Servicio)
            .Where(s => playasIds.Contains(s.PlyID) && s.ServExFyHIni >= desdeUtc && s.ServExFyHIni <= hastaUtc && s.ServicioProveido != null && s.ServicioProveido.Servicio != null)
            .GroupBy(s => s.ServicioProveido!.Servicio!.SerNom)
            .Select(g => new GestionSerieValorVM
            {
                Label = g.Key,
                Value = g.Count()
            })
            .OrderByDescending(g => g.Value)
            .ToListAsync();

        var valoracionesHistoricas = await _ctx.Playas
            .AsNoTracking()
            .Where(pl => playasIds.Contains(pl.PlyID))
            .Select(pl => pl.PlyValProm)
            .ToListAsync();

        var valoracionPromedio = valoracionesHistoricas.Any() ? valoracionesHistoricas.Average() : 0m;

        var abonosCoincidentes = await _ctx.Abonos
            .AsNoTracking()
            .Where(a => playasIds.Contains(a.PlyID) &&
                        a.AboFyhIni <= hastaUtc &&
                        (a.AboFyhFin == null || a.AboFyhFin >= desdeUtc))
            .CountAsync();

        return new GestionHistoricoVM
        {
            DesdeUtc = desdeUtc,
            HastaUtc = hastaUtc,
            Entradas = entradas,
            Egresos = egresos,
            Reubicaciones = reubicaciones,
            ServiciosExtrasCompletados = serviciosExtrasCompletados,
            VehiculosUnicos = vehiculosUnicos,
            PermanenciaPromedioMin = Math.Round(permanenciaPromedio, 1),
            LlavesPromedio = llavesPromedio,
            AbonosCoincidentes = abonosCoincidentes,
            ValoracionPromedio = Math.Round(valoracionPromedio, 2),
            OcupacionDiaria = ocupacionDiaria,
            RotacionDiaria = rotacionDiaria,
            ClasificacionVehiculos = clasificacionVehiculos,
            TopPatentes = topPatentes,
            PlayerosActivos = playerosActivos,
            ServiciosExtraPorTipo = serviciosExtraPorTipo
        };
    }
}

