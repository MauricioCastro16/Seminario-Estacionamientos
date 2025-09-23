namespace estacionamientos.ViewModels
{
    public class TarifaHistGroupVM
    {
        public string ServicioNombre { get; set; } = "";
        public List<TarifaPeriodoVM> Periodos { get; set; } = new();
    }

    public class TarifaPeriodoVM
    {
        public string ClaseVehiculo { get; set; } = "";
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public decimal Monto { get; set; }
        public bool Vigente => !FechaFin.HasValue || FechaFin > DateTime.UtcNow;
    }
}
