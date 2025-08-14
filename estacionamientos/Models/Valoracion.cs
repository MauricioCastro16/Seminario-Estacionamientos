using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    // Rating del Conductor a la Playa
    public class Valoracion
    {
        [Required]
        public int PlyID { get; set; }

        [Required]
        public int ConNU { get; set; }

        // Estrellas 1..5 (ajustá rango si usás otro esquema)
        [Range(1, 5)]
        public int ValNumEst { get; set; }

        public bool ValFav { get; set; }

        // Navegaciones
        public PlayaEstacionamiento Playa { get; set; } = default!;
        public Conductor Conductor { get; set; } = default!;
    }
}
