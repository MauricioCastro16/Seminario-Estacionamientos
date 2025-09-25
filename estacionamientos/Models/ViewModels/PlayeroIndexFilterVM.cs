namespace estacionamientos.ViewModels
{
    public class PlayeroIndexFilterVM
    {
        public List<PlayeroIndexVM> Playeros { get; set; } = new();
        public string Q { get; set; } = "";
        public string FilterBy { get; set; } = "todos";
        public List<string> Nombres { get; set; } = new();
        public List<string> Playas { get; set; } = new();
        public List<string> Todos { get; set; } = new();
        public bool HayFiltros => !string.IsNullOrWhiteSpace(Q) || 
                                 (Nombres?.Any() ?? false) || 
                                 (Playas?.Any() ?? false) || 
                                 (Todos?.Any() ?? false);
    }
}

