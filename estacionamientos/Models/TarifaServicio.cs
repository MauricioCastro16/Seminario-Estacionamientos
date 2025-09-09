using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;


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
        [ValidateNever]
        public ServicioProveido ServicioProveido { get; set; } = default!;

        [ValidateNever]
        public ClasificacionVehiculo ClasificacionVehiculo { get; set; } = default!;
    }
}
