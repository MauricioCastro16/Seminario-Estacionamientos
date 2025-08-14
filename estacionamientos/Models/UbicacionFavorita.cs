using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    public class UbicacionFavorita
    {
        // PK,FK al Conductor
        [Required]
        public int ConNU { get; set; }

        // PK “nombre/apodo” único por conductor
        [Required, StringLength(50)]
        public string UbfApodo { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string UbfProv { get; set; } = string.Empty;

        [Required, StringLength(80)]
        public string UbfCiu { get; set; } = string.Empty;

        [Required, StringLength(120)]
        public string UbfDir { get; set; } = string.Empty;

        [StringLength(30)]
        public string? UbfTipo { get; set; }   // casa, trabajo, etc.

        // navegación
        public Conductor Conductor { get; set; } = default!;
    }
}
