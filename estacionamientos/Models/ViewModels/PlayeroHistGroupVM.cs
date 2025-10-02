namespace estacionamientos.ViewModels
{
    public class PlayeroHistGroupVM
    {
        public int PlaNU { get; set; }
        public string PlayeroNombre { get; set; } = "";
        public string PlayeroEmail { get; set; } = "";
        public string PlayeroTelefono { get; set; } = "";
        public List<PeriodoVM> Periodos { get; set; } = new();
    }

    public class PeriodoVM
    {
        public string PlayaNombre { get; set; } = "";
        public DateTime? FechaInicio { get; set; }   // UTC si existen
        public DateTime? FechaFin { get; set; }      // null = vigente
        public bool Vigente { get; set; }
    }
}
