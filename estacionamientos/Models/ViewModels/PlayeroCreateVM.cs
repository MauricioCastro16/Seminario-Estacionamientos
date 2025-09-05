using System.ComponentModel.DataAnnotations;
namespace estacionamientos.ViewModels
{
    public class PlayeroCreateVM
    {
        // Datos del nuevo Playero (hereda de Usuario)
        public estacionamientos.Models.Playero Playero { get; set; } = new();

        // Playa seleccionada para asignar
        [Required(ErrorMessage = " Este campo es obligatorio")]
        public int PlayaId { get; set; }
    }
}
