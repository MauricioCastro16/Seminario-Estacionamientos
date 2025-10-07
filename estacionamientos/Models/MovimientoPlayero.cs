using System.ComponentModel.DataAnnotations;


namespace estacionamientos.Models
{
    public enum TipoMovimiento
    {
        [Display(Name = "Ingreso de Vehículo")]
        IngresoVehiculo,

        [Display(Name = "Egreso de Vehículo")]
        EgresoVehiculo,

        [Display(Name = "Reubicación de Vehículo")]
        ReubicacionVehiculo

        // Agregar mas movimientos si se necesita
    }
    public class MovimientoPlayero
    {
        public int MovNum { get; set; }

        public int PlyID { get; set; }

        public int PlaNU { get; set; }

        public TipoMovimiento TipoMov { get; set; } 

        public DateTime FechaMov { get; set; } = DateTime.UtcNow;

        //datos útiles
        public int? PlzNum { get; set; }
        public string? VehPtnt { get; set; }

        public PlayaEstacionamiento? Playa { get; set; }      // PlyID → Playa
        public Playero? Playero { get; set; }  // PlaNU → Playero
    }
}

