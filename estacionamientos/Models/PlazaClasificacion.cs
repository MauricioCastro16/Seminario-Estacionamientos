// Models/PlazaClasificacion.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace estacionamientos.Models
{
    public class PlazaClasificacion
    {
        // PK compuesta
        public int PlyID { get; set; }
        public int PlzNum { get; set; }
        public int ClasVehID { get; set; }

        // Navs
        public PlazaEstacionamiento Plaza { get; set; } = default!;
        public ClasificacionVehiculo Clasificacion { get; set; } = default!;
    }
}
