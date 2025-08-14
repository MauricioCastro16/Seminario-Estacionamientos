using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace estacionamientos.Models
{
    // Tabla intermedia: PK compuesta (ConNU, VehPtnt)
    public class Conduce
    {
        // FK a Conductor (hereda de Usuario)
        [Required]
        public int ConNU { get; set; }

        // FK a Vehiculo
        [Required, StringLength(10)]
        public string VehPtnt { get; set; } = string.Empty;

        // Navegaciones
        public Conductor Conductor { get; set; } = default!;
        public Vehiculo Vehiculo { get; set; } = default!;
    }
}
