using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    public class Vehiculo
    {
        [Key]
        [StringLength(10)]
        public string VehPtnt { get; set; } = string.Empty;

        [Required, StringLength(80)]
        public string VehMarc { get; set; } = string.Empty;

        // FK requerido a ClasificacionVehiculo
        [Required]
        public int ClasVehID { get; set; }

        // Navegación
        public ClasificacionVehiculo Clasificacion { get; set; } = default!;

        public ICollection<Conduce> Conducciones { get; set; } = new List<Conduce>();
        public ICollection<Ocupacion> Ocupaciones { get; set; } = new List<Ocupacion>();
    }
}
