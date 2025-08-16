using System.ComponentModel.DataAnnotations;

namespace estacionamientos.Models
{
    public class Ocupacion
    {
        // PK compuesta
        public int PlyID { get; set; }           // playa
        public int PlzNum { get; set; }          // plaza
        public string VehPtnt { get; set; } = ""; // vehículo
        [Required]
        public DateTime OcufFyhIni { get; set; } // inicio de ocupación

        // Otros campos
        public DateTime? OcufFyhFin { get; set; } // fin (si ya liberó)
        public bool OcuLlavDej { get; set; }     // dejó llaves

        // Pago (opcional) → FK compuesta a Pago (PlyID, PagNum)
        public int? PagNum { get; set; }

        // Navs
        public PlazaEstacionamiento Plaza { get; set; } = default!;
        public Vehiculo Vehiculo { get; set; } = default!;
        public Pago? Pago { get; set; }          // puede ser null
    }
}
