using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    public class TarifaServicio
    {
        // PK compuesta
        public int PlyID { get; set; }
        public int SerID { get; set; }
        public int ClasVehID { get; set; }
        [Required]
        public DateTime TasFecIni { get; set; }   // vigencia desde

        public DateTime? TasFecFin { get; set; }  // vigencia hasta (null = vigente)
        [Required]
        public decimal TasMonto { get; set; }

        // Navs
        public ServicioProveido ServicioProveido { get; set; } = default!;
        public ClasificacionVehiculo ClasificacionVehiculo { get; set; } = default!;
    }
}
