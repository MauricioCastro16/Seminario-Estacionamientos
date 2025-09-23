namespace estacionamientos.Models.ViewModels
{
    public class TarifasIndexVM
    {
        public string? Q { get; set; }
        public string FilterBy { get; set; } = "todos";
        public string? SelectedOption { get; set; }
        public string? Remove { get; set; }

        // Filtros acumulados
        public List<string> Servicios { get; set; } = new();
        public List<string> Clases { get; set; } = new();
        public List<string> Todos { get; set; } = new();
        public List<string> Montos { get; set; } = new();
        public List<string> FechasDesde { get; set; } = new();
        public List<string> FechasHasta { get; set; } = new();

        public List<TarifaServicio> Tarifas { get; set; } = new();

        public bool HayFiltros =>
            FilterBy != "todos" ||
            !string.IsNullOrWhiteSpace(Q) ||
            !string.IsNullOrWhiteSpace(SelectedOption) ||
            Servicios.Count > 0 || Clases.Count > 0 || Todos.Count > 0 || 
            Montos.Count > 0 || FechasDesde.Count > 0 || FechasHasta.Count > 0;
    }
}
