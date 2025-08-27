namespace estacionamientos.ViewModels
{
    public class PlayeroIndexVM
    {
        public estacionamientos.Models.Playero Playero { get; set; } = default!;
        public List<estacionamientos.Models.PlayaEstacionamiento> Playas { get; set; } = new();
    }
}
