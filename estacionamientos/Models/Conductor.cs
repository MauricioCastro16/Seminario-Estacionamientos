using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    // Hereda toda la info de Usuario
    public class Conductor : Usuario
    {
        // Sin campos adicionales por ahora
        public ICollection<Conduce> Conducciones { get; set; } = new List<Conduce>();
        public ICollection<UbicacionFavorita> UbicacionesFavoritas { get; set; } = new List<UbicacionFavorita>();
        public ICollection<Valoracion> Valoraciones { get; set; } = new List<Valoracion>();

    }
}
