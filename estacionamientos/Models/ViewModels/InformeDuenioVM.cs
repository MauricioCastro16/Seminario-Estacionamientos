using System;
using System.Collections.Generic;

namespace estacionamientos.ViewModels
{
    // Filtros que muestra/recibe la vista (fechas en local para la UI)
    public class InformeFiltroVM
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public List<int>? PlayasIds { get; set; }
        public int? DuenioId { get; set; }
    }

    // Item del mix por método de pago
    public class MetodoPagoMixVM
    {
        public int MepID { get; set; }
        public string Metodo { get; set; } = "";
        public decimal Monto { get; set; }
        public int Cantidad { get; set; }
        public decimal PorcentajeMonto { get; set; }
    }

    // KPIs ejecutivos
    public class InformeKpisVM
    {
        public decimal IngresosTotales { get; set; }
        public int CantPagos { get; set; }
        public decimal TicketPromedio => CantPagos > 0 ? IngresosTotales / CantPagos : 0m;
        public List<MetodoPagoMixVM> MixMetodos { get; set; } = new();
    }

    // Fila del desglose por playa
    public class InformePlayaRowVM
    {
        public int PlyID { get; set; }
        public string PlayaNombre { get; set; } = "";
        public decimal IngresosTotales { get; set; }
        public int CantPagos { get; set; }
        public decimal TicketPromedio => CantPagos > 0 ? IngresosTotales / CantPagos : 0m;
    }

    // VM raíz para la vista
    public class InformeDuenioVM
    {
        public InformeFiltroVM Filtros { get; set; } = new();
        public InformeKpisVM Kpis { get; set; } = new();
        public List<InformePlayaRowVM> PorPlaya { get; set; } = new();
    }
}
