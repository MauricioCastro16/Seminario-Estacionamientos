using System;
using System.Collections.Generic;

namespace estacionamientos.ViewModels;

public enum GestionModo
{
    EnVivo,
    RangoFechas
}

public class GestionFiltroVM
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
}

public class GestionPlayaOptionVM
{
    public int PlyID { get; set; }
    public string PlayaNombre { get; set; } = string.Empty;
}

public class GestionSerieValorVM
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string? Extra { get; set; }
}

public class GestionNivelOcupacionVM
{
    public int? Piso { get; set; }
    public int PlazasHabilitadas { get; set; }
    public int PlazasOcupadas { get; set; }
}

public class GestionPlazaEstadoVM
{
    public int PlzNum { get; set; }
    public int? Piso { get; set; }
    public string? Nombre { get; set; }
}

public class GestionServicioDetalleVM
{
    public string Servicio { get; set; } = string.Empty;
    public string? Vehiculo { get; set; }
    public DateTime InicioUtc { get; set; }
    public int? DuracionEstimadaMin { get; set; }
    public double MinutosTranscurridos { get; set; }
    public bool Atrasado { get; set; }
}

public class GestionMovimientoResumenVM
{
    public string Tipo { get; set; } = string.Empty;
    public int Conteo { get; set; }
}

public class GestionValoracionRecienteVM
{
    public int Estrellas { get; set; }
    public bool Favorito { get; set; }
    public DateTime? FechaUtc { get; set; }
}

public class GestionEnVivoVM
{
    public DateTime GeneradoUtc { get; set; } = DateTime.UtcNow;
    public int PlayasMonitoreadas { get; set; }
    public string? PlayaNombre { get; set; }
    public int PlazasHabilitadas { get; set; }
    public int PlazasOcupadas { get; set; }
    public int PlazasFueraServicio { get; set; }
    public int PlayerosActivos { get; set; }
    public int ServiciosExtrasActivos { get; set; }
    public string EstadoOperativo { get; set; } = "Sin datos";
    public decimal ValoracionPromedio { get; set; }
    public int AbonosActivos { get; set; }
    public int EntradasHoy { get; set; }
    public int EgresosHoy { get; set; }
    public int RotacionesUltimaHora { get; set; }
    public double DuracionPromedioActivaMin { get; set; }
    public int LlavesEnCustodia { get; set; }
    public int VehiculosAbonadosActuales { get; set; }
    public int VehiculosOcasionalesActuales { get; set; }
    public List<GestionSerieValorVM> ClasificacionVehiculos { get; set; } = new();
    public List<GestionSerieValorVM> TopPatentesHoy { get; set; } = new();
    public List<GestionNivelOcupacionVM> OcupacionPorPiso { get; set; } = new();
    public List<GestionPlazaEstadoVM> PlazasNoHabilitadas { get; set; } = new();
    public List<GestionServicioDetalleVM> ServiciosPendientes { get; set; } = new();
    public List<GestionMovimientoResumenVM> MovimientosUltimaHora { get; set; } = new();
    public List<GestionValoracionRecienteVM> ValoracionesRecientes { get; set; } = new();
    public bool AlertaSinPlayeros => PlayerosActivos == 0;
}

public class GestionHistoricoVM
{
    public DateTime DesdeUtc { get; set; }
    public DateTime HastaUtc { get; set; }
    public int Entradas { get; set; }
    public int Egresos { get; set; }
    public int Reubicaciones { get; set; }
    public int ServiciosExtrasCompletados { get; set; }
    public int VehiculosUnicos { get; set; }
    public double PermanenciaPromedioMin { get; set; }
    public int LlavesPromedio { get; set; }
    public int AbonosCoincidentes { get; set; }
    public decimal ValoracionPromedio { get; set; }
    public List<GestionSerieValorVM> OcupacionDiaria { get; set; } = new();
    public List<GestionSerieValorVM> RotacionDiaria { get; set; } = new();
    public List<GestionSerieValorVM> ClasificacionVehiculos { get; set; } = new();
    public List<GestionSerieValorVM> TopPatentes { get; set; } = new();
    public List<GestionSerieValorVM> PlayerosActivos { get; set; } = new();
    public List<GestionSerieValorVM> ServiciosExtraPorTipo { get; set; } = new();
}

public class GestionDashboardVM
{
    public GestionModo Modo { get; set; } = GestionModo.EnVivo;
    public GestionFiltroVM Filtros { get; set; } = new();
    public List<GestionPlayaOptionVM> PlayasDisponibles { get; set; } = new();
    public List<int> PlayasSeleccionadas { get; set; } = new();
    public int? PlayaSeleccionadaId { get; set; }
    public string PlayaSeleccionadaNombre { get; set; } = string.Empty;
    public GestionEnVivoVM EnVivo { get; set; } = new();
    public GestionHistoricoVM Historico { get; set; } = new();
}
