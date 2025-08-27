namespace estacionamientos.ViewModels
{
    public class PlayeroCreateVM
    {
        // Datos del nuevo Playero (hereda de Usuario)
        public estacionamientos.Models.Playero Playero { get; set; } = new();

        // Playa seleccionada para asignar
        public int PlayaId { get; set; }
    }
}
