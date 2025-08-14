using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    public class ClasificacionVehiculo
    {
        [Key]
        public int ClasVehID { get; set; }              // PK y FK en Vehiculo

        [Required, StringLength(40)]
        public string ClasVehTipo { get; set; } = "";   // Nombre del tipo (p.ej., “Auto”, “Moto”)

        [StringLength(200)]
        public string? ClasVehDesc { get; set; }        // Descripción opcional

        // Inversa: muchos vehículos pueden tener esta clasificación
        public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
    }
}