namespace estacionamientos.ViewModels
{
    public class RegistrarPagosRequest
    {
        public int PlyID { get; set; }
        public int PlzNum { get; set; }
        public DateTime AboFyhIni { get; set; }
        public List<int> PeriodosAPagar { get; set; } = new();
        public int MetodoPago { get; set; }
        public DateTime FechaPago { get; set; }
        public decimal TotalPagar { get; set; }
    }
}
