using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    // Hereda toda la info de Usuario
    public class Conductor : Usuario
    {
        [StringLength(8, MinimumLength = 8, ErrorMessage = "* El DNI debe tener exactamente 8 dígitos.")]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "* El DNI debe contener solo números y tener 8 dígitos.")]
        [Display(Name = "DNI")]
        public string? ConDNI { get; set; }

        public ICollection<Conduce> Conducciones { get; set; } = new List<Conduce>();
        public ICollection<UbicacionFavorita> UbicacionesFavoritas { get; set; } = new List<UbicacionFavorita>();
        public ICollection<Valoracion> Valoraciones { get; set; } = new List<Valoracion>();

    }
}
